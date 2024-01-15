using System.Text;

namespace Superintendent.Core
{
    public interface IAllocator
    {
        nint Allocate(int bytes);
        void Free(nint address, int bytes);

        nint AllocateString(string value, Encoding? encoding = null);

        nint AllocateStrings(string[] values, Encoding? encoding = null);
        nint AllocateArray<T>(T[] values, Encoding? encoding = null) where T : unmanaged;
    }
}
