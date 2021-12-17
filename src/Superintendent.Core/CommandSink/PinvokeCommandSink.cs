using Superintendent.Core;
using Superintendent.Core.Native;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Superintendent.CommandSink
{
    public unsafe class PinvokeCommandSink : ICommandSink
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

        public void Read(nint address, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(this.baseOffset + address, b, data.Length);
            }
        }

        public void Read(Ptr<nint> pointerToAddress, Span<byte> data)
        {
            byte* ptrVal = stackalloc byte[8];

            Read(this.baseOffset + pointerToAddress, ptrVal, 8);

            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(*(nint*)ptrVal, b, data.Length);
            }
        }

        public void ReadAt(nint address, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Read(address, b, data.Length);
            }
        }

        public void Write(nint relativeAddress, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Write(this.baseOffset + relativeAddress, b, data.Length);
            }
        }

        public void WriteAt(nint absoluteAddress, Span<byte> data)
        {
            fixed (byte* b = &MemoryMarshal.GetReference(data))
            {
                Write(absoluteAddress, b, data.Length);
            }
        }

        private void Read(nint address, byte* destination, int length)
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

        private void Write(nint address, byte* source, int length)
        {
            Console.WriteLine($"Writing {length} bytes to {this.currentProcess} at {address.ToString("x")}");

            if (!Win32.WriteProcessMemory(this.currentProcess, address, source, length, out var written))
            {
                throw new Exception($"Couldn't write data to {address.ToString("x")}", new Win32Exception());
            }

            if (written != length)
            {
                throw new Exception("Couldn't write all requested data", new Win32Exception());
            }
        }

        public T CallFunctionAt<T>(nint functionPointer, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
        {
            throw new NotImplementedException();
        }

        public T CallFunction<T>(nint functionPointerOffset, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null)
        {
            throw new NotImplementedException();
        }
    }
}
