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
#include <tlhelp32.h>
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

extern "C" void setthreadlocal(uint64 value);
extern "C" uint64 getthreadlocal();

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

    Status ReadPointer(ServerContext* context, const PointerReadRequest* request, PointerReadResponse* reply) override {
        return Work([&](auto start) {
            
            auto address = resolvePointer(request->base(), request->chain(), request->chain_size());
            reply->set_address(address);
            spdlog::info("Pointer read requested, resolved to 0x{0:x}", address);

            reply->set_data(std::string((char*)address, request->size()));
            TIMER_STOP;
        });
    }

    Status WritePointer(ServerContext* context, const PointerWriteRequest* request, PointerWriteResponse* reply) override {
        return Work([&](auto start) {
            auto address = resolvePointer(request->base(), request->chain(), request->chain_size());
            reply->set_address(address);
            spdlog::info("Pointer write requested, resolved to 0x{0:x}", address);

            auto data = request->data();
            memcpy((char*)address, data.data(), data.length());
            TIMER_STOP;
        });
    }

    uint64 resolvePointer(uint64 base, RepeatedField<uint32> chain, int chainLength)
    {
        uint64 address = base;

        for (int i = 0; i < chainLength; i++)
        {
            address = *(uint64*)(address + chain.Get(i));
        }

        return address;
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

    Status SetThreadLocalPointer(ServerContext* context, const SetThreadLocalPointerRequest* request, SetThreadLocalPointerResponse* reply) override {
        return Work([&](auto start) {
            auto val = request->value();
            spdlog::info("Set ThreadLocalPointer value requested for {0:x}", val);
            
            setthreadlocal(val);

            TIMER_STOP;
        });
    }

    Status GetThreadLocalPointer(ServerContext* context, const GetThreadLocalPointerRequest* request, GetThreadLocalPointerResponse* reply) override {
        return Work([&](auto start) {

            spdlog::info("Get ThreadLocalPointer value requested");
            auto val = getthreadlocal();
            reply->set_value(val);

            spdlog::info("Get ThreadLocalPointer value requested, val {0:x}", val);

            TIMER_STOP;
        });
    }


    Status GetWorkerThread(ServerContext* context, const GetWorkerThreadRequest* request, GetWorkerThreadResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("GetWorkerThread requested");
            auto me = GetCurrentThreadId();
            reply->set_threadid(me);

            TIMER_STOP;
        });
    }

    Status PauseAppThreads(ServerContext* context, const PauseAppThreadsRequest* request, PauseAppThreadsResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Pausing app threads");

            SetThreadsExec(false, reply->mutable_threadsuspendcounts());
            
            TIMER_STOP;
        });
    }

    Status ResumeAppThreads(ServerContext* context, const ResumeAppThreadsRequest* request, ResumeAppThreadsResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Resuming app threads");

            SetThreadsExec(true, reply->mutable_threadsuspendcounts());

            TIMER_STOP;
        });
    }

    

    Status DxStart(ServerContext* context, const DxStartRequest* request, DxStartResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Hooking DX");

            

            TIMER_STOP;
        });
    }


    Status DxEnd(ServerContext* context, const DxEndRequest* request, DxEndResponse* reply) override {
        return Work([&](auto start) {
            spdlog::info("Unhooking DX");


            TIMER_STOP;
        });
    }

    bool SetThreadsExec(bool run, Map<uint32, uint32>* map)
    {
        auto action = run ? "resuming" : "pausing";

        auto me = GetCurrentThreadId();
        auto proc = GetCurrentProcessId();

        auto threadSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD, proc);
        if (threadSnapshot == INVALID_HANDLE_VALUE)
        {
            spdlog::error("Couldn't create snapshot when {0} app threads", action);
            return false;
        }

        THREADENTRY32 thread;
        thread.dwSize = sizeof(THREADENTRY32);

        if (!Thread32First(threadSnapshot, &thread))
        {
            spdlog::error("Couldn't enumerate thread snapshot when {0} app threads", action);
            CloseHandle(threadSnapshot);
            return false;
        }

        do
        {
            // sanity check
            if (thread.th32OwnerProcessID != proc)
                continue;

            // Don't manip ourselves :)
            if (thread.th32ThreadID == me)
                continue;

            auto thandle = OpenThread(THREAD_SUSPEND_RESUME, false, thread.th32ThreadID);
            
            // couldn't open handle
            if (thandle == INVALID_HANDLE_VALUE || thandle == NULL)
                continue;

            DWORD prev;

            if (run)
            {
                prev = ResumeThread(thandle);
            }
            else
            {
                prev = SuspendThread(thandle);
            }

            CloseHandle(thandle);

            (*map)[(uint32)thread.th32ThreadID] = prev;

        } while (Thread32Next(threadSnapshot, &thread));
    }


    Status Work(std::function<void(std::chrono::steady_clock::time_point)> action) {
        std::stringstream ss;
        ss << std::this_thread::get_id();
        unsigned long exception;
        uint64 data[11];
        char exceptionMessage[128];
        data[10] = (uint64) & exceptionMessage;
        grpc::StatusCode statusCode = grpc::StatusCode::OK;
        auto start = high_resolution_clock::now();

        [&] {
            __try {
                action(start);
            }
            __except (FilterException(GetExceptionCode(), GetExceptionInformation(), data)) {
                statusCode = grpc::StatusCode::ABORTED;
                exception = GetExceptionCode();
            }
        }();

        std::string errMessage = "";

        if (exception != 0)
        {
            errMessage = fmt::format("Failure during RPC 0x{0:x} at 0x{1:x}\r\n RSP:{2:x}\r\n RBP:{3:x}\r\n RAX:{4:x}\r\n RBX:{5:x}\r\n RCX:{6:x}\r\n RDX:{7:x}\r\n R8:{8:x}\r\n R9:{9:x}\r\n LastBranchFrom:{10:x}", exception, data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9]);
        }

        return Status(statusCode, errMessage);
    }

    int FilterException(unsigned long exception, EXCEPTION_POINTERS* exceptionPointers, uint64* data) {
        auto loc = (uint64)exceptionPointers->ExceptionRecord->ExceptionAddress;

        auto ctx = exceptionPointers->ContextRecord;

        auto rsp = ctx->Rsp;
        auto rbp = ctx->Rbp;

        auto rax = ctx->Rax;
        auto rbx = ctx->Rbx;
        auto rcx = ctx->Rcx;
        auto rdx = ctx->Rdx;
        
        auto r8 = ctx->R8;
        auto r9 = ctx->R9;
        auto lastBranch = ctx->LastBranchFromRip;

        data[0] = loc;
        data[1] = rsp;
        data[2] = rbp;
        data[3] = rax;
        data[4] = rbx;
        data[5] = rcx;
        data[6] = rdx;
        data[7] = r8;
        data[8] = r9;
        data[9] = lastBranch;
        auto buf = (char*)data[10];

        spdlog::error("Failure during RPC 0x{0:x} at 0x{1:x}\r\n RSP:{2:x}\r\n RBP:{3:x}\r\n RAX:{4:x}\r\n RBX:{5:x}\r\n RCX:{6:x}\r\n RDX:{7:x}\r\n R8:{8:x}\r\n R9:{9:x}\r\n LastBranchFrom:{10:x}", exception, loc, rsp, rbp, rax, rbx, rcx, rdx, r8, r9, lastBranch);


        return EXCEPTION_EXECUTE_HANDLER;
    }
};