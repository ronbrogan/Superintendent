using Superintendent.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Superintendent.Core.Remote
{
    public sealed class PinvokeRemoteProcess : IRemoteProcess
    {
        private Process process;
        private IntPtr processHandle;
        private bool disposedValue;
        private ICommandSink processCommandSink;

        public event EventHandler<ProcessAttachArgs>? ProcessAttached;

        public Process Process => this.process;

        public int ProcessId => this.Process?.Id ?? -1;

        public IEnumerable<ProcessThread> Threads => this.Process?.Threads.Cast<ProcessThread>() ?? Enumerable.Empty<ProcessThread>();

        public IEnumerable<ProcessModule> Modules => this.Process?.Modules.Cast<ProcessModule>() ?? Enumerable.Empty<ProcessModule>();


        public PinvokeRemoteProcess(int processId)
        {
            this.process = Process.GetProcessById(processId);
            this.processHandle = Win32.OpenProcess(AccessPermissions.ProcessAllAccess, false, processId);
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
            ProcessModule? loadedModule = null;

            foreach(ProcessModule mod in this.process.Modules)
            {
                if(module.Equals(mod.ModuleName, StringComparison.OrdinalIgnoreCase))
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

        public void Write<T>(nint relativeAddress, T data) where T : unmanaged => this.processCommandSink.Write(relativeAddress, data);

        public void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged => this.processCommandSink.Write(absoluteAddress, data);

        public void Read(nint address, Span<byte> data) => this.processCommandSink.Read(address, data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data) => this.processCommandSink.Read(ptrToaddress, data);

        public void ReadAt(nint address, Span<byte> data) => this.processCommandSink.ReadAt(address, data);

        public void Read<T>(nint relativeAddress, out T data) where T : unmanaged => this.processCommandSink.ReadAt(relativeAddress, out data);

        public void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged => this.processCommandSink.ReadAt(absoluteAddress, out data);

        public nint GetBaseOffset() => 0;


        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.process.Dispose();
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

        public (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
        {
            throw new NotSupportedException();
        }

        public (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
        {
            throw new NotSupportedException();
        }

        public void SetTlsValue(int index, nint value)
        {
            throw new NotSupportedException();
        }
    }
}
