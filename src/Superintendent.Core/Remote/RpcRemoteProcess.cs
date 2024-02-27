using Grpc.Net.Client;
using Mombasa;
using Superintendent.Core.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.Remote
{
    public class RpcRemoteProcess : IRemoteProcess
    {
        public static string MombasaPath => Environment.GetEnvironmentVariable("MombasaPath")
                ?? Path.Combine(Environment.CurrentDirectory, "mombasa.dll");

        internal MombasaBridge.MombasaBridgeClient? Bridge { get; set; }
        private Process? process;
        private ICommandSink? processCommandSink;
        private ProcessWatcher? processWatcher;
        private Dictionary<string, IntPtr> processModuleOffsets = new();

        public event EventHandler<ProcessAttachArgs>? ProcessAttached;
        public event EventHandler? ProcessDetached;
        public event EventHandler<AttachExceptionArgs>? AttachException;

        public Process? Process => this.process;

        public int ProcessId => this.Process?.Id ?? -1;

        public IEnumerable<ProcessThread> Threads => this.Process?.Threads.Cast<ProcessThread>() ?? Enumerable.Empty<ProcessThread>();

        public IEnumerable<ProcessModule> Modules => this.Process?.Modules.Cast<ProcessModule>() ?? Enumerable.Empty<ProcessModule>();

        private bool injectedMombasa = false;

        public RpcRemoteProcess(int pid)
        {
            this.process = Process.GetProcessById(pid);
            this.processCommandSink = this.GetCommandSink();

            this.AttachToProcess(this.process);
        }

        public RpcRemoteProcess() { }

        public void Attach(Func<Process, bool>? attachGuard = null, params string[] processNames)
        {
            processWatcher = new ProcessWatcher(processNames);
            processWatcher.Run(
                p => {
                    if (attachGuard?.Invoke(p) ?? true)
                    {
                        process = p;
                        processCommandSink = this.GetCommandSink();
                        AttachToProcess(p);
                        processModuleOffsets.Clear();
                        return true;
                    }

                    return false;
                },
                () => DetachFromProcess(),
                (i, e) => HandleAttachFailure(i, e));
        }

        public void AttachToProcess(Process proc)
        {
            Logger.LogInformation($"Attaching to process: {proc.Id}");

            var hasMombasa = proc.Modules.Cast<ProcessModule>().Any(m => m.ModuleName == "mombasa.dll");

            if (!hasMombasa)
            {
                var path = MombasaPath;

                if (!File.Exists(path))
                {
                    throw new Exception($"Unable to locate mombasa bridge dll at '{path}', specify with 'MombasaPath' environment variable");
                }

                this.injectedMombasa = Win32.InjectModule(proc.Id, path);
            }

            var channel = GrpcChannel.ForAddress("http://127.0.0.1:50051");
            this.Bridge = new MombasaBridge.MombasaBridgeClient(channel);

            this.ProcessAttached?.Invoke(this, new ProcessAttachArgs() { Process = this, ProcessId = proc.Id });
        }

        public void EjectMombasa()
        {
            if (this.process != null)
                Win32.EjectModule(this.process.Id, MombasaPath);
        }

        public void DetachFromProcess()
        {
            if (this.injectedMombasa)
            {
                this.EjectMombasa();
                this.injectedMombasa = false;
            }

            this.ProcessDetached?.Invoke(this, null!);
        }

        private void HandleAttachFailure(int i, Exception? e)
        {
            this.AttachException?.Invoke(this, new AttachExceptionArgs()
            {
                ProcessId = i,
                Exception = e
            });
        }

        public ICommandSink GetCommandSink()
        {
            return new RpcCommandSink(this, this.process?.MainModule?.ModuleName);
        }

        public ICommandSink GetCommandSink(string module)
        {
            return new RpcCommandSink(this, module);
        }

        public nint GetAbsoluteAddress(nint offset) => (this.process?.MainModule?.BaseAddress ?? IntPtr.Zero) + offset;

        public IntPtr? GetModuleBase(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return IntPtr.Zero;

            if (this.processModuleOffsets.TryGetValue(moduleName, out var offset))
            {
                return offset;
            }

            var loadedModule = this.process?.Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => moduleName.Equals(m.ModuleName, StringComparison.OrdinalIgnoreCase));

            if (loadedModule == null)
            {
                throw new Exception($"Module {moduleName} is not loaded in target process");
            }

            this.processModuleOffsets[moduleName] = loadedModule.BaseAddress;

            return loadedModule.BaseAddress;
        }

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite)
        {
            if (this.Bridge == null)
                throw new Exception("Process is not attached");

            var resp = this.Bridge.AllocateMemory(new MemoryAllocateRequest()
            {
                Length = (uint)length,
                Protection = (uint)protection
            });

            return (nint)resp.Address;
        }

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release)
        {
            if (this.Bridge == null)
                throw new Exception("Process is not attached");

            this.Bridge.FreeMemory(new MemoryFreeRequest()
            {
                Address = (ulong)address,
                Length = (uint)length,
                FreeType = (uint)freeType
            });
        }

        public void SetProtection(nint address, int length, MemoryProtection desiredProtection)
        {
            if (this.Bridge == null)
                throw new Exception("Process is not attached");

            var resp = this.Bridge.ProtectMemory(new MemoryProtectRequest()
            {
                Address = (ulong)address,
                Length = (uint)length,
                Protection = (uint)desiredProtection
            });
        }

        public void SuspendAppThreads()
        {
            if (this.Bridge == null)
                throw new Exception("Process is not attached");

            var resp = this.Bridge.PauseAppThreads(new PauseAppThreadsRequest());

            var suspendCounts = string.Join(", ", resp.ThreadSuspendCounts.Select(kv => $"TID:{kv.Key} ({kv.Value})"));
            Logger.LogInformation($"ThreadSuspend complete, suspend counts before op: {suspendCounts}");
        }

        public void ResumeAppThreads()
        {
            if (this.Bridge == null)
                throw new Exception("Process is not attached");

            var resp = this.Bridge.ResumeAppThreads(new ResumeAppThreadsRequest());

            var suspendCounts = string.Join(", ", resp.ThreadSuspendCounts.Select(kv => $"TID:{kv.Key} ({kv.Value})"));
            Logger.LogInformation($"ThreadResume complete, suspend counts before op: {suspendCounts}");
        }

        public void WriteSpan<T>(nint relativeAddress, ReadOnlySpan<T> data) where T : unmanaged => processCommandSink?.WriteSpan<T>(relativeAddress, data);

        public void WriteSpanAt<T>(nint absoluteAddress, ReadOnlySpan<T> data) where T : unmanaged => processCommandSink?.WriteSpanAt<T>(absoluteAddress, data);

        public void Write<T>(nint relativeAddress, T data) where T : unmanaged => processCommandSink?.Write(relativeAddress, data);

        public void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged => processCommandSink?.WriteAt(absoluteAddress, data);

        public void ReadSpan<T>(nint address, Span<T> data) where T : unmanaged => processCommandSink?.ReadSpan<T>(address, data);

        public void ReadSpanAt<T>(nint address, Span<T> data) where T : unmanaged => processCommandSink?.ReadSpanAt<T>(address, data);

        public void Read<T>(nint address, out T data) where T : unmanaged { data = default; processCommandSink?.Read(address, out data); }

        public void ReadAt<T>(nint address, out T data) where T : unmanaged { data = default; processCommandSink?.ReadAt(address, out data); }

        public nint GetBaseOffset() => processCommandSink?.GetBaseOffset() ?? -1;

        public (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
        {
            if (processCommandSink == null) return (false, default(T));

            return processCommandSink.CallFunctionAt<T>(functionPointer, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
        {
            if (processCommandSink == null) return (false, default(T));

            return processCommandSink.CallFunction<T>(functionPointerOffset, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }

        public void SetTlsValue(int index, nint value) => processCommandSink?.SetTlsValue(index, value);

        public Task PollMemory(nint relativeAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
            => this.processCommandSink?.PollMemory(relativeAddress, intervalMs, byteCount, callback, token) ?? Task.CompletedTask;

        public Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
            => this.processCommandSink?.PollMemoryAt(absoluteAddress, intervalMs, byteCount, callback, token) ?? Task.CompletedTask;

        public Task PollMemory<T>(nint relativeAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
            => this.processCommandSink?.PollMemory<T>(relativeAddress, intervalMs, callback, token) ?? Task.CompletedTask;

        public Task PollMemoryAt<T>(nint absoluteAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
            => this.processCommandSink?.PollMemoryAt<T>(absoluteAddress, intervalMs, callback, token) ?? Task.CompletedTask;

        public void Dispose()
        {
            this.EjectMombasa();
            this.processWatcher?.Dispose();
            this.process?.Dispose();
        }

        public void SetThreadLocalPointer(nint value)
        {
            this.processCommandSink?.SetThreadLocalPointer(value);
        }

        public nint GetThreadLocalPointer() => this.processCommandSink?.GetThreadLocalPointer() ?? 0;

        public void ReadPointer<T>(Ptr<T> relativePointer, out T data) where T : unmanaged { data = default; this.processCommandSink?.ReadPointer(relativePointer, out data); }

        public void ReadPointerSpan<T>(Ptr<T> relativePointer, Span<T> data) where T : unmanaged { data = default; this.processCommandSink?.ReadPointerSpan(relativePointer, data); }

        public void ReadAbsolutePointer<T>(Ptr<T> absolutePointer, out T data) where T : unmanaged { data = default; this.processCommandSink?.ReadAbsolutePointer(absolutePointer, out data); }

        public void ReadAbsolutePointerSpan<T>(Ptr<T> absolutePointer, Span<T> data) where T : unmanaged { data = default; this.processCommandSink?.ReadAbsolutePointerSpan(absolutePointer, data); }

        public void WritePointer<T>(Ptr<T> relativePointer, T data) where T : unmanaged => this.processCommandSink?.WritePointer(relativePointer, data);

        public void WritePointerSpan<T>(Ptr<T> relativePointer, ReadOnlySpan<T> data) where T : unmanaged => this.processCommandSink?.WritePointerSpan(relativePointer, data);

        public void WriteAbsolutePointer<T>(Ptr<T> absolutePointer, T data) where T : unmanaged => this.processCommandSink?.WriteAbsolutePointer(absolutePointer, data);

        public void WriteAbsolutePointerSpan<T>(Ptr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged => this.processCommandSink?.WriteAbsolutePointerSpan(absolutePointer, data);

        public void ReadPointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged { data = default; this.processCommandSink?.ReadPointer(absolutePointer, out data); }

        public void ReadPointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged => this.processCommandSink?.ReadPointerSpan(absolutePointer, data);

        public void ReadAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged { data = default; this.processCommandSink?.ReadAbsolutePointer(absolutePointer, out data); }

        public void ReadAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged => this.processCommandSink?.ReadAbsolutePointerSpan(absolutePointer, data);

        public void WritePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged => this.processCommandSink?.WritePointer(absolutePointer, data);

        public void WritePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged => this.processCommandSink?.WritePointerSpan(absolutePointer, data);

        public void WriteAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged => this.processCommandSink?.WriteAbsolutePointer(absolutePointer, data);

        public void WriteAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged => this.processCommandSink?.WriteAbsolutePointerSpan(absolutePointer, data);
    }
}
