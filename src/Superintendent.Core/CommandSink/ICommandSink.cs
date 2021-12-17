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

        public void Read(nint address, Span<byte> data);

        public void Read(Ptr<nint> ptrToaddress, Span<byte> data);

        void ReadAt(nint address, Span<byte> data);

        nint GetBaseOffset();

        T CallFunctionAt<T>(nint functionPointer, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null);
        T CallFunction<T>(nint functionPointerOffset, ulong? arg1 = null, ulong? arg2 = null, ulong? arg3 = null, ulong? arg4 = null);
    }
}
