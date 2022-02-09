#include <iostream>
#include <memory>
#include <string>
#include <thread>
#include <fmt/compile.h>

#include <grpc/support/log.h>
#include <grpcpp/grpcpp.h>

#include "mombasa.grpc.pb.h"
#include <spdlog/spdlog.h>

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerBuilder;
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

// Logic and data behind the server's behavior.
class MombasaBridgeImpl final : public MombasaBridge::Service {

    std::shared_ptr<spdlog::logger> log = spdlog::get("console");

    Status CallFunction(ServerContext* context, const CallRequest* request, CallResponse* reply) override {
        return Work([&](auto start) {
            log->info("Call function invoked for {0:x}, {1} args, isfloat: {2}", request->functionpointer(), request->args_size(), request->returnsfloat());
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

    Status ReadMemory(ServerContext* context, const MemoryReadRequest* request, MemoryReadResponse* reply) override {
        return Work([&](auto start) {
            log->info("Read memory requested for {0:x}", request->address());

            auto address = (char*)request->address();
            std::string data(address, request->count());
            reply->set_data(data);
            reply->set_address((google::protobuf::uint64)address);
            TIMER_STOP;
        });
    }

    Status WriteMemory(ServerContext* context, const MemoryWriteRequest* request, MemoryWriteResponse* reply) override {
        return Work([&](auto start) {
            log->info("Write memory requested for {0:x}", request->address());
            auto address = (char*)request->address();
            auto data = request->data();
            memcpy(address, data.data(), data.length());
            TIMER_STOP;
        });
    }

    Status AllocateMemory(ServerContext* context, const MemoryAllocateRequest* request, MemoryAllocateResponse* reply) override {
        return Work([&](auto start) {
            log->info("Allocate memory requested");
            auto commit = 0x1000;
            auto allocated = VirtualAlloc(NULL, request->length(), commit, request->protection());
            reply->set_address((uint64)allocated);
            TIMER_STOP;
        });
    }

    Status FreeMemory(ServerContext* context, const MemoryFreeRequest* request, MemoryFreeResponse* reply) override {
        return Work([&](auto start) {
            log->info("Free memory requested for {0:x}", request->address());
            auto freed = VirtualFree((void*)request->address(), 0, request->freetype());
            TIMER_STOP;
        });
    }

    Status SetTlsValue(ServerContext* context, const SetTlsValueRequest* request, SetTlsValueResponse* reply) override {
        return Work([&](auto start) {
            auto index = request->index();
            auto address = request->value();
            log->info("Set TLS value requested for {0}/{1:x}", index, address);
            auto set = TlsSetValue(index, (void*)address);
            TIMER_STOP;
        });
    }

    Status Work(std::function<void(std::chrono::steady_clock::time_point)> action) {
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
                log->error("Failure during RPC 0x{0:x}", exception);
            }
        }();

        std::string errMessage = "";

        if (exception != 0)
        {
            errMessage = fmt::format("Failure during RPC 0x{0:x}", exception);
        }

        return Status(statusCode, errMessage);
    }
};