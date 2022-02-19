// Generated by the gRPC C++ plugin.
// If you make any local change, they will be lost.
// source: mombasa.proto

#include "mombasa.pb.h"
#include "mombasa.grpc.pb.h"

#include <functional>
#include <grpcpp/impl/codegen/async_stream.h>
#include <grpcpp/impl/codegen/async_unary_call.h>
#include <grpcpp/impl/codegen/channel_interface.h>
#include <grpcpp/impl/codegen/client_unary_call.h>
#include <grpcpp/impl/codegen/client_callback.h>
#include <grpcpp/impl/codegen/message_allocator.h>
#include <grpcpp/impl/codegen/method_handler.h>
#include <grpcpp/impl/codegen/rpc_service_method.h>
#include <grpcpp/impl/codegen/server_callback.h>
#include <grpcpp/impl/codegen/server_callback_handlers.h>
#include <grpcpp/impl/codegen/server_context.h>
#include <grpcpp/impl/codegen/service_type.h>
#include <grpcpp/impl/codegen/sync_stream.h>
namespace mombasa {

static const char* MombasaBridge_method_names[] = {
  "/mombasa.MombasaBridge/CallFunction",
  "/mombasa.MombasaBridge/AllocateMemory",
  "/mombasa.MombasaBridge/FreeMemory",
  "/mombasa.MombasaBridge/WriteMemory",
  "/mombasa.MombasaBridge/ReadMemory",
  "/mombasa.MombasaBridge/PollMemory",
  "/mombasa.MombasaBridge/SetTlsValue",
};

std::unique_ptr< MombasaBridge::Stub> MombasaBridge::NewStub(const std::shared_ptr< ::grpc::ChannelInterface>& channel, const ::grpc::StubOptions& options) {
  (void)options;
  std::unique_ptr< MombasaBridge::Stub> stub(new MombasaBridge::Stub(channel, options));
  return stub;
}

MombasaBridge::Stub::Stub(const std::shared_ptr< ::grpc::ChannelInterface>& channel, const ::grpc::StubOptions& options)
  : channel_(channel), rpcmethod_CallFunction_(MombasaBridge_method_names[0], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  , rpcmethod_AllocateMemory_(MombasaBridge_method_names[1], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  , rpcmethod_FreeMemory_(MombasaBridge_method_names[2], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  , rpcmethod_WriteMemory_(MombasaBridge_method_names[3], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  , rpcmethod_ReadMemory_(MombasaBridge_method_names[4], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  , rpcmethod_PollMemory_(MombasaBridge_method_names[5], options.suffix_for_stats(),::grpc::internal::RpcMethod::SERVER_STREAMING, channel)
  , rpcmethod_SetTlsValue_(MombasaBridge_method_names[6], options.suffix_for_stats(),::grpc::internal::RpcMethod::NORMAL_RPC, channel)
  {}

::grpc::Status MombasaBridge::Stub::CallFunction(::grpc::ClientContext* context, const ::mombasa::CallRequest& request, ::mombasa::CallResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::CallRequest, ::mombasa::CallResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_CallFunction_, context, request, response);
}

void MombasaBridge::Stub::async::CallFunction(::grpc::ClientContext* context, const ::mombasa::CallRequest* request, ::mombasa::CallResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::CallRequest, ::mombasa::CallResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_CallFunction_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::CallFunction(::grpc::ClientContext* context, const ::mombasa::CallRequest* request, ::mombasa::CallResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_CallFunction_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::CallResponse>* MombasaBridge::Stub::PrepareAsyncCallFunctionRaw(::grpc::ClientContext* context, const ::mombasa::CallRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::CallResponse, ::mombasa::CallRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_CallFunction_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::CallResponse>* MombasaBridge::Stub::AsyncCallFunctionRaw(::grpc::ClientContext* context, const ::mombasa::CallRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncCallFunctionRaw(context, request, cq);
  result->StartCall();
  return result;
}

::grpc::Status MombasaBridge::Stub::AllocateMemory(::grpc::ClientContext* context, const ::mombasa::MemoryAllocateRequest& request, ::mombasa::MemoryAllocateResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::MemoryAllocateRequest, ::mombasa::MemoryAllocateResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_AllocateMemory_, context, request, response);
}

void MombasaBridge::Stub::async::AllocateMemory(::grpc::ClientContext* context, const ::mombasa::MemoryAllocateRequest* request, ::mombasa::MemoryAllocateResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::MemoryAllocateRequest, ::mombasa::MemoryAllocateResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_AllocateMemory_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::AllocateMemory(::grpc::ClientContext* context, const ::mombasa::MemoryAllocateRequest* request, ::mombasa::MemoryAllocateResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_AllocateMemory_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryAllocateResponse>* MombasaBridge::Stub::PrepareAsyncAllocateMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryAllocateRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::MemoryAllocateResponse, ::mombasa::MemoryAllocateRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_AllocateMemory_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryAllocateResponse>* MombasaBridge::Stub::AsyncAllocateMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryAllocateRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncAllocateMemoryRaw(context, request, cq);
  result->StartCall();
  return result;
}

::grpc::Status MombasaBridge::Stub::FreeMemory(::grpc::ClientContext* context, const ::mombasa::MemoryFreeRequest& request, ::mombasa::MemoryFreeResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::MemoryFreeRequest, ::mombasa::MemoryFreeResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_FreeMemory_, context, request, response);
}

void MombasaBridge::Stub::async::FreeMemory(::grpc::ClientContext* context, const ::mombasa::MemoryFreeRequest* request, ::mombasa::MemoryFreeResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::MemoryFreeRequest, ::mombasa::MemoryFreeResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_FreeMemory_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::FreeMemory(::grpc::ClientContext* context, const ::mombasa::MemoryFreeRequest* request, ::mombasa::MemoryFreeResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_FreeMemory_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryFreeResponse>* MombasaBridge::Stub::PrepareAsyncFreeMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryFreeRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::MemoryFreeResponse, ::mombasa::MemoryFreeRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_FreeMemory_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryFreeResponse>* MombasaBridge::Stub::AsyncFreeMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryFreeRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncFreeMemoryRaw(context, request, cq);
  result->StartCall();
  return result;
}

::grpc::Status MombasaBridge::Stub::WriteMemory(::grpc::ClientContext* context, const ::mombasa::MemoryWriteRequest& request, ::mombasa::MemoryWriteResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::MemoryWriteRequest, ::mombasa::MemoryWriteResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_WriteMemory_, context, request, response);
}

void MombasaBridge::Stub::async::WriteMemory(::grpc::ClientContext* context, const ::mombasa::MemoryWriteRequest* request, ::mombasa::MemoryWriteResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::MemoryWriteRequest, ::mombasa::MemoryWriteResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_WriteMemory_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::WriteMemory(::grpc::ClientContext* context, const ::mombasa::MemoryWriteRequest* request, ::mombasa::MemoryWriteResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_WriteMemory_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryWriteResponse>* MombasaBridge::Stub::PrepareAsyncWriteMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryWriteRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::MemoryWriteResponse, ::mombasa::MemoryWriteRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_WriteMemory_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryWriteResponse>* MombasaBridge::Stub::AsyncWriteMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryWriteRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncWriteMemoryRaw(context, request, cq);
  result->StartCall();
  return result;
}

::grpc::Status MombasaBridge::Stub::ReadMemory(::grpc::ClientContext* context, const ::mombasa::MemoryReadRequest& request, ::mombasa::MemoryReadResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::MemoryReadRequest, ::mombasa::MemoryReadResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_ReadMemory_, context, request, response);
}

void MombasaBridge::Stub::async::ReadMemory(::grpc::ClientContext* context, const ::mombasa::MemoryReadRequest* request, ::mombasa::MemoryReadResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::MemoryReadRequest, ::mombasa::MemoryReadResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_ReadMemory_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::ReadMemory(::grpc::ClientContext* context, const ::mombasa::MemoryReadRequest* request, ::mombasa::MemoryReadResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_ReadMemory_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryReadResponse>* MombasaBridge::Stub::PrepareAsyncReadMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryReadRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::MemoryReadResponse, ::mombasa::MemoryReadRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_ReadMemory_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::MemoryReadResponse>* MombasaBridge::Stub::AsyncReadMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryReadRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncReadMemoryRaw(context, request, cq);
  result->StartCall();
  return result;
}

::grpc::ClientReader< ::mombasa::MemoryReadResponse>* MombasaBridge::Stub::PollMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryPollRequest& request) {
  return ::grpc::internal::ClientReaderFactory< ::mombasa::MemoryReadResponse>::Create(channel_.get(), rpcmethod_PollMemory_, context, request);
}

void MombasaBridge::Stub::async::PollMemory(::grpc::ClientContext* context, const ::mombasa::MemoryPollRequest* request, ::grpc::ClientReadReactor< ::mombasa::MemoryReadResponse>* reactor) {
  ::grpc::internal::ClientCallbackReaderFactory< ::mombasa::MemoryReadResponse>::Create(stub_->channel_.get(), stub_->rpcmethod_PollMemory_, context, request, reactor);
}

::grpc::ClientAsyncReader< ::mombasa::MemoryReadResponse>* MombasaBridge::Stub::AsyncPollMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryPollRequest& request, ::grpc::CompletionQueue* cq, void* tag) {
  return ::grpc::internal::ClientAsyncReaderFactory< ::mombasa::MemoryReadResponse>::Create(channel_.get(), cq, rpcmethod_PollMemory_, context, request, true, tag);
}

::grpc::ClientAsyncReader< ::mombasa::MemoryReadResponse>* MombasaBridge::Stub::PrepareAsyncPollMemoryRaw(::grpc::ClientContext* context, const ::mombasa::MemoryPollRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncReaderFactory< ::mombasa::MemoryReadResponse>::Create(channel_.get(), cq, rpcmethod_PollMemory_, context, request, false, nullptr);
}

::grpc::Status MombasaBridge::Stub::SetTlsValue(::grpc::ClientContext* context, const ::mombasa::SetTlsValueRequest& request, ::mombasa::SetTlsValueResponse* response) {
  return ::grpc::internal::BlockingUnaryCall< ::mombasa::SetTlsValueRequest, ::mombasa::SetTlsValueResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), rpcmethod_SetTlsValue_, context, request, response);
}

void MombasaBridge::Stub::async::SetTlsValue(::grpc::ClientContext* context, const ::mombasa::SetTlsValueRequest* request, ::mombasa::SetTlsValueResponse* response, std::function<void(::grpc::Status)> f) {
  ::grpc::internal::CallbackUnaryCall< ::mombasa::SetTlsValueRequest, ::mombasa::SetTlsValueResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_SetTlsValue_, context, request, response, std::move(f));
}

void MombasaBridge::Stub::async::SetTlsValue(::grpc::ClientContext* context, const ::mombasa::SetTlsValueRequest* request, ::mombasa::SetTlsValueResponse* response, ::grpc::ClientUnaryReactor* reactor) {
  ::grpc::internal::ClientCallbackUnaryFactory::Create< ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(stub_->channel_.get(), stub_->rpcmethod_SetTlsValue_, context, request, response, reactor);
}

::grpc::ClientAsyncResponseReader< ::mombasa::SetTlsValueResponse>* MombasaBridge::Stub::PrepareAsyncSetTlsValueRaw(::grpc::ClientContext* context, const ::mombasa::SetTlsValueRequest& request, ::grpc::CompletionQueue* cq) {
  return ::grpc::internal::ClientAsyncResponseReaderHelper::Create< ::mombasa::SetTlsValueResponse, ::mombasa::SetTlsValueRequest, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(channel_.get(), cq, rpcmethod_SetTlsValue_, context, request);
}

::grpc::ClientAsyncResponseReader< ::mombasa::SetTlsValueResponse>* MombasaBridge::Stub::AsyncSetTlsValueRaw(::grpc::ClientContext* context, const ::mombasa::SetTlsValueRequest& request, ::grpc::CompletionQueue* cq) {
  auto* result =
    this->PrepareAsyncSetTlsValueRaw(context, request, cq);
  result->StartCall();
  return result;
}

MombasaBridge::Service::Service() {
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[0],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::CallRequest, ::mombasa::CallResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::CallRequest* req,
             ::mombasa::CallResponse* resp) {
               return service->CallFunction(ctx, req, resp);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[1],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::MemoryAllocateRequest, ::mombasa::MemoryAllocateResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::MemoryAllocateRequest* req,
             ::mombasa::MemoryAllocateResponse* resp) {
               return service->AllocateMemory(ctx, req, resp);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[2],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::MemoryFreeRequest, ::mombasa::MemoryFreeResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::MemoryFreeRequest* req,
             ::mombasa::MemoryFreeResponse* resp) {
               return service->FreeMemory(ctx, req, resp);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[3],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::MemoryWriteRequest, ::mombasa::MemoryWriteResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::MemoryWriteRequest* req,
             ::mombasa::MemoryWriteResponse* resp) {
               return service->WriteMemory(ctx, req, resp);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[4],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::MemoryReadRequest, ::mombasa::MemoryReadResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::MemoryReadRequest* req,
             ::mombasa::MemoryReadResponse* resp) {
               return service->ReadMemory(ctx, req, resp);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[5],
      ::grpc::internal::RpcMethod::SERVER_STREAMING,
      new ::grpc::internal::ServerStreamingHandler< MombasaBridge::Service, ::mombasa::MemoryPollRequest, ::mombasa::MemoryReadResponse>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::MemoryPollRequest* req,
             ::grpc::ServerWriter<::mombasa::MemoryReadResponse>* writer) {
               return service->PollMemory(ctx, req, writer);
             }, this)));
  AddMethod(new ::grpc::internal::RpcServiceMethod(
      MombasaBridge_method_names[6],
      ::grpc::internal::RpcMethod::NORMAL_RPC,
      new ::grpc::internal::RpcMethodHandler< MombasaBridge::Service, ::mombasa::SetTlsValueRequest, ::mombasa::SetTlsValueResponse, ::grpc::protobuf::MessageLite, ::grpc::protobuf::MessageLite>(
          [](MombasaBridge::Service* service,
             ::grpc::ServerContext* ctx,
             const ::mombasa::SetTlsValueRequest* req,
             ::mombasa::SetTlsValueResponse* resp) {
               return service->SetTlsValue(ctx, req, resp);
             }, this)));
}

MombasaBridge::Service::~Service() {
}

::grpc::Status MombasaBridge::Service::CallFunction(::grpc::ServerContext* context, const ::mombasa::CallRequest* request, ::mombasa::CallResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::AllocateMemory(::grpc::ServerContext* context, const ::mombasa::MemoryAllocateRequest* request, ::mombasa::MemoryAllocateResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::FreeMemory(::grpc::ServerContext* context, const ::mombasa::MemoryFreeRequest* request, ::mombasa::MemoryFreeResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::WriteMemory(::grpc::ServerContext* context, const ::mombasa::MemoryWriteRequest* request, ::mombasa::MemoryWriteResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::ReadMemory(::grpc::ServerContext* context, const ::mombasa::MemoryReadRequest* request, ::mombasa::MemoryReadResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::PollMemory(::grpc::ServerContext* context, const ::mombasa::MemoryPollRequest* request, ::grpc::ServerWriter< ::mombasa::MemoryReadResponse>* writer) {
  (void) context;
  (void) request;
  (void) writer;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}

::grpc::Status MombasaBridge::Service::SetTlsValue(::grpc::ServerContext* context, const ::mombasa::SetTlsValueRequest* request, ::mombasa::SetTlsValueResponse* response) {
  (void) context;
  (void) request;
  (void) response;
  return ::grpc::Status(::grpc::StatusCode::UNIMPLEMENTED, "");
}


}  // namespace mombasa

