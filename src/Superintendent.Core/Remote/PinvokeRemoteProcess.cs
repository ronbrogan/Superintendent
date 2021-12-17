using Superintendent.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Superintendent.Core.Remote
{
    public sealed class PinvokeRemoteProcess : IRemoteProcess
    {
        private Process process;
        private IntPtr processHandle;
        private bool disposedValue;
        private ICommandSink processCommandSink;

        public event EventHandler<ProcessAttachArgs> ProcessAttached;

        public int ProcessId => this.process.Id;

        public IEnumerable<ProcessThread> Threads { get { foreach (ProcessThread t in this.process.Threads) yield return t; } }

        public PinvokeRemoteProcess(int processId)
        {
            this.process = Process.GetProcessById(processId);
            this.processHandle = Win32.OpenProcess(AccessPermissions.AllAccess, false, processId);
            this.processCommandSink = this.GetCommandSink();
            this.ProcessAttached?.Invoke(this, new ProcessAttachArgs() { Process = this, ProcessId = processId });
        }

        public bool InjectModule(string pathToModule)
        {
            return Win32.InjectModule(this.process.Id, this.processHandle, pathToModule);
        }

        public ICommandSink GetCommandSink()
        {
            return new PinvokeCommandSink(this.processHandle, 0);
        }

        public ICommandSink GetCommandSink(string module)
        {
            ProcessModule loadedModule = null;

            foreach(ProcessModule mod in this.process.Modules)
            {
                if(mod.ModuleName.Equals(module, StringComparison.OrdinalIgnoreCase))
                {
                    loadedModule = mod;
                    break;
                }
            }

            if(loadedModule == null)
            {
                throw new Exception($"Module {module} is not loading target process");
            }

            return new PinvokeCommandSink(this.processHandle, loadedModule.BaseAddress);
        }

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite)
        {
            return Win32.VirtualAllocEx(this.processHandle, IntPtr.Zero, (uint)length, AllocationType.Commit, protection);
        }

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release)
        {
            Win32.VirtualFreeEx(this.processHandle, address, (uint)length, freeType);
        }

        public void SetProtection(nint address, MemoryProtection desiredProtection)
        {
            throw new NotImplementedException();
        }

        // Implement ICommandSink through 0-offset command sink instance
        public void Write(nint relativeAddress, Span<byte> data) => this.processCommandSink.Write(relativeAddress, data);

        public void WriteAt(nint absoluteAddress, Span<byte> data) => this.processCommandSink.Write(absoluteAddress, data);

        public void Read(nint address, Span<byte> data) => this.processCommandSink.Read(address, data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data) => this.processCommandSink.Read(ptrToaddress, data);

        public void ReadAt(nint address, Span<byte> data) => this.processCommandSink.ReadAt(address, data);

        public nint GetBaseOffset() => 0;


        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                Win32.CloseHandle(this.processHandle);
                disposedValue = true;
            }
        }

        ~PinvokeRemoteProcess()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public T CallFunctionAt<T>(nint functionPointer, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
        {
            throw new NotImplementedException();
        }

        public T CallFunction<T>(nint functionPointerOffset, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
        {
            throw new NotImplementedException();
        }
    }
}
