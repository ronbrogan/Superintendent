#include <chrono>
#include <thread>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <grpcpp/grpcpp.h>
#include <grpcpp/alarm.h>

#include "mombasa_base.h"
#include "mombasa_calldata.h"
#include "mombasa.grpc.pb.h"
using namespace mombasa;

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerAsyncWriter;
using grpc::ServerBuilder;
using grpc::ServerCompletionQueue;
using grpc::ServerContext;
using grpc::Status;
using grpc::StatusCode;


// Take in the "service" instance (in this case representing an asynchronous
// server) and the completion queue "cq" used for asynchronous communication
// with the gRPC runtime.
CallData::CallData(MombasaBridge::AsyncService* service,
    ServerCompletionQueue* cq,
    std::function<void(ServerContext* context, TReq* request, ServerAsyncResponseWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc,
    std::function<Status(ServerContext* context, TReq* request, TResp* response)> implfunc)
    : ready(true), service_(service), cq_(cq), ctx_(new ServerContext()), responder_(ctx_), readfunc_(readfunc), implfunc_(implfunc) {
    // Invoke the serving logic right away.
    readfunc(ctx_, &request_, &responder_, cq_, cq_, this);
}

void CallData::Handle() {
    if (ready) {
        // Process current request
        auto status = implfunc_(ctx_, &request_, &reply_);
        responder_.Finish(reply_, status, this);
        ready = false;
    }
    else {
        // allow this instance to handle another request
        Reset();
        readfunc_(ctx_, &request_, &responder_, cq_, cq_, this);
    }
}

void CallData::Reset() {
    delete ctx_;
    ctx_ = new ServerContext();
    responder_ = ServerAsyncResponseWriter<TResp>(ctx_);
    ready = true;
}


PollingStreamCallData::PollingStreamCallData(MombasaBridge::AsyncService* service,
    ServerCompletionQueue* cq,
    std::function<void(ServerContext* context, TReq* request, ServerAsyncWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc,
    std::function<Status(ServerContext* context, TReq* request, TResp* response)> implfunc,
    std::function<PollingState(TReq* request)> pollparams)
    : state(State::NEW),
    service_(service),
    cq_(cq),
    ctx_(new ServerContext()),
    responder_(ctx_),
    readfunc_(readfunc),
    pollparams_(pollparams),
    implfunc_(implfunc),
    dequeueBuffer(std::chrono::milliseconds(1))
{

    // Invoke the serving logic right away.
    ctx_->AsyncNotifyWhenDone(this);
    readfunc(ctx_, &request_, &responder_, cq_, cq_, this);
}

void PollingStreamCallData::Handle() {
    // if this is a connection start, we'll spin off another instance to handle the connection
    if (state == State::NEW) {
        Clone();
        pollingState = pollparams_(&request_);
        state = State::READY;
    }

    // Flag for delete if the client is gone
    if (ctx_->IsCancelled() && state != State::DEAD) {
        state = State::DEAD;
        return;
    }

    // if we're ready to write, write if we can
    if (state == State::READY) {

        if (pollingState.ShouldPoll()) {
            pollingState.RecordPoll();
            TResp reply;
            auto status = implfunc_(ctx_, &request_, &reply);
            responder_.Write(reply, this);
        }
        else {
            // Use an expiring alarm to push back to the completion queue roughly before next poll should come
            auto next = pollingState.GetNextPollTime() - dequeueBuffer;
            alarm.Set(cq_, next, this);
        }
    }

    if (state == State::DONE) {
        responder_.Finish(Status::OK, this);
        state = State::DEAD;
        return;
    }

    if (state == State::DEAD) {
        Teardown();
    }
}


void PollingStreamCallData::Clone() {
    new PollingStreamCallData(service_, cq_, readfunc_, implfunc_, pollparams_);
}

void PollingStreamCallData::Teardown() {
    delete this;
}

