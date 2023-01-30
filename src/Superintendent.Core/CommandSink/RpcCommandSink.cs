using Google.Protobuf;
using Grpc.Core;
using Mombasa;
using Superintendent.CommandSink;
using Superintendent.Core.Native;
using Superintendent.Core.Remote;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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

        public nint GetAbsoluteAddress(nint offset) => this.BaseOffset + offset;

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

        public unsafe void Write<T>(nint relativeAddress, T data) where T : unmanaged
        {
            var bytes = new Span<byte>(&data, sizeof(T));
            this.Write(relativeAddress, bytes);
        }

        public unsafe void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged
        {
            var bytes = new Span<byte>(&data, sizeof(T));
            this.WriteAt(absoluteAddress, bytes);
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

            resp.Data.Memory.Span.CopyTo(data);

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

            resp.Data.Memory.Span.CopyTo(data);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Server", resp.DurationMicroseconds);
        }

        public unsafe void Read<T>(nint relativeAddress, out T data) where T : unmanaged
        {
            Unsafe.SkipInit(out data);
            var bytes = new Span<byte>(Unsafe.AsPointer(ref data), sizeof(T));
            this.Read(relativeAddress, bytes);
        }

        public unsafe void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged
        {
            Unsafe.SkipInit(out data);
            var bytes = new Span<byte>(Unsafe.AsPointer(ref data), sizeof(T));
            this.ReadAt(absoluteAddress, bytes);
        }

        public Task PollMemory(nint relativeAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var resp = this.RpcBridge.PollMemory(new MemoryPollRequest()
            {
                Address = (ulong)(this.BaseOffset + relativeAddress),
                Count = byteCount,
                Interval = intervalMs
            });

            return this.PollReadResponses(resp.ResponseStream, callback, token);
        }

        public Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var resp = this.RpcBridge.PollMemory(new MemoryPollRequest()
            {
                Address = (ulong)absoluteAddress,
                Count = byteCount,
                Interval = intervalMs
            });

            return this.PollReadResponses(resp.ResponseStream, callback, token);
        }

        public unsafe Task PollMemory<T>(nint relativeAddress, uint intervalMs, Action<T> callback, CancellationToken token = default)
             where T : unmanaged
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var resp = this.RpcBridge.PollMemory(new MemoryPollRequest()
            {
                Address = (ulong)(this.BaseOffset + relativeAddress),
                Count = (uint)sizeof(T),
                Interval = intervalMs
            });

            return this.PollReadResponses(resp.ResponseStream, callback, token);
        }

        public unsafe Task PollMemoryAt<T>(nint absoluteAddress, uint intervalMs, Action<T> callback, CancellationToken token = default)
             where T : unmanaged
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            var resp = this.RpcBridge.PollMemory(new MemoryPollRequest() 
            { 
                Address = (ulong)absoluteAddress, 
                Count = (uint)sizeof(T), 
                Interval = intervalMs
            });

            return this.PollReadResponses(resp.ResponseStream, callback, token);
        }

        private Task PollReadResponses(IAsyncStreamReader<MemoryReadResponse> resp, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            return Task.Run(async () =>
            {
                while (await resp.MoveNext(token))
                {
                    callback(resp.Current.Data.Memory.Span);
                }
            });
        }

        private Task PollReadResponses<T>(IAsyncStreamReader<MemoryReadResponse> resp, Action<T> callback, CancellationToken token = default)
            where T : unmanaged
        {
            return Task.Run(async () =>
            {
                while (await resp.MoveNext(token))
                {
                    callback(this.Cast<T>(resp.Current.Data));
                }
            });
        }

        private unsafe T Cast<T>(ByteString data) where T : unmanaged
        {
            fixed(byte* bytes = data.Memory.Span)
            {
                return *(T*)bytes;
            }
        }

        public void SetProtection(nint address, MemoryProtection desiredProtection)
        {
            throw new NotImplementedException();
        }

        public (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
            => CallFunctionAt<T>(this.BaseOffset + functionPointerOffset, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);

        public (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
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

            if (arg1.HasValue) callReq.Args.Add((ulong)arg1.Value);
            if (arg2.HasValue) callReq.Args.Add((ulong)arg2.Value);
            if (arg3.HasValue) callReq.Args.Add((ulong)arg3.Value);
            if (arg4.HasValue) callReq.Args.Add((ulong)arg4.Value);
            if (arg5.HasValue) callReq.Args.Add((ulong)arg5.Value);
            if (arg6.HasValue) callReq.Args.Add((ulong)arg6.Value);
            if (arg7.HasValue) callReq.Args.Add((ulong)arg7.Value);
            if (arg8.HasValue) callReq.Args.Add((ulong)arg8.Value);
            if (arg9.HasValue) callReq.Args.Add((ulong)arg9.Value);
            if (arg10.HasValue) callReq.Args.Add((ulong)arg10.Value);
            if (arg11.HasValue) callReq.Args.Add((ulong)arg11.Value);
            if (arg12.HasValue) callReq.Args.Add((ulong)arg12.Value);

            var resp = this.RpcBridge.CallFunction(callReq);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(CallFunctionAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(CallFunctionAt)}_Server", resp.DurationMicroseconds);

            var success = resp.Success;

            if(!success)
            {
                Logger.LogError($"Rpc_{nameof(CallFunctionAt)} call failure");
            }

            if (typeof(T) == typeof(float))
            {
                return (success, (T)(object)BitConverter.UInt32BitsToSingle((uint)resp.Value));
            }
            else if(typeof(T) == typeof(nint) || typeof(T) == typeof(IntPtr))
            {
                return (success, (T)(object)(IntPtr)resp.Value);
            }
            else
            {
                return (success, (T)(object)resp.Value);
            }
        }

        public void SetTlsValue(int index, nint value)
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");

            this.RpcBridge.SetTlsValue(new SetTlsValueRequest()
            {
                Index = (uint)index,
                Value = (ulong)value
            });
        }
    }
}


