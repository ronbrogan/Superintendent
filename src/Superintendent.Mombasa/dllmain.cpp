// dllmain.cpp : Defines the entry point for the DLL application.
#include <chrono>
#include <thread>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/sinks/basic_file_sink.h>
#include "mombasa_bridge.cpp"

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
    //auto console_sink = spdlog::stdout_color_mt("console");
    //spdlog::set_default_logger(console_sink);

    HMODULE currentModule;
    if (GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCWSTR)DllMain, &currentModule))
    {
        char filePath[512];
        GetModuleFileNameA(currentModule, filePath, 512);

        std::cout << "Loaded mombasa: " << filePath << std::endl;

        auto logpath = std::filesystem::path(filePath).parent_path().append("log.mombasa.txt");

        std::cout << "Mombasa log: " << logpath << std::endl;

        auto file_sink = std::make_shared<spdlog::sinks::basic_file_sink_mt>(logpath.string());
        file_sink->set_level(spdlog::level::trace);

        spdlog::logger logger("multi_sink", { 
            std::make_shared<spdlog::sinks::stdout_color_sink_mt>(), 
            file_sink
        });

        logger.flush_on(spdlog::level::trace);

        spdlog::set_default_logger(std::make_shared<spdlog::logger>(logger));
    }

    spdlog::info("mombasa intialize!");
    auto thread = new std::thread(GrpcStartup);
    grpcThread = thread;
}

__declspec(dllexport) void Teardown()
{
    spdlog::info("mombasa teardown!");

    grpcServer->Shutdown();

    grpcThread->join();
    delete grpcThread;
}

void GrpcStartup() 
{
    spdlog::info("grpc_startup running");

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

    spdlog::info("gRPC Server listening on " + server_address);

    // Wait for the server to shutdown. Note that some other thread must be
    // responsible for shutting down the server for this call to ever return.
    grpcServer->Wait();

    spdlog::info("gRPC thread should be ending");
}
