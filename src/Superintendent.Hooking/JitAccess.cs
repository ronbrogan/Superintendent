using Superintendent.Core.Native;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Xsl;

namespace Superintendent.Hooking
{
    public unsafe class JitAccess
    {
        public static Guid jitVersion = new Guid("5ed35c58-857b-48dd-a818-7c0136dc9f73");

        private const int compileMethodIndex = 0;
        private const int versionMethodIndex = 2;
        private const int getMethodDefFromMethodIndex = 113;
        private static delegate* unmanaged[Stdcall, SuppressGCTransition]<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int> compileMethod;
        private static delegate* unmanaged[Stdcall, SuppressGCTransition]<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int> compileMethodWrapper;
        //private static CompileMethod compileMethod;

        //[ModuleInitializer]
        public static void Setup()
        {
            if (compileMethod == null)
                throw new InvalidOperationException("JitAccess was unable to initialize, there may be a version mismatch between the library and the current runtime");
        }

        static JitAccess()
        {
            var jitDll = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName == "clrjit.dll");

            if(jitDll == null || !File.Exists(jitDll.FileName))
            {
                throw new InvalidOperationException("No jit module found");
            }

            var getJit = (delegate* unmanaged<nint*>)NativeLibrary.GetExport(jitDll.BaseAddress, "getJit");

            var jitPtr = getJit();

            var corJitCompiler = *jitPtr;

            var ptrToCompileMethod = (nint*)(corJitCompiler + (compileMethodIndex * sizeof(nint)));
            JitAccess.compileMethod = (delegate* unmanaged[Stdcall, SuppressGCTransition]<void*, void*, void*, uint, byte**, int*, int>)*ptrToCompileMethod;
            var getVersionIdenitfier = (delegate* unmanaged<nint, Guid*, void>)*(nint*)(corJitCompiler + (versionMethodIndex * sizeof(nint)));

            CreateImplDelegate();
            //JitAccess.compileMethod((void*)0, (void*)0, (void*)0, 0, (byte**)0, (int*)0);

            Guid foundVersion;
            getVersionIdenitfier(corJitCompiler, & foundVersion);

            if(foundVersion != jitVersion)
            {
                throw new NotSupportedException("Invalid jit version");
            }

            MethodBodies.TryAdd(0, (1, 0));

            Win32.VirtualProtect((IntPtr)ptrToCompileMethod, sizeof(nint), MemoryProtection.ReadWrite, out var orig);

            var wrapperAddress = (nint)(delegate* unmanaged<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int>)&CompileMethodWrapper;

            EnsureWrapperJitted(wrapperAddress);

            
            
            


            *ptrToCompileMethod = wrapperAddress;

            Win32.VirtualProtect((IntPtr)ptrToCompileMethod, sizeof(nint), orig, out _);
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CompileMethod(void* @this, void* comp, CORINFO_METHOD_INFO* info, uint flags, byte** entry, int* size);

        public static ConcurrentDictionary<nint, (nint, int)> MethodBodies = new();

        [ThreadStatic]
        private static RefInt CompileEntry;

        [UnmanagedCallersOnly]
        public static int CompileMethodWrapper(void* t, void* a, CORINFO_METHOD_INFO* b, uint c, byte** d, int* e)
        {
            CompileEntry ??= new RefInt();
            CompileEntry.Value++;

            try
            {
                if ((nint)t == 0)
                    return 0;

                var result = compileMethodWrapper(t, a, b, c, d, e);

                if (CompileEntry.Value == 1)
                {
                    var getMethodDefFromMethodPtr = (*(nint*)a) + (sizeof(nint) * getMethodDefFromMethodIndex);
                    var getMethodDefFromMethod = (delegate* unmanaged<nint, int>)getMethodDefFromMethodPtr;
                    var mdtoken = getMethodDefFromMethod(b->method);

                    // store
                    MethodBodies.TryAdd((nint)a, ((nint)(*d), *e));
                }

                return result;
            }
            finally
            {
                CompileEntry.Value--;
            }
        }

        private static void EnsureWrapperJitted(nint address)
        {
            var ptr = (delegate* unmanaged<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int>)&CompileMethodWrapper;

            ptr((void*)0, (void*)0, (CORINFO_METHOD_INFO*)0, 0, (byte**)0, (int*)0);

            //return;

            Span<byte> callPointer = new byte[] {
                // mov rax, 0000000000000000h
                0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // jmp rax
                0xFF, 0xE0
            };

            BitConverter.GetBytes(address).CopyTo(callPointer.Slice(2, sizeof(nint)));

            var loc = Win32.VirtualAlloc(IntPtr.Zero, callPointer.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

            try
            {
                callPointer.CopyTo(new Span<byte>((void*)loc, callPointer.Length));

                var delg = Marshal.GetDelegateForFunctionPointer<CompileMethod>(loc);

                // making this call simulates a call to method as if it were from a native library
                // causing the jit stub to fully jit the method
                delg((void*)0, (void*)0, (CORINFO_METHOD_INFO*)0, 0, (byte**)0, (int*)0);
            }
            finally
            {
                Win32.VirtualFree(loc, callPointer.Length, AllocationType.Release);
            }
        }

        private static void CreateImplDelegate()
        {
            var ptr = (nint)(delegate* unmanaged<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int>)&CompileMethodWrapper;

            Span<byte> callPointer = new byte[] {
                // mov rax, 0000000000000000h
                0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                // jmp rax
                0xFF, 0xE0
            };

            BitConverter.GetBytes(ptr).CopyTo(callPointer.Slice(2, sizeof(nint)));

            var loc = Win32.VirtualAlloc(IntPtr.Zero, callPointer.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

            callPointer.CopyTo(new Span<byte>((void*)loc, callPointer.Length));

            var delg = (delegate* unmanaged[Stdcall, SuppressGCTransition]<void*, void*, CORINFO_METHOD_INFO*, uint, byte**, int*, int>)loc;

            // making this call simulates a call to method as if it were from a native library
            // causing the jit stub to fully jit the method
            delg((void*)0, (void*)0, (CORINFO_METHOD_INFO*)0, 0, (byte**)0, (int*)0);

            var alloc = new Span<byte>((void*)loc, callPointer.Length);
            BitConverter.GetBytes((nint)compileMethod).CopyTo(alloc.Slice(2, sizeof(nint)));

            JitAccess.compileMethodWrapper = delg;
        }

        private void UpdateImplDelegate(nint alloc)
        {
        
        }

        private static void CallWrapper()
        {

        }

        public static nint Get()
        {
            return 0;
        }

        public unsafe struct CORINFO_METHOD_INFO
        {
            public nint method;
            public nint module;
            public byte* ILCode;
            public uint ILCodeSize;
            public uint maxStack;
            public uint EHcount;
            public CorInfoOptions options;
            public CorInfoRegionKind regionKind;
            //public CORINFO_SIG_INFO args;
            //public CORINFO_SIG_INFO locals;
        }

        public enum CorInfoOptions
        {
            CORINFO_OPT_INIT_LOCALS = 0x00000010, // zero initialize all variables

            CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020, // is this shared generic code that access the generic context from the this pointer?  If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
            CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
            CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080, // is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)?  If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as CORINFO_CALLCONV_PARAMTYPE
            CORINFO_GENERICS_CTXT_MASK = (CORINFO_GENERICS_CTXT_FROM_THIS |
                                                       CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                                       CORINFO_GENERICS_CTXT_FROM_METHODTABLE),
            CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100, // Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
        }

        public enum CorInfoRegionKind
        {
            CORINFO_REGION_NONE,
            CORINFO_REGION_HOT,
            CORINFO_REGION_COLD,
            CORINFO_REGION_JIT,
        }

        public class RefInt
        {
            public int Value;
        }
    }
}