#pragma once
#include <chrono>
#include <thread>
#include <filesystem>
#include <spdlog/spdlog.h>
#include <grpcpp/grpcpp.h>
#include <grpcpp/alarm.h>

#include "mombasa_base.h"
#include "mombasa.grpc.pb.h"
using namespace mombasa;

using grpc::Server;
using grpc::ServerAsyncResponseWriter;
using grpc::ServerAsyncWriter;
using grpc::ServerBuilder;
using grpc::CompletionQueue;
using grpc::ServerCompletionQueue;
using grpc::ServerContext;
using grpc::Status;
using grpc::StatusCode;

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
        }
        else {
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

template <typename TReq, typename TResp>
class PollingStreamCallData : public CallDataBase {
private:
    enum State { NEW, READY, DONE, DYING, DEAD };
    State state;
    // The means of communication with the gRPC runtime for an asynchronous
    // server.
    MombasaBridge::AsyncService* service_;
    // The producer-consumer queue where for asynchronous server notifications.
    ServerCompletionQueue* cq_;
    // Context for the rpc, allowing to tweak aspects of it such as the use
    // of compression, authentication, as well as to send metadata back to the
    // client.
    ServerContext* ctx_;

    PollingState pollingState;

    TReq request_;
    ServerAsyncWriter<TResp> responder_;

    std::function<void(ServerContext* context, TReq* request, ServerAsyncWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc_;
    std::function<PollingState(TReq* request)> pollparams_;
    std::function<Status(ServerContext* context, PollingState* pollState, TReq* request, TResp* response)> implfunc_;
    grpc::Alarm alarm;
    std::chrono::milliseconds dequeueBuffer;

public:

    PollingStreamCallData(MombasaBridge::AsyncService* service,
        ServerCompletionQueue* cq,
        std::function<void(ServerContext* context, TReq* request, ServerAsyncWriter<TResp>* response, CompletionQueue* new_call_cq, ServerCompletionQueue* notification_cq, void* tag)> readfunc,
        std::function<Status(ServerContext* context, PollingState* pollState, TReq* request, TResp* response)> implfunc,
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

    void Handle() override {
        // if this is a connection start, we'll spin off another instance to handle the connection
        if (state == State::NEW) {
            Clone();
            pollingState = pollparams_(&request_);
            spdlog::info("Poller {0:x} starting, scheduling every {1}ms", (uint64)this, pollingState.GetInterval());
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
                auto status = implfunc_(ctx_, &pollingState, &request_, &reply);

                if (status.error_code() == StatusCode::OK) {
                    responder_.Write(reply, this);
                }
                else {
                    responder_.Finish(status, this);
                    state = State::DYING;
                    return;
                }
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

        if (state == State::DYING) {
            state = State::DEAD;
            return;
        }

        if (state == State::DEAD) {
            Teardown();
        }
    }


    void Clone() {
        new PollingStreamCallData(service_, cq_, readfunc_, implfunc_, pollparams_);
    }

    void Teardown() {
        spdlog::info("Poller {0:x} stopping", (uint64)this);
        delete this;
    }
};
