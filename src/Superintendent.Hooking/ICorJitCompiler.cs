using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Superintendent.Hooking
{
    public interface ICorJitCompilerInfo<T>
    {
        string FileVersion { get; }
        Guid Version { get; }


    }

    public unsafe struct ICorJitCompiler_6_0_8
    {
        //[FieldOffset(4 * IntPtr.Size)]
        //public nint GetVersion(out Guid v);
    }
}
