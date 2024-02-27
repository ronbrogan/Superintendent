// dllmain.cpp : Defines the entry point for the DLL application.
#define _SILENCE_STDEXT_ARR_ITERS_DEPRECATION_WARNING


#include <chrono>
#include <thread>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <grpcpp/grpcpp.h>
#include <grpcpp/alarm.h>

#include "mombasa_base.h"
#include "mombasa_calldata.h"
#include "mombasa_bridge.h"
using namespace mombasa;

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerAsyncWriter;
using grpc::ServerBuilder;
using grpc::ServerCompletionQueue;
using grpc::ServerContext;
using grpc::Status;
using grpc::StatusCode;

static std::thread* shutdownThread;
static std::thread* grpcThread;
static std::shared_ptr<Server> grpcServer;
static std::shared_ptr<ServerCompletionQueue> grpcCompletionQueue;

__declspec(dllexport) void Initialize();
__declspec(dllexport) void Teardown();
void GrpcStartup();
void Shutdown();
void ShutdownFailsafe();
void HandleRpcs(MombasaBridge::AsyncService* service);

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        Initialize();
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        Teardown();
        break;
    }
    return TRUE;
}

__declspec(dllexport) void Initialize()
{
    HMODULE currentModule;
    if (GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCWSTR)DllMain, &currentModule))
    {
        char filePath[512];
        GetModuleFileNameA(currentModule, filePath, 512);

        std::cout << "Loaded mombasa: " << filePath << std::endl;

        auto logpath = std::filesystem::path(filePath).parent_path().append("log.mombasa.txt");

        std::cout << "Mombasa log: " << logpath << std::endl;

        auto file_sink = std::make_shared<spdlog::sinks::rotating_file_sink_mt>(logpath.string(), 1024*1024, 2, true);
        file_sink->set_level(spdlog::level::trace);

        spdlog::logger logger("multi_sink", { 
            std::make_shared<spdlog::sinks::stdout_color_sink_mt>(), 
            file_sink
        });

        logger.flush_on(spdlog::level::trace);

        spdlog::set_default_logger(std::make_shared<spdlog::logger>(logger));
    }
    else
    {
        auto console_sink = spdlog::stdout_color_mt("console");
        spdlog::set_default_logger(console_sink);
    }

    // ISO8601 [pid]<tid> [level] msg
    spdlog::set_pattern("%Y-%m-%d %H:%M:%S.%e %z [%P]<%t> [%l] %v");

    spdlog::info("mombasa intialize!");
    auto thread = new std::thread(GrpcStartup);

    SetThreadDescription(thread->native_handle(), L"mombasa-worker");

    grpcThread = thread;
}

__declspec(dllexport) void Teardown()
{
    spdlog::info("mombasa teardown!");
    shutdownThread = new std::thread(Shutdown);
}

void Shutdown()
{
    const std::chrono::milliseconds waitDuration = std::chrono::milliseconds(50);
    const std::chrono::time_point<std::chrono::system_clock> deadline = std::chrono::system_clock::now() + waitDuration;

    auto failsafe = new std::thread(ShutdownFailsafe);

    grpcServer->Shutdown(deadline);
    spdlog::info("mombasa server has shutdown");

    grpcCompletionQueue->Shutdown();
    spdlog::info("mombasa cq has shutdown");

    // if we make it to here, we can safely join and terminate
    failsafe->join();
}

void ShutdownFailsafe()
{
    Sleep(100);

    // ideall we'd join after shutdowns, however even with deadline
    // the server shutdown can just hang forever :) really nice software
    grpcThread->detach();
    delete grpcThread;
    spdlog::info("mombasa server thread detached");


    shutdownThread->detach();
    delete shutdownThread;
    spdlog::info("mombasa shutdown thread detached");
}

void GrpcStartup() 
{
    spdlog::info("grpc_startup running");

    std::string server_address("127.0.0.1:50051");
    MombasaBridge::AsyncService service;

    grpc::EnableDefaultHealthCheckService(true);
    ServerBuilder builder;
    // Listen on the given address without any authentication mechanism.
    builder.AddListeningPort(server_address, grpc::InsecureServerCredentials());
    // Register "service" as the instance through which we'll communicate with
    // clients. In this case it corresponds to an *synchronous* service.
    builder.RegisterService(&service);

    builder.SetMaxReceiveMessageSize(128 * 1024 * 1024);
    builder.SetMaxSendMessageSize(128 * 1024 * 1024);

    grpcCompletionQueue = builder.AddCompletionQueue();

    // Finally assemble the server.
    grpcServer = builder.BuildAndStart();

    spdlog::info("gRPC Server listening on " + server_address);

    HandleRpcs(&service);
}

void HandleRpcs(MombasaBridge::AsyncService* s) {

    MombasaBridgeImpl impl;
    auto i = &impl;
    auto cq = grpcCompletionQueue.get();

    new CallData<CallRequest, CallResponse>(s, cq,
        [s](auto && ...args) { s->RequestCallFunction(args...); },
        [i](auto && ...args) { return i->CallFunction(args...); });

    new CallData<MemoryAllocateRequest, MemoryAllocateResponse>(s, cq,
        [s](auto && ...args) { s->RequestAllocateMemory(args...); },
        [i](auto && ...args) { return i->AllocateMemory(args...); });

    new CallData<MemoryFreeRequest, MemoryFreeResponse>(s, cq,
        [s](auto && ...args) { s->RequestFreeMemory(args...); },
        [i](auto && ...args) { return i->FreeMemory(args...); });

    new CallData<MemoryProtectRequest, MemoryProtectResponse>(s, cq,
        [s](auto && ...args) { s->RequestProtectMemory(args...); },
        [i](auto && ...args) { return i->ProtectMemory(args...); });

    new CallData<MemoryWriteRequest, MemoryWriteResponse>(s, cq,
        [s](auto && ...args) { s->RequestWriteMemory(args...); },
        [i](auto && ...args) { return i->WriteMemory(args...); });

    new CallData<MemoryReadRequest, MemoryReadResponse>(s, cq,
        [s](auto && ...args) { s->RequestReadMemory(args...); },
        [i](auto && ...args) { return i->ReadMemory(args...); });

    new CallData<PointerReadRequest, PointerReadResponse>(s, cq,
        [s](auto && ...args) { s->RequestReadPointer(args...); },
        [i](auto && ...args) { return i->ReadPointer(args...); });

    new CallData<PointerWriteRequest, PointerWriteResponse>(s, cq,
        [s](auto && ...args) { s->RequestWritePointer(args...); },
        [i](auto && ...args) { return i->WritePointer(args...); });

    new CallData<GetWorkerThreadRequest, GetWorkerThreadResponse>(s, cq,
        [s](auto && ...args) { s->RequestGetWorkerThread(args...); },
        [i](auto && ...args) { return i->GetWorkerThread(args...); });

    new CallData<PauseAppThreadsRequest, PauseAppThreadsResponse>(s, cq,
        [s](auto && ...args) { s->RequestPauseAppThreads(args...); },
        [i](auto && ...args) { return i->PauseAppThreads(args...); });

    new CallData<ResumeAppThreadsRequest, ResumeAppThreadsResponse>(s, cq,
        [s](auto && ...args) { s->RequestResumeAppThreads(args...); },
        [i](auto && ...args) { return i->ResumeAppThreads(args...); });

    new CallData<SetTlsValueRequest, SetTlsValueResponse>(s, cq,
        [s](auto && ...args) { s->RequestSetTlsValue(args...); },
        [i](auto && ...args) { return i->SetTlsValue(args...); });

    new CallData<SetThreadLocalPointerRequest, SetThreadLocalPointerResponse>(s, cq,
        [s](auto && ...args) { s->RequestSetThreadLocalPointer(args...); },
        [i](auto && ...args) { return i->SetThreadLocalPointer(args...); });

    new CallData<GetThreadLocalPointerRequest, GetThreadLocalPointerResponse>(s, cq,
        [s](auto && ...args) { s->RequestGetThreadLocalPointer(args...); },
        [i](auto && ...args) { return i->GetThreadLocalPointer(args...); });

    new PollingStreamCallData<MemoryPollRequest, MemoryReadResponse>(s, cq,
        [s](auto && ...args) { s->RequestPollMemory(args...); },
        [i](auto && ...args) { return i->ReadMemory(args...); },
        [s](const MemoryPollRequest* request) { return PollingState(request->interval()); });

    new CallData<DxStartRequest, DxStartResponse>(s, cq,
        [s](auto && ...args) { s->RequestDxStart(args...); },
        [i](auto && ...args) { return i->DxStart(args...); });

    new CallData<DxEndRequest, DxEndResponse>(s, cq,
        [s](auto && ...args) { s->RequestDxEnd(args...); },
        [i](auto && ...args) { return i->DxEnd(args...); });

    void* tag;
    bool ok;
    bool run = true;
    while (run) {
        // Block waiting to read the next event from the completion queue. The
        // event is uniquely identified by its tag, which in this case is the
        // memory address of a CallData instance.
        // The return value of Next should always be checked. This return value
        // tells us whether there is any kind of event or cq_ is shutting down.
        run = grpcCompletionQueue->Next(&tag, &ok);

        if (run && ok) {
            static_cast<CallDataBase*>(tag)->Handle();
        }
    }
}