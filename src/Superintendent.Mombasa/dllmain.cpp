// dllmain.cpp : Defines the entry point for the DLL application.
#include <chrono>
#include <thread>
#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include "MombasaServer.cpp"

static std::thread* grpcThread;
static std::unique_ptr<Server> grpcServer;

__declspec(dllexport) void Initialize();
__declspec(dllexport) void Teardown();
void GrpcStartup();

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
    auto console = spdlog::stdout_color_mt("console");
    auto log = spdlog::get("console");
    log->info("mombasa intialize!");
    auto thread = new std::thread(GrpcStartup);
    grpcThread = thread;
}

__declspec(dllexport) void Teardown()
{
    auto log = spdlog::get("console");
    log->info("mombasa teardown!");

    grpcServer->Shutdown();

    grpcThread->join();
    delete grpcThread;
}

void GrpcStartup() 
{
    auto log = spdlog::get("console");
    log->info("grpc_startup running");

    std::string server_address("127.0.0.1:50051");
    MombasaBridgeImpl service;

    grpc::EnableDefaultHealthCheckService(true);
    ServerBuilder builder;
    // Listen on the given address without any authentication mechanism.
    builder.AddListeningPort(server_address, grpc::InsecureServerCredentials());
    // Register "service" as the instance through which we'll communicate with
    // clients. In this case it corresponds to an *synchronous* service.
    builder.RegisterService(&service);

    // Finally assemble the server.
    grpcServer = builder.BuildAndStart();

    log->info("gRPC Server listening on " + server_address);

    // Wait for the server to shutdown. Note that some other thread must be
    // responsible for shutting down the server for this call to ever return.
    grpcServer->Wait();

    log->info("gRPC thread should be ending");
}
