using Superintendent.CommandSink;
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

    public interface IRemoteProcess : IDisposable, ICommandSink
    {
        public int ProcessId { get; }

        public event EventHandler<ProcessAttachArgs> ProcessAttached;

        ICommandSink GetCommandSink(string module);

        public void SetProtection(nint address, MemoryProtection desiredProtection);

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite);

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release);

        public IEnumerable<ProcessThread> Threads { get; }
    }
}
