using System;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.CommandSink
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
        nint GetAbsoluteAddress(nint offset);
        void Write(nint relativeAddress, Span<byte> data);
        void WriteAt(nint absoluteAddress, Span<byte> data);
        void Write<T>(nint relativeAddress, T data) where T : unmanaged;
        void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged;

        void Read(nint address, Span<byte> data);
        void Read(Ptr<nint> ptrToaddress, Span<byte> data);
        void ReadAt(nint address, Span<byte> data);
        void Read<T>(nint relativeAddress, out T data) where T : unmanaged;
        void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged;

        Task PollMemory(nint relativeAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default);
        Task PollMemoryAt(nint absoluteAddress, uint intervalMs, uint byteCount, ReadOnlySpanAction<byte> callback, CancellationToken token = default);
        Task PollMemory<T>(nint relativeAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged;
        Task PollMemoryAt<T>(nint absoluteAddress, uint intervalMs, Action<T> callback, CancellationToken token = default) where T : unmanaged;

        nint GetBaseOffset();

        (bool, T) CallFunctionAt<T>(nint functionPointer, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged;
        (bool, T) CallFunction<T>(nint functionPointerOffset, nint? arg1 = null, nint? arg2 = null, nint? arg3 = null, nint? arg4 = null, nint? arg5 = null, nint? arg6 = null, nint? arg7 = null, nint? arg8 = null, nint? arg9 = null, nint? arg10 = null, nint? arg11 = null, nint? arg12 = null) where T : unmanaged;

        void SetTlsValue(int index, nint value);
        void SetThreadLocalPointer(nint value);
        nint GetThreadLocalPointer();
    }
}
