using Superintendent.Core;
using Superintendent.Core.Native;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.CommandSink
{
    public class PinvokeCommandSink : ICommandSink
    {
        private IntPtr currentProcess;
        private readonly nint baseOffset;

        public PinvokeCommandSink(IntPtr process, nint baseOffset)
        {
            //this.currentProcess = OpenProcess(Access.Default, false, processId);
            this.currentProcess = process;
            this.baseOffset = baseOffset;
        }

        public nint GetBaseOffset() => this.baseOffset;

        public nint GetAbsoluteAddress(nint offset) => this.baseOffset + offset;

        public unsafe void Read(nint address, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(this.baseOffset + address, b, data.Length);
            }
        }

        public unsafe void Read(Ptr<nint> pointerToAddress, Span<byte> data)
        {
            byte* ptrVal = stackalloc byte[8];

            Read(this.baseOffset + pointerToAddress, ptrVal, 8);

            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(*(nint*)ptrVal, b, data.Length);
            }
        }

        public unsafe void ReadAt(nint address, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(address, b, data.Length);
            }
        }

        public unsafe void Write(nint relativeAddress, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Write(this.baseOffset + relativeAddress, b, data.Length);
            }
        }

        public unsafe void WriteAt(nint absoluteAddress, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Write(absoluteAddress, b, data.Length);
            }
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

        private unsafe void Read(nint address, byte* destination, int length)
        {
            if (!Win32.ReadProcessMemory(this.currentProcess, address, destination, length, out var read))
            {
                throw new Exception($"Couldn't read data at {address.ToString("x")}", new Win32Exception());
            }

            if (read != length)
            {
                throw new Exception("Couldn't read all requested data", new Win32Exception());
            }
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

        private unsafe void Write(nint address, byte* source, int length)
        {
            Logger.LogTrace($"Writing {length} bytes to {this.currentProcess} at {address:x}");

            if (!Win32.WriteProcessMemory(this.currentProcess, address, source, length, out var written))
            {
                throw new Exception($"Couldn't write data to {address.ToString("x")}", new Win32Exception());
            }

            if (written != length)
            {
                throw new Exception("Couldn't write all requested data", new Win32Exception());
            }
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

        public Task PollMemory(nint relativeAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            var data = new byte[byteCount];
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.Read(relativeAddress, data);
                    callback(data);
                    await Task.Delay((int)intervalMs);
                }
            });
        }

        public Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            var data = new byte[byteCount];
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.ReadAt(absoluteAddress, data);
                    callback(data);
                    await Task.Delay((int)intervalMs);
                }
            });
        }

        public Task PollMemory<T>(nint relativeAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.Read<T>(relativeAddress, out var item);
                    callback(item);
                    await Task.Delay((int)intervalMs);
                }
            });
        }

        public Task PollMemoryAt<T>(nint absoluteAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged
        {
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.ReadAt<T>(absoluteAddress, out var item);
                    callback(item);
                    await Task.Delay((int)intervalMs);
                }
            });
        }
    }
}
