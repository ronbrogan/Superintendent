#pragma once

#include <chrono>
#include <grpcpp/grpcpp.h>
#include <google/protobuf/stubs/port.h>
using namespace google::protobuf;

class CallDataBase {
public:
    virtual void Handle() = 0;
};


class PollingState {
private:
    std::chrono::milliseconds m_interval;
    int32 m_count;
    uint64 m_actualCount;
    std::chrono::time_point<std::chrono::system_clock> m_lastPoll;
public:
    // default ctor with 0 count to prevent polling
    PollingState() : m_count(0), m_actualCount(0) {}

    PollingState(uint32 interval, int32 count = -1)
        : m_interval(std::chrono::milliseconds((interval <= 0) ? 1 : interval)),
        m_count(count),
        m_actualCount(0),
        m_lastPoll(std::chrono::system_clock::now() - m_interval) {}

    bool ShouldPoll() {
        if (m_count > 0 && m_actualCount >= m_count) {
            return false;
        }

        auto now = std::chrono::system_clock::now();

        return (now > (m_lastPoll + m_interval));
    }

    void RecordPoll() {
        m_lastPoll = std::chrono::system_clock::now();
        m_actualCount++;
    }

    std::chrono::time_point<std::chrono::system_clock> GetNextPollTime() {
        return m_lastPoll + m_interval;
    }

    uint64 GetPollCount() {
        return m_actualCount;
    }

    uint32 GetInterval() {
        return m_interval.count();
    }
};
