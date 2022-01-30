using Superintendent.Core;
using System;

namespace Superintendent.CommandSink
{
    [Flags]
    public enum RegionProtection
    {
        Read = 1,
        Write = 2,
        Execute = 4
    }

    public interface ICommandSink
    {
        public void Write(nint relativeAddress, Span<byte> data);

        public void WriteAt(nint absoluteAddress, Span<byte> data);

        public void Write<T>(nint relativeAddress, T data) where T : unmanaged;

        public void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged;

        public void Read(nint address, Span<byte> data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data);

        void ReadAt(nint address, Span<byte> data);

        public void Read<T>(nint relativeAddress, out T data) where T : unmanaged;

        public void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged;

        nint GetBaseOffset();

        (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged;
        (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged;

        public void SetTlsValue(int index, nint value);
    }
}
