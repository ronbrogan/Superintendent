using Google.Protobuf;
using Mombasa;
using Superintendent.CommandSink;
using Superintendent.Core.Native;
using Superintendent.Core.Remote;
using System;
using System.Diagnostics;
using System.Linq;

namespace Superintendent.Core.CommandSink
{
    internal class RpcCommandSink : ICommandSink
    {
        private readonly RpcRemoteProcess process;
        private readonly string module;
        private nint BaseOffset => this.process.GetModuleBase(this.module).GetValueOrDefault();

        private MombasaBridge.MombasaBridgeClient? RpcBridge => process?.Bridge;

        public RpcCommandSink(RpcRemoteProcess process, string module)
        {
            this.process = process;
            this.module = module;
        }

        public nint GetBaseOffset() => this.BaseOffset;

        public void Write(nint relativeAddress, Span<byte> data)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.WriteMemory(new MemoryWriteRequest
            {
                Address = (ulong)(this.BaseOffset + relativeAddress),
                Data = ByteString.CopyFrom(data)
            });

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(Write)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(Write)}_Server", resp.DurationMicroseconds);
        }

        public void WriteAt(nint absoluteAddress, Span<byte> data)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.WriteMemory(new MemoryWriteRequest
            {
                Address = (ulong)absoluteAddress,
                Data = ByteString.CopyFrom(data)
            });

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WriteAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WriteAt)}_Server", resp.DurationMicroseconds);
        }

        public void Read(nint address, Span<byte> data)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.ReadMemory(new MemoryReadRequest
            {
                Address = (ulong)(this.BaseOffset + address),
                Count = (uint)data.Length
            });

            resp.Data.ToArray().CopyTo(data);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(Read)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(Read)}_Server", resp.DurationMicroseconds);
        }

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data)
        {
            this.Read(ptrToaddress.Value, data);
        }

        public void ReadAt(nint address, Span<byte> data)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.ReadMemory(new MemoryReadRequest
            {
                Address = (ulong)address,
                Count = (uint)data.Length
            });

            resp.Data.ToArray().CopyTo(data);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Server", resp.DurationMicroseconds);
        }

        public void SetProtection(nint address, MemoryProtection desiredProtection)
        {
            throw new NotImplementedException();
        }

        public T CallFunction<T>(nint functionPointerOffset, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
            => CallFunctionAt<T>(this.BaseOffset + functionPointerOffset, arg1, arg2, arg3, arg4);

        public T CallFunctionAt<T>(nint functionPointer, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var timer = Stopwatch.StartNew();

            if (typeof(T) != typeof(nint) && typeof(T) != typeof(IntPtr) && typeof(T) != typeof(ulong) && typeof(T) != typeof(float))
            {
                throw new ArgumentException("Type argument must be 64 bit int or float");
            }

            var callReq = new CallRequest()
            {
                FunctionPointer = (ulong)functionPointer,
                ReturnsFloat = typeof(T) == typeof(float)
            };

            if (arg1.HasValue) callReq.Args.Add(arg1.Value);
            if (arg2.HasValue) callReq.Args.Add(arg2.Value);
            if (arg3.HasValue) callReq.Args.Add(arg3.Value);
            if (arg4.HasValue) callReq.Args.Add(arg4.Value);

            var resp = this.RpcBridge.CallFunction(callReq);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(CallFunctionAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(CallFunctionAt)}_Server", resp.DurationMicroseconds);

            if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.UInt32BitsToSingle((uint)resp.Value);
            }
            else if(typeof(T) == typeof(nint) || typeof(T) == typeof(IntPtr))
            {
                return (T)(object)(IntPtr)resp.Value;
            }
            else
            {
                return (T)(object)resp.Value;
            }
        }
    }
}


