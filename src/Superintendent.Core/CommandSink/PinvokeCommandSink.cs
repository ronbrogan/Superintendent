using Superintendent.Core.Native;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.CommandSink
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

        public unsafe void ReadSpan<T>(nint relativeAddress, Span<T> data) where T : unmanaged
        {
            fixed (T* b = &MemoryMarshal.GetReference(data))
            {
                Read(this.baseOffset + relativeAddress, (byte*)b, data.Length * sizeof(T));
            }
        }

        public unsafe void ReadSpanAt<T>(nint absoluteAddress, Span<T> data) where T : unmanaged
        {
            fixed (T* b = &MemoryMarshal.GetReference(data))
            {
                Read(absoluteAddress, (byte*)b, data.Length * sizeof(T));
            }
        }

        public unsafe void WriteSpan<T>(nint relativeAddress, ReadOnlySpan<T> data) where T : unmanaged
        {
            fixed (T* b = &MemoryMarshal.GetReference(data))
            {
                Write(this.baseOffset + relativeAddress, (byte*)b, data.Length * sizeof(T));
            }
        }

        public unsafe void WriteSpanAt<T>(nint absoluteAddress, ReadOnlySpan<T> data) where T : unmanaged
        {
            fixed (T* b = &MemoryMarshal.GetReference(data))
            {
                Write(absoluteAddress, (byte*)b, data.Length * sizeof(T));
            }
        }

        public unsafe void Write<T>(nint relativeAddress, T data) where T : unmanaged
        {
            this.Write(this.baseOffset + relativeAddress, (byte*)&data, sizeof(T));
        }

        public unsafe void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged
        {
            this.Write(absoluteAddress, (byte*)&data, sizeof(T));
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
            this.Read(this.baseOffset + relativeAddress, (byte*)Unsafe.AsPointer(ref data), sizeof(T));
        }

        public unsafe void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged
        {
            Unsafe.SkipInit(out data);
            this.Read(absoluteAddress, (byte*)Unsafe.AsPointer(ref data), sizeof(T));
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
            var data = GC.AllocateArray<byte>((int)byteCount, pinned: true);
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.ReadSpan<byte>(this.baseOffset + relativeAddress, data);
                    callback(data);
                    await Task.Delay((int)intervalMs);
                }
            });
        }

        public Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default)
        {
            var data = GC.AllocateArray<byte>((int)byteCount, pinned: true);
            return Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    this.ReadSpanAt<byte>(absoluteAddress, data);
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

        public void SetThreadLocalPointer(nint value)
        {
            throw new NotSupportedException();
        }

        public nint GetThreadLocalPointer()
        {
            throw new NotSupportedException();
        }

        public void ReadPointer<T>(Ptr<T> relativePointer, out T data) where T : unmanaged
        {
            var address = ResolvePointer(relativePointer, this.baseOffset);
            this.ReadAt(address, out data);
        }

        public void ReadPointerSpan<T>(Ptr<T> relativePointer, Span<T> data) where T : unmanaged
        {
            var address = ResolvePointer(relativePointer, this.baseOffset);
            this.ReadSpanAt(address, data);
        }

        public void ReadAbsolutePointer<T>(Ptr<T> absolutePointer, out T data) where T : unmanaged
        {
            var address = ResolvePointer(absolutePointer);
            this.ReadAt(address, out data);
        }

        public void ReadAbsolutePointerSpan<T>(Ptr<T> absolutePointer, Span<T> data) where T : unmanaged
        {
            var address = ResolvePointer(absolutePointer);
            this.ReadSpanAt(address, data);
        }

        public void WritePointer<T>(Ptr<T> relativePointer, T data) where T : unmanaged
        {
            var address = ResolvePointer(relativePointer, this.baseOffset);
            this.WriteAt(address, data);
        }

        public void WritePointerSpan<T>(Ptr<T> relativePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            var address = ResolvePointer(relativePointer, this.baseOffset);
            this.WriteSpanAt(address, data);
        }

        public void WriteAbsolutePointer<T>(Ptr<T> absolutePointer, T data) where T : unmanaged
        {
            var address = ResolvePointer(absolutePointer);
            this.WriteAt(address, data);
        }

        public void WriteAbsolutePointerSpan<T>(Ptr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged
        {
            var address = ResolvePointer(absolutePointer);
            this.WriteSpanAt(address, data);
        }

        private nint ResolvePointer<T>(Ptr<T> pointer, nint baseOffset = 0) where T: unmanaged
        {
            nint address = pointer.Base + this.baseOffset;

            foreach(var next in pointer.Chain)
            {
                this.ReadAt<nint>(address + next, out address);
            }

            return address;
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
    }
}
