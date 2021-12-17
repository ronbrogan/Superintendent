using Grpc.Net.Client;
using Mombasa;
using Superintendent.CommandSink;
using Superintendent.Core.CommandSink;
using Superintendent.Core.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Superintendent.Core.Remote
{
    public class RpcRemoteProcess : IRemoteProcess
    {
        internal MombasaBridge.MombasaBridgeClient Bridge { get; set; }
        private Process process;
        private ICommandSink processCommandSink;
        private ProcessWatcher processWatcher;
        private Dictionary<string, IntPtr> processModuleOffsets = new();

        public event EventHandler<ProcessAttachArgs> ProcessAttached;
        public event EventHandler ProcessDetached;

        public int ProcessId => this.process.Id;

        public IEnumerable<ProcessThread> Threads { get { foreach (ProcessThread t in this.process.Threads) yield return t; } }

        public RpcRemoteProcess(int pid)
        {
            this.process = Process.GetProcessById(pid);
            this.processCommandSink = this.GetCommandSink();

            this.AttachToProcess(this.process);
        }

        public RpcRemoteProcess() { }

        public async Task Attach(params string[] processNames)
        {
            processWatcher = new ProcessWatcher(processNames);
            await processWatcher.Run(p => {
                process = p;
                processCommandSink = this.GetCommandSink();
                AttachToProcess(p);
                processModuleOffsets.Clear();
            }, () => ProcessDetached?.Invoke(this, null));
        }

        public void AttachToProcess(Process proc)
        {
            Console.WriteLine($"Attaching to process: {proc.Id}");

            var needsMombasa = true;
            foreach(ProcessModule module in proc.Modules)
            {
                if(Path.GetFileName(module.FileName) == "mombasa.dll")
                {
                    needsMombasa = false;
                    break;
                }
            }

            if(needsMombasa)
            {
                var path = Environment.GetEnvironmentVariable("MombasaPath")
                ?? Path.Combine(Environment.CurrentDirectory, "mombasa.dll");

                if (!File.Exists(path))
                {
                    throw new Exception($"Unable to locate mombasa bridge dll at '{path}', specify with 'MombasaPath' environment variable");
                }

                Win32.InjectModule(proc.Id, path);
            }

            var channel = GrpcChannel.ForAddress("http://127.0.0.1:50051");
            this.Bridge = new MombasaBridge.MombasaBridgeClient(channel);

            this.ProcessAttached?.Invoke(this, new ProcessAttachArgs() { Process = this, ProcessId = proc.Id });
        }

        public ICommandSink GetCommandSink()
        {
            return new RpcCommandSink(this, string.Empty);
        }

        public ICommandSink GetCommandSink(string module)
        {
            return new RpcCommandSink(this, module);
        }

        public IntPtr? GetModuleBase(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return IntPtr.Zero;

            if(this.processModuleOffsets.TryGetValue(moduleName, out var offset))
            {
                return offset;
            }

            ProcessModule loadedModule = null;

            foreach (ProcessModule mod in this.process.Modules)
            {
                if (mod.ModuleName != null 
                    && mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    loadedModule = mod;
                    break;
                }
            }

            if (loadedModule == null)
            {
                throw new Exception($"Module {moduleName} is not loaded in target process");
            }

            this.processModuleOffsets[moduleName] = loadedModule.BaseAddress;

            return loadedModule.BaseAddress;
        }

        public nint Allocate(int length, MemoryProtection protection = MemoryProtection.ReadWrite)
        {
            var resp = this.Bridge.AllocateMemory(new MemoryAllocateRequest()
            {
                Length = (uint)length,
                Protection = (uint)protection
            });

            return (nint)resp.Address;
        }

        public void Free(nint address, int length = 0, AllocationType freeType = AllocationType.Release)
        {
            this.Bridge.FreeMemory(new MemoryFreeRequest()
            {
                Address = (ulong)address,
                Length = (uint)length,
                FreeType = (uint)freeType
            });
        }

        public void Write(nint relativeAddress, Span<byte> data) => processCommandSink.Write(relativeAddress, data);

        public void WriteAt(nint absoluteAddress, Span<byte> data) => processCommandSink.WriteAt(absoluteAddress, data);

        public void Read(nint address, Span<byte> data) => processCommandSink.Read(address, data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data) => processCommandSink.Read(ptrToaddress, data);

        public void ReadAt(nint address, Span<byte> data) => processCommandSink.ReadAt(address, data);

        public nint GetBaseOffset() => processCommandSink.GetBaseOffset();

        public T CallFunctionAt<T>(nint functionPointer, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null) => processCommandSink.CallFunctionAt<T>(functionPointer, arg1, arg2, arg3, arg4);

        public T CallFunction<T>(nint functionPointerOffset, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null) => processCommandSink.CallFunction<T>(functionPointerOffset, arg1, arg2, arg3, arg4);

        public void SetProtection(nint address, MemoryProtection desiredProtection)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
