// dllmain.cpp : Defines the entry point for the DLL application.
#include <chrono>
#include <thread>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <spdlog/sinks/stdout_color_sinks.h>
#include <spdlog/sinks/rotating_file_sink.h>
#include <grpcpp/grpcpp.h>
#include "mombasa_bridge.cpp"
using namespace mombasa;

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerBuilder;
using grpc::ServerCompletionQueue;
using grpc::ServerContext;
using grpc::Status;

static std::thread* grpcThread;
static std::shared_ptr<Server> grpcServer;
static std::shared_ptr<ServerCompletionQueue> grpcCompletionQueue;

__declspec(dllexport) void Initialize();
__declspec(dllexport) void Teardown();
void GrpcStartup();
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
    grpcThread = thread;
}

__declspec(dllexport) void Teardown()
{
    spdlog::info("mombasa teardown!");

    auto deadline = std::chrono::system_clock::now();
    
    grpcServer->Shutdown(deadline);
    spdlog::info("mombasa server has shutdown");

    grpcCompletionQueue->Shutdown();
    spdlog::info("mombasa cq has shutdown");

    grpcThread->join();
    delete grpcThread;
    spdlog::info("mombasa server thread has joined");
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

// Class encompasing the state and logic needed to serve a request.

class CallDataBase {
public:
    virtual void Handle() = 0;
};

template <typename TReq, typename TResp>
class CallData : public CallDataBase {
public:
    // Take in the "service" instance (in this case representing an asynchronous
    // server) and the completion queue "cq" used for asynchronous communication
    // with the gRPC runtime.
    CallData(MombasaBridge::AsyncService* service,
        ServerCompletionQueue* cq,
        std::function<void(ServerContext* context, TReq* request, ServerAsyncResponseWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc,
        std::function<Status(ServerContext* context, TReq* request, TResp* response)> implfunc)
        : ready(true), service_(service), cq_(cq), ctx_(new ServerContext()), responder_(ctx_), readfunc_(readfunc), implfunc_(implfunc) {
        // Invoke the serving logic right away.
        readfunc(ctx_, &request_, &responder_, cq_, cq_, this);
    }

    void Handle() override {
        if (ready) {
            // Process current request
            auto status = implfunc_(ctx_, &request_, &reply_);
            responder_.Finish(reply_, status, this);
            ready = false;
        } else {
            // allow this instance to handle another request
            Reset();
            readfunc_(ctx_, &request_, &responder_, cq_, cq_, this);
        }
    }

    void Reset() {
        delete ctx_;
        ctx_ = new ServerContext();
        responder_ = ServerAsyncResponseWriter<TResp>(ctx_);
        ready = true;
    }

private:
    bool ready;
    // The means of communication with the gRPC runtime for an asynchronous
    // server.
    MombasaBridge::AsyncService* service_;
    // The producer-consumer queue where for asynchronous server notifications.
    ServerCompletionQueue* cq_;
    // Context for the rpc, allowing to tweak aspects of it such as the use
    // of compression, authentication, as well as to send metadata back to the
    // client.
    ServerContext* ctx_;

    TReq request_;
    TResp reply_;
    ServerAsyncResponseWriter<TResp> responder_;

    std::function<void(ServerContext* context, TReq* request, ServerAsyncResponseWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc_;
    std::function<Status(ServerContext* context, TReq* request, TResp* response)> implfunc_;
};

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

    new CallData<MemoryWriteRequest, MemoryWriteResponse>(s, cq,
        [s](auto && ...args) { s->RequestWriteMemory(args...); },
        [i](auto && ...args) { return i->WriteMemory(args...); });

    new CallData<MemoryReadRequest, MemoryReadResponse>(s, cq,
        [s](auto && ...args) { s->RequestReadMemory(args...); },
        [i](auto && ...args) { return i->ReadMemory(args...); });

    new CallData<SetTlsValueRequest, SetTlsValueResponse>(s, cq,
        [s](auto && ...args) { s->RequestSetTlsValue(args...); },
        [i](auto && ...args) { return i->SetTlsValue(args...); });

    void* tag;
    bool ok;
    while (true) {
        // Block waiting to read the next event from the completion queue. The
        // event is uniquely identified by its tag, which in this case is the
        // memory address of a CallData instance.
        // The return value of Next should always be checked. This return value
        // tells us whether there is any kind of event or cq_ is shutting down.
        GPR_ASSERT(grpcCompletionQueue->Next(&tag, &ok));
        GPR_ASSERT(ok);
        static_cast<CallDataBase*>(tag)->Handle();
    }
}