#include <iostream>
#include <memory>
#include <string>
#include <thread>
#include <fmt/compile.h>

#include <grpc/support/log.h>
#include <grpcpp/grpcpp.h>

#include "mombasa_base.h"
#include "mombasa_bridge.h"
#include "mombasa.grpc.pb.h"
#include <spdlog/spdlog.h>

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


Status MombasaBridgeImpl::CallFunction(ServerContext* context, const CallRequest* request, CallResponse* reply) {
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

Status MombasaBridgeImpl::ReadMemory(ServerContext* context, const MemoryPollRequest* request, MemoryReadResponse* reply) {
    return Work([&](auto start) {
        spdlog::info("Read memory requested for {0:x}", request->address());

        auto address = (char*)request->address();
        std::string data(address, request->count());
        reply->set_data(data);
        reply->set_address((google::protobuf::uint64)address);
        TIMER_STOP;
        });
}

Status MombasaBridgeImpl::ReadMemory(ServerContext* context, const MemoryReadRequest* request, MemoryReadResponse* reply) {
    return Work([&](auto start) {
        spdlog::info("Read memory requested for {0:x}", request->address());

        auto address = (char*)request->address();
        std::string data(address, request->count());
        reply->set_data(data);
        reply->set_address((google::protobuf::uint64)address);
        TIMER_STOP;
        });
}

Status MombasaBridgeImpl::WriteMemory(ServerContext* context, const MemoryWriteRequest* request, MemoryWriteResponse* reply) {
    return Work([&](auto start) {
        spdlog::info("Write memory requested for {0:x}", request->address());
        auto address = (char*)request->address();
        auto data = request->data();
        memcpy(address, data.data(), data.length());
        TIMER_STOP;
        });
}

Status MombasaBridgeImpl::AllocateMemory(ServerContext* context, const MemoryAllocateRequest* request, MemoryAllocateResponse* reply) {
    return Work([&](auto start) {
        spdlog::info("Allocate memory requested");
        auto commit = 0x1000;
        auto allocated = VirtualAlloc(NULL, request->length(), commit, request->protection());
        reply->set_address((uint64)allocated);
        TIMER_STOP;
        });
}

Status MombasaBridgeImpl::FreeMemory(ServerContext* context, const MemoryFreeRequest* request, MemoryFreeResponse* reply) {
    return Work([&](auto start) {
        spdlog::info("Free memory requested for {0:x}", request->address());
        auto freed = VirtualFree((void*)request->address(), 0, request->freetype());
        TIMER_STOP;
        });
}

Status MombasaBridgeImpl::SetTlsValue(ServerContext* context, const SetTlsValueRequest* request, SetTlsValueResponse* reply) {
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
    grpc::StatusCode statusCode = grpc::StatusCode::OK;
    auto start = high_resolution_clock::now();

    [&] {
        __try {
            action(start);
        }
        __except (EXCEPTION_EXECUTE_HANDLER) {
            statusCode = grpc::StatusCode::DATA_LOSS;
            exception = GetExceptionCode();
            spdlog::error("Failure during RPC 0x{0:x}", exception);
        }
    }();

    std::string errMessage = "";

    if (exception != 0)
    {
        errMessage = fmt::format("Failure during RPC 0x{0:x}", exception);
    }

    return Status(statusCode, errMessage);
}
