using Grpc.Net.Client;
using Mombasa;
using Superintendent.CommandSink;
using Superintendent.Core.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public void Attach(params string[] processNames)
        {
            processWatcher = new ProcessWatcher(processNames);
            processWatcher.Run(
                p => {
                    process = p;
                    processCommandSink = this.GetCommandSink();
                    AttachToProcess(p);
                    processModuleOffsets.Clear();
                },
                () => DetachFromProcess(),
                (i,e) => HandleAttachFailure(i,e));
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

        public void DetachFromProcess()
        {
            if (this.process == null || this.process.HasExited) return;

            if (this.injectedMombasa)
            {
                Win32.EjectModule(this.process.Id, MombasaPath);
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

        public void Write(nint relativeAddress, Span<byte> data) => processCommandSink?.Write(relativeAddress, data);

        public void WriteAt(nint absoluteAddress, Span<byte> data) => processCommandSink?.WriteAt(absoluteAddress, data);

        public void Write<T>(nint relativeAddress, T data) where T : unmanaged => processCommandSink?.Write(relativeAddress, data);

        public void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged => processCommandSink?.WriteAt(absoluteAddress, data);

        public void Read(nint address, Span<byte> data) => processCommandSink?.Read(address, data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data) => processCommandSink?.Read(ptrToaddress, data);

        public void ReadAt(nint address, Span<byte> data) => processCommandSink?.ReadAt(address, data);

        public void Read<T>(nint address, out T data) where T : unmanaged { data = default; processCommandSink?.Read(address, out data); }

        public void ReadAt<T>(nint address, out T data) where T : unmanaged { data = default; processCommandSink?.ReadAt(address, out data); }

        public nint GetBaseOffset() => processCommandSink?.GetBaseOffset() ?? -1;

        public (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T: unmanaged
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

        public void SetProtection(nint address, MemoryProtection desiredProtection)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.processWatcher?.Dispose();
            this.process?.Dispose();
        }
    }
}
