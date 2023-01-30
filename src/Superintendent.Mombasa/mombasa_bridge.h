#pragma once
#include <iostream>
#include <memory>
#include <string>
#include <thread>
#include <fmt/compile.h>

#include <grpc/support/log.h>
#include <grpcpp/grpcpp.h>

#include "mombasa_base.h"
#include "mombasa.grpc.pb.h"
#include <spdlog/spdlog.h>

#include <stdlib.h>
#include <locale.h>
#include <stdio.h>
#include <tchar.h>

#include <process.h>
#include <iostream>
#include <Windows.h>
#include "dbghelp.h"

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerBuilder;
using grpc::CompletionQueue;
using grpc::ServerCompletionQueue;
using grpc::ServerContext;
using grpc::Status;
using std::chrono::high_resolution_clock;
using namespace mombasa;
using namespace google::protobuf;

#define TIMER_STOP reply->set_durationmicroseconds(std::chrono::duration_cast<std::chrono::microseconds>(high_resolution_clock::now() - start).count())

extern "C" {
    // argv must have 12 elements present!!
    uint64 function_dispatcher(void* func, uint64* argv, bool isFloat);
}

// Exporting it as a friendly name to easily track down in disassembly, etc
extern "C" __declspec(dllexport) uint64 FunctionDispatcher(void* func, uint64 * argv, bool isFloat)
{
    return function_dispatcher(func, argv, isFloat);
}

// Logic and data behind the server's behavior.
class MombasaBridgeImpl final : public MombasaBridge::AsyncService {

public:

    Status CallFunction(ServerContext* context, const CallRequest* request, CallResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Call function invoked for {0:x}, {1} args, isfloat: {2}", request->functionpointer(), request->args_size(), request->returnsfloat());
            auto func = (void*)request->functionpointer();
            auto argc = request->args_size();
            uint64 argv[12];
            auto isfloat = request->returnsfloat();

            for (auto i = 0; i < argc; i++)
                argv[i] = request->args(i);

            auto retval = function_dispatcher(func, argv, isfloat);
            reply->set_value(retval);
            reply->set_success(true);

            TIMER_STOP;
        });
    }

    Status ReadMemory(ServerContext* context, PollingState* pollingState, const MemoryPollRequest* request, MemoryReadResponse* reply) {
        return Work([&](auto start) {
            auto n = pollingState->GetPollCount();
            if (n <= 1) {
                spdlog::info("Polling memory requested for {0:x}, every {1}ms", request->address(), pollingState->GetInterval());
            }
            else if (n % 10000 == 0) {
                spdlog::info("Poll number {0} for memory read at address {1:x}", n, request->address());
            }

            auto address = (char*)request->address();
            std::string data(address, request->count());
            reply->set_data(data);
            reply->set_address((google::protobuf::uint64)address);
            TIMER_STOP;
        });
    }

    Status ReadMemory(ServerContext* context, const MemoryReadRequest* request, MemoryReadResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Read memory requested for {0:x}", request->address());

            auto address = (char*)request->address();
            std::string data(address, request->count());
            reply->set_data(data);
            reply->set_address((google::protobuf::uint64)address);
            TIMER_STOP;
        });
    }

    Status WriteMemory(ServerContext* context, const MemoryWriteRequest* request, MemoryWriteResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Write memory requested for {0:x}", request->address());
            auto address = (char*)request->address();
            auto data = request->data();
            memcpy(address, data.data(), data.length());
            TIMER_STOP;
        });
    }

    Status AllocateMemory(ServerContext* context, const MemoryAllocateRequest* request, MemoryAllocateResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Allocate memory requested");
            auto commit = 0x1000;
            auto allocated = VirtualAlloc(NULL, request->length(), commit, request->protection());
            reply->set_address((uint64)allocated);
            TIMER_STOP;
        });
    }

    Status FreeMemory(ServerContext* context, const MemoryFreeRequest* request, MemoryFreeResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Free memory requested for {0:x}", request->address());
            auto freed = VirtualFree((void*)request->address(), 0, request->freetype());
            TIMER_STOP;
        });
    }

    Status ProtectMemory(ServerContext* context, const MemoryProtectRequest* request, MemoryProtectResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Protect memory requested");
            DWORD old;
            VirtualProtect((void*)request->address(), request->length(), request->protection(), &old);
            TIMER_STOP;
        });
    }

    Status SetTlsValue(ServerContext* context, const SetTlsValueRequest* request, SetTlsValueResponse* reply) override {
        return Work([&](auto start) {
            auto index = request->index();
            auto address = request->value();
            spdlog::info("Set TLS value requested for {0}/{1:x}", index, address);
            auto set = TlsSetValue(index, (void*)address);
            TIMER_STOP;
        });
    }

    Status Work(std::function<void(std::chrono::steady_clock::time_point)> action) {
        std::stringstream ss;
        ss << std::this_thread::get_id();
        unsigned long exception;
        uint64 exceptionLocation;
        grpc::StatusCode statusCode = grpc::StatusCode::OK;
        auto start = high_resolution_clock::now();

        [&] {
            __try {
                action(start);
            }
            __except (FilterException(GetExceptionCode(), GetExceptionInformation(), &exceptionLocation)) {
                statusCode = grpc::StatusCode::ABORTED;
                exception = GetExceptionCode();
            }
        }();

        std::string errMessage = "";

        if (exception != 0)
        {
            errMessage = fmt::format("Failure during RPC 0x{0:x} at 0x{1:x}", exception, exceptionLocation);
        }

        return Status(statusCode, errMessage);
    }

    int FilterException(unsigned long exception, EXCEPTION_POINTERS* exceptionPointers, uint64* outExceptionLocation) {
        auto loc = (uint64)exceptionPointers->ExceptionRecord->ExceptionAddress;
        spdlog::error("Failure during RPC 0x{0:x} at 0x{1:x}", exception, loc);
        *outExceptionLocation = loc;
        return EXCEPTION_EXECUTE_HANDLER;
    }
};