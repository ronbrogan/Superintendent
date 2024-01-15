using Superintendent.Core.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Superintendent.Core.Remote
{
    public class ProcessAttachArgs
    {
        public int ProcessId { get; set; }
        public IRemoteProcess Process { get; set; }
    }

    public class AttachExceptionArgs
    {
        public int ProcessId { get; set; }
        public Exception? Exception { get; set; }
    }

    public interface IRemoteProcess : IDisposable, ICommandSink
    {
        public Process? Process { get; }

        public int ProcessId { get; }

        public IEnumerable<ProcessThread> Threads { get; }

        public IEnumerable<ProcessModule> Modules { get; }


        public event EventHandler<ProcessAttachArgs> ProcessAttached;

        ICommandSink GetCommandSink(string module);

        public void SetProtection(nint address, int length, MemoryProtection desiredProtection);

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite);

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release);

        public void SetTlsValue(int index, nint value);
        void SuspendAppThreads();
        void ResumeAppThreads();
    }
}
