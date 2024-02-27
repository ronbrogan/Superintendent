using System;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.CommandSink
{
    /// <summary>
    /// A way to interface with a process, scoped to a particular module (dll).
    /// Any realtive commands will add the BaseOffset of the module automatically.
    /// Absolute commands, with names like (Read|Write)At are provided for convenience and
    /// do not add the base offset of the module.
    /// </summary>
    public interface ICommandSink
    {
        nint GetAbsoluteAddress(nint offset);
        void WriteSpan<T>(nint relativeAddress, ReadOnlySpan<T> data) where T : unmanaged;
        void WriteSpanAt<T>(nint absoluteAddress, ReadOnlySpan<T> data) where T : unmanaged;
        void Write<T>(nint relativeAddress, T data) where T : unmanaged;
        void WriteAt<T>(nint absoluteAddress, T data) where T : unmanaged;

        void ReadSpan<T>(nint address, Span<T> data) where T : unmanaged;
        void ReadSpanAt<T>(nint address, Span<T> data) where T : unmanaged;
        void Read<T>(nint relativeAddress, out T data) where T : unmanaged;
        void ReadAt<T>(nint absoluteAddress, out T data) where T : unmanaged;

        void ReadPointer<T>(Ptr<T> relativePointer, out T data) where T : unmanaged;
        void ReadPointerSpan<T>(Ptr<T> relativePointer, Span<T> data) where T : unmanaged;

        void ReadPointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged;
        void ReadPointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged;
        void ReadAbsolutePointer<T>(Ptr<T> absolutePointer, out T data) where T : unmanaged; 
        void ReadAbsolutePointerSpan<T>(Ptr<T> absolutePointer, Span<T> data) where T : unmanaged;
        void ReadAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, out T data) where T : unmanaged;
        void ReadAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, Span<T> data) where T : unmanaged;

        void WritePointer<T>(Ptr<T> relativePointer, T data) where T : unmanaged;
        void WritePointerSpan<T>(Ptr<T> relativePointer, ReadOnlySpan<T> data) where T : unmanaged;

        void WritePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged;
        void WritePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged;
        void WriteAbsolutePointer<T>(Ptr<T> absolutePointer, T data) where T : unmanaged;
        void WriteAbsolutePointerSpan<T>(Ptr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged;
        void WriteAbsolutePointer<T>(AbsolutePtr<T> absolutePointer, T data) where T : unmanaged;
        void WriteAbsolutePointerSpan<T>(AbsolutePtr<T> absolutePointer, ReadOnlySpan<T> data) where T : unmanaged;


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
