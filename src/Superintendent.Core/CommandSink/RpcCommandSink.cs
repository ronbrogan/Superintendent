using Google.Protobuf;
using Grpc.Core;
using Mombasa;
using Superintendent.Core.Native;
using Superintendent.Core.Remote;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

#region Address Read/Write

        public unsafe void Read<T>(nint relativeAddress, out T data) where T : unmanaged
        {
            data = default;
            this.ReadSpan(relativeAddress, MemoryMarshal.CreateSpan(ref data, 1));
        }

        public unsafe void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged
        {
            data = default;
            this.ReadSpanAt(absoluteAddress, MemoryMarshal.CreateSpan(ref data, 1));
        }

        public unsafe void Write<T>(nint relativeAddress, T data) where T : unmanaged
        {
            this.WriteSpan(relativeAddress, MemoryMarshal.CreateReadOnlySpan(ref data, 1));
        }

        public unsafe void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged
        {
            this.WriteSpanAt(absoluteAddress, MemoryMarshal.CreateReadOnlySpan(ref data, 1));
        }

        public void ReadSpan<T>(nint relativeAddress, Span<T> data) where T : unmanaged
        {
            this.ReadSpanAt(this.BaseOffset + relativeAddress, data);
        }

        public void WriteSpan<T>(nint relativeAddress, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.WriteSpanAt(this.BaseOffset + relativeAddress, data);
        }

        public void ReadSpanAt<T>(nint address, Span<T> data) where T : unmanaged
        {
            this.EnsureRpcBridge();

            var dataBytes = MemoryMarshal.AsBytes(data);

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.ReadMemory(new MemoryReadRequest
            {
                Address = (ulong)address,
                Count = (uint)dataBytes.Length
            });

            resp.Data.Memory.Span.CopyTo(dataBytes);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadAt)}_Server", resp.DurationMicroseconds);
        }

        public void WriteSpanAt<T>(nint absoluteAddress, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.EnsureRpcBridge();

            var dataBytes = MemoryMarshal.AsBytes(data);

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.WriteMemory(new MemoryWriteRequest
            {
                Address = (ulong)absoluteAddress,
                Data = ByteString.CopyFrom(dataBytes)
            });

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WriteAt)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WriteAt)}_Server", resp.DurationMicroseconds);
        }


#endregion

#region Pointer Read/Write

        public unsafe void ReadPointer<T>(Ptr<T> relativePointer, out T data) where T : unmanaged
        {
            data = default;
            var dataBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
            ReadPointerBytes(relativePointer, dataBytes, addBaseOffset: true);
        }

        public void ReadPointerSpan<T>(Ptr<T> relativePointer, Span<T> data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(data);
            ReadPointerBytes(relativePointer, dataBytes, addBaseOffset: true);
        }

        public void ReadAbsolutePointer<T>(Ptr<T> absolutePointer, out T data) where T : unmanaged
        {
            data = default;
            var dataBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
            ReadPointerBytes(absolutePointer, dataBytes);
        }

        public void ReadAbsolutePointerSpan<T>(Ptr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(data);
            ReadPointerBytes(absolutePointer, dataBytes);
        }

        public void WritePointer<T>(Ptr<T> relativePointer, T data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
            WritePointerBytes(relativePointer, dataBytes, addBaseOffset: true);
        }

        public void WritePointerSpan<T>(Ptr<T> relativePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(data);
            WritePointerBytes(relativePointer, dataBytes, addBaseOffset: true);
        }

        public void WriteAbsolutePointer<T>(Ptr<T> absolutePointer, T data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
            WritePointerBytes(absolutePointer, dataBytes);
        }

        public void WriteAbsolutePointerSpan<T>(Ptr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            var dataBytes = MemoryMarshal.AsBytes(data);
            WritePointerBytes(absolutePointer, dataBytes);
        }

        public void ReadPointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged
        {
            this.ReadAbsolutePointer(absolutePointer.AsPtr(), out data);
        }

        public void ReadPointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            this.ReadAbsolutePointerSpan(absolutePointer.AsPtr(), data);
        }

        public void ReadAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged
        {
            this.ReadAbsolutePointer(absolutePointer.AsPtr(), out data);
        }

        public void ReadAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            this.ReadAbsolutePointerSpan(absolutePointer.AsPtr(), data);
        }

        public void WritePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged
        {
            this.WriteAbsolutePointer(absolutePointer.AsPtr(), data);
        }

        public void WritePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.WriteAbsolutePointerSpan(absolutePointer.AsPtr(), data);
        }

        public void WriteAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged
        {
            this.WriteAbsolutePointer(absolutePointer.AsPtr(), data);
        }

        public void WriteAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            this.WriteAbsolutePointerSpan(absolutePointer.AsPtr(), data);
        }

        private void ReadPointerBytes<T>(Ptr<T> pointer, Span<byte> output, bool addBaseOffset = false) where T : unmanaged
        {
            this.EnsureRpcBridge();

            var req = new PointerReadRequest()
            {
                Size = (uint)output.Length,
                Base = (ulong)(pointer.Base + (addBaseOffset ? this.BaseOffset : 0)),
            };

            foreach (var link in pointer.Chain)
            {
                req.Chain.Add((uint)link);
            }

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.ReadPointer(req);
            resp.Data.Memory.Span.CopyTo(output);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadPointerBytes)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(ReadPointerBytes)}_Server", resp.DurationMicroseconds);
        }

        private void WritePointerBytes<T>(Ptr<T> pointer, ReadOnlySpan<byte> input, bool addBaseOffset = false) where T : unmanaged
        {
            this.EnsureRpcBridge();

            var req = new PointerWriteRequest()
            {
                Base = (ulong)(pointer.Base + (addBaseOffset ? this.BaseOffset : 0)),
                Data = ByteString.CopyFrom(input)
            };

            foreach (var link in pointer.Chain)
            {
                req.Chain.Add((uint)link);
            }

            var timer = Stopwatch.StartNew();
            var resp = this.RpcBridge.WritePointer(req);

            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WritePointerBytes)}_Client", (ulong)(timer.Elapsed.TotalMilliseconds * 1000));
            Tracer.Instance.TraceMicroseconds($"Rpc_{nameof(WritePointerBytes)}_Server", resp.DurationMicroseconds);
        }

        #endregion

#region Polling

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

#endregion

        public (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
            => CallFunctionAt<T>(this.BaseOffset + functionPointerOffset, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);

        public (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged
        {
            this.EnsureRpcBridge();

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
            this.EnsureRpcBridge();

            this.RpcBridge.SetTlsValue(new SetTlsValueRequest()
            {
                Index = (uint)index,
                Value = (ulong)value
            });
        }

        public void SetThreadLocalPointer(nint value)
        {
            this.EnsureRpcBridge();

            this.RpcBridge.SetThreadLocalPointer(new SetThreadLocalPointerRequest()
            {
                Value = (ulong)value
            });
        }

        public nint GetThreadLocalPointer()
        {
            this.EnsureRpcBridge();

            var resp = this.RpcBridge.GetThreadLocalPointer(new GetThreadLocalPointerRequest());
            return (nint)resp.Value;
        }

        private void EnsureRpcBridge()
        {
            if (this.RpcBridge == null)
                throw new Exception("Process is not attached");
        }
    }
}


