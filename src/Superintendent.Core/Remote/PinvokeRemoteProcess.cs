using Superintendent.Core.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.Remote
{
    public sealed class PinvokeRemoteProcess : IRemoteProcess
    {
        private Process process;
        private IntPtr processHandle;
        private bool disposedValue;
        private ICommandSink processCommandSink;
        private RpcRemoteProcess? rpcConnection = null;

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

        public RpcRemoteProcess GetRpcConnection()
        {
            if (this.rpcConnection == null)
                this.rpcConnection = new RpcRemoteProcess(this.process.Id);
            
            return this.rpcConnection;
        }

        public bool InjectModule(string pathToModule)
        {
            return Win32.InjectModule(this.process.Id, this.processHandle, pathToModule);
        }

        public ICommandSink GetCommandSink()
        {
            return new PinvokeCommandSink(this.processHandle, (this.process?.MainModule?.BaseAddress ?? IntPtr.Zero));
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

        public nint GetAbsoluteAddress(nint offset) => (this.process?.MainModule?.BaseAddress ?? IntPtr.Zero) + offset;

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite)
        {
            return Win32.VirtualAllocEx(this.processHandle, IntPtr.Zero, (uint)length, AllocationType.Commit, protection);
        }

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release)
        {
            Win32.VirtualFreeEx(this.processHandle, address, (uint)length, freeType);
        }

        public void SetProtection(nint address, int length, MemoryProtection desiredProtection)
        {
            Win32.VirtualProtectEx(this.processHandle, address, (uint)length, desiredProtection, out _);
        }

        // Implement ICommandSink through 0-offset command sink instance
        public void WriteSpan<T>(nint relativeAddress, ReadOnlySpan<T> data) where T: unmanaged => this.processCommandSink.WriteSpan<T>(relativeAddress, data);

        public void WriteSpanAt<T>(nint absoluteAddress, ReadOnlySpan<T> data) where T : unmanaged => this.processCommandSink.WriteSpanAt<T>(absoluteAddress, data);

        public void Write<T>(nint relativeAddress, T data) where T : unmanaged => this.processCommandSink.Write(relativeAddress, data);

        public void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged => this.processCommandSink.WriteAt(absoluteAddress, data);

        public void ReadSpan<T>(nint address, Span<T> data) where T : unmanaged => this.processCommandSink.ReadSpan<T>(address, data);

        public void Read<T>(nint relativeAddress, out T data) where T : unmanaged => this.processCommandSink.Read(relativeAddress, out data);
        
        public void ReadSpanAt<T>(nint address, Span<T> data) where T : unmanaged => this.processCommandSink.ReadSpanAt<T>(address, data);

        public void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged => this.processCommandSink.ReadAt(absoluteAddress, out data);

        public nint GetBaseOffset() => this.processCommandSink.GetBaseOffset();


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

        public void SetThreadLocalPointer(nint value)
        {
            throw new NotSupportedException();
        }

        public nint GetThreadLocalPointer()
        {
            throw new NotSupportedException();
        }

        public Task PollMemory(nint relativeAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
            => this.processCommandSink.PollMemory(relativeAddress, intervalMs, byteCount, callback, token);

        public Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
            => this.processCommandSink.PollMemoryAt(absoluteAddress, intervalMs, byteCount, callback, token);

        public Task PollMemory<T>(nint relativeAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
            => this.processCommandSink.PollMemory<T>(relativeAddress, intervalMs, callback, token);

        public Task PollMemoryAt<T>(nint absoluteAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
            => this.processCommandSink.PollMemoryAt<T>(absoluteAddress, intervalMs, callback, token);


        public void ReadPointer<T>(Ptr<T> relativePointer, out T data) where T : unmanaged
        {
            this.processCommandSink.ReadPointer<T>(relativePointer, out data);
        }

        public void ReadPointerSpan<T>(Ptr<T> relativePointer, Span<T> data) where T : unmanaged
        {
            this.processCommandSink.ReadPointerSpan<T>(relativePointer, data);
        }

        public void ReadAbsolutePointer<T>(Ptr<T> absolutePointer, out T data) where T : unmanaged
        {
            this.processCommandSink.ReadAbsolutePointer<T>(absolutePointer, out data);
        }

        public void ReadAbsolutePointerSpan<T>(Ptr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            this.processCommandSink.ReadAbsolutePointerSpan<T>(absolutePointer, data);
        }

        public void WritePointer<T>(Ptr<T> relativePointer, T data) where T : unmanaged
        {
            this.processCommandSink.WritePointer<T>(relativePointer, data);
        }

        public void WritePointerSpan<T>(Ptr<T> relativePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.processCommandSink.WritePointerSpan<T>(relativePointer, data);
        }

        public void WriteAbsolutePointer<T>(Ptr<T> absolutePointer, T data) where T : unmanaged
        {
            this.processCommandSink.WriteAbsolutePointer<T>(absolutePointer, data);
        }

        public void WriteAbsolutePointerSpan<T>(Ptr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.processCommandSink.WriteAbsolutePointerSpan<T>(absolutePointer, data);
        }

        public void ReadPointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged
        {
            processCommandSink.ReadPointer(absolutePointer, out data);
        }

        public void ReadPointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            processCommandSink.ReadPointerSpan(absolutePointer, data);
        }

        public void ReadAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged
        {
            processCommandSink.ReadAbsolutePointer(absolutePointer, out data);
        }

        public void ReadAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            processCommandSink.ReadAbsolutePointerSpan(absolutePointer, data);
        }

        public void WritePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged
        {
            processCommandSink.WritePointer(absolutePointer, data);
        }

        public void WritePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            processCommandSink.WritePointerSpan(absolutePointer, data);
        }

        public void WriteAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged
        {
            processCommandSink.WriteAbsolutePointer(absolutePointer, data);
        }

        public void WriteAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            processCommandSink.WriteAbsolutePointerSpan(absolutePointer, data);
        }

        public void SuspendAppThreads()
        {
            foreach(var thread in this.Threads)
            {
                var thandle = Win32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                Win32.SuspendThread(thandle);

                Win32.CloseHandle(thandle);
            }
        }

        public void ResumeAppThreads()
        {
            foreach (var thread in this.Threads)
            {
                var thandle = Win32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);

                Win32.ResumeThread(thandle);

                Win32.CloseHandle(thandle);
            }
        }
    }
}
