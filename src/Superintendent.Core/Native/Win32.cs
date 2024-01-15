using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Superintendent.Core.Native
{
    public unsafe class Win32
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(AccessPermissions desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, ref ProcessBasicInformation ProcessInformation, int ProcessInformationLength, IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(IntPtr threadHandle, ThreadInformationClass ThreadInformationClass, ref ThreadBasicInformation ProcessInformation, int ThreadInformationLength, out IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(IntPtr threadHandle, ThreadInformationClass ThreadInformationClass, ref IntPtr ProcessInformation, int ThreadInformationLength, out IntPtr ReturnLength);


        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr process, nint address, [Out] byte* data, int lengthToRead, out nint bytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr process, nint address, [In] byte* data, int lengthToWrite, out nint bytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFree(IntPtr lpAddress, int dwSize, AllocationType flAllocationType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, MemoryProtection flProtect, out MemoryProtection oldflProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, MemoryProtection flProtect, out MemoryProtection oldflProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetThreadContext(IntPtr hThread, ref CONTEXT64 lpContext);

        /// <summary>
        /// Inject the module into the process. 
        /// This will open and close a handle to the process for use during injection
        /// </summary>
        public static bool InjectModule(int pid, string pathToModule)
        {
            var injectPermissions = AccessPermissions.ProcessCreateThread | AccessPermissions.ProcessQueryInformation | AccessPermissions.ProcessVmOperation | AccessPermissions.ProcessVmWrite | AccessPermissions.ProcessVmRead;

            var handle = OpenProcess(injectPermissions, false, pid);

            var injected = InjectModule(pid, handle, pathToModule);

            CloseHandle(handle);

            return injected;
        }

        public static bool InjectModule(int pid, IntPtr processHandle, string pathToModule)
        {
            foreach (ProcessModule mod in Process.GetProcessById(pid).Modules)
            {
                if (mod.FileName == pathToModule)
                    return false;
            }

            var loadLibrary = GetProcAddress(GetModuleHandle("Kernel32"), "LoadLibraryA");

            var bytes = Encoding.ASCII.GetBytes(pathToModule);
            var nameLoc = VirtualAllocEx(processHandle, IntPtr.Zero, (uint)bytes.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

            fixed (byte* b = bytes)
            {
                WriteProcessMemory(processHandle, nameLoc, b, bytes.Length, out var written);
            }

            var threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0, loadLibrary, nameLoc, 0, out var id);

            var threadExit = (uint)ExitCode.StillActive;

            while (threadExit == (uint)ExitCode.StillActive)
            {
                Thread.Sleep(5);
                GetExitCodeThread(threadHandle, out threadExit);
                Logger.LogInformation("Waiting for injection thread to exit");
            }

            CloseHandle(threadHandle);
            VirtualFreeEx(processHandle, nameLoc, 0, AllocationType.Decommit);

            return true;
        }

        /// <summary>
        /// Eject the module from the process. 
        /// This will open and close a handle to the process for use during ejection.
        /// This should only be used on modules that were injected and gracefully handle removal
        /// </summary>
        public static void EjectModule(int pid, string pathToModule)
        {
            var injectPermissions = AccessPermissions.ProcessCreateThread | AccessPermissions.ProcessQueryInformation | AccessPermissions.ProcessVmOperation | AccessPermissions.ProcessVmWrite | AccessPermissions.ProcessVmRead;

            var handle = OpenProcess(injectPermissions, false, pid);

            EjectModule(pid, handle, pathToModule);

            CloseHandle(handle);
        }

        public static bool EjectModule(int pid, IntPtr processHandle, string pathToModule)
        {
            IntPtr module = IntPtr.Zero;

            foreach (ProcessModule mod in Process.GetProcessById(pid).Modules)
            {
                if (mod.FileName == pathToModule)
                    return false;

                module = mod.BaseAddress;
            }

            var freeLibrary = GetProcAddress(GetModuleHandle("Kernel32"), "FreeLibrary");

            var threadHandle = CreateRemoteThread(processHandle, IntPtr.Zero, 0, freeLibrary, module, 0, out var id);

            var threadExit = (uint)ExitCode.StillActive;

            while (threadExit == (uint)ExitCode.StillActive)
            {
                Thread.Sleep(5);
                GetExitCodeThread(threadHandle, out threadExit);
                Logger.LogInformation("Waiting for ejection thread to exit");
            }

            CloseHandle(threadHandle);

            return true;
        }
    }

    public enum ThreadInformationClass
    {
        ThreadBasicInformation,
        ThreadTimes,
        ThreadPriority,
        ThreadBasePriority,
        ThreadAffinityMask,
        ThreadImpersonationToken,
        ThreadDescriptorTableEntry,
        ThreadEnableAlignmentFaultFixup,
        ThreadEventPair,
        ThreadQuerySetWin32StartAddress,
        ThreadZeroTlsCell,
        ThreadPerformanceCount,
        ThreadAmILastThread,
        ThreadIdealProcessor,
        ThreadPriorityBoost,
        ThreadSetTlsArrayAddress,
        ThreadIsIoPending,
        ThreadHideFromDebugger
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessBasicInformation
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadBasicInformation
    {
        public IntPtr ExitStatus;
        public IntPtr TebBaseAddress;
        public IntPtr ClientId;
        public IntPtr AffinityMask;
        public IntPtr Priority;
        public IntPtr BasePriority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnicodeString
    {
        public short Length;
        public short MaximumLength;
        public IntPtr Buffer;
    }

    public enum CONTEXT_FLAGS : uint
    {
        CONTEXT_i386 = 0x10000,
        CONTEXT_i486 = 0x10000,   //  same as i386
        CONTEXT_CONTROL = CONTEXT_i386 | 0x01, // SS:SP, CS:IP, FLAGS, BP
        CONTEXT_INTEGER = CONTEXT_i386 | 0x02, // AX, BX, CX, DX, SI, DI
        CONTEXT_SEGMENTS = CONTEXT_i386 | 0x04, // DS, ES, FS, GS
        CONTEXT_FLOATING_POINT = CONTEXT_i386 | 0x08, // 387 state
        CONTEXT_DEBUG_REGISTERS = CONTEXT_i386 | 0x10, // DB 0-3,6,7
        CONTEXT_EXTENDED_REGISTERS = CONTEXT_i386 | 0x20, // cpu specific extensions
        CONTEXT_FULL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS,
        CONTEXT_ALL = CONTEXT_CONTROL | CONTEXT_INTEGER | CONTEXT_SEGMENTS | CONTEXT_FLOATING_POINT | CONTEXT_DEBUG_REGISTERS | CONTEXT_EXTENDED_REGISTERS
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FLOATING_SAVE_AREA
    {
        public uint ControlWord;
        public uint StatusWord;
        public uint TagWord;
        public uint ErrorOffset;
        public uint ErrorSelector;
        public uint DataOffset;
        public uint DataSelector;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] RegisterArea;
        public uint Cr0NpxState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONTEXT
    {
        public uint ContextFlags; //set this to an appropriate value
                                  // Retrieved by CONTEXT_DEBUG_REGISTERS
        public uint Dr0;
        public uint Dr1;
        public uint Dr2;
        public uint Dr3;
        public uint Dr6;
        public uint Dr7;
        // Retrieved by CONTEXT_FLOATING_POINT
        public FLOATING_SAVE_AREA FloatSave;
        // Retrieved by CONTEXT_SEGMENTS
        public uint SegGs;
        public uint SegFs;
        public uint SegEs;
        public uint SegDs;
        // Retrieved by CONTEXT_INTEGER
        public uint Edi;
        public uint Esi;
        public uint Ebx;
        public uint Edx;
        public uint Ecx;
        public uint Eax;
        // Retrieved by CONTEXT_CONTROL
        public uint Ebp;
        public uint Eip;
        public uint SegCs;
        public uint EFlags;
        public uint Esp;
        public uint SegSs;
        // Retrieved by CONTEXT_EXTENDED_REGISTERS
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] ExtendedRegisters;
    }

    // Next x64

    [StructLayout(LayoutKind.Sequential)]
    public struct M128A
    {
        public ulong High;
        public long Low;

        public override string ToString()
        {
            return string.Format("High:{0}, Low:{1}", this.High, this.Low);
        }
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct XSAVE_FORMAT64
    {
        public ushort ControlWord;
        public ushort StatusWord;
        public byte TagWord;
        public byte Reserved1;
        public ushort ErrorOpcode;
        public uint ErrorOffset;
        public ushort ErrorSelector;
        public ushort Reserved2;
        public uint DataOffset;
        public ushort DataSelector;
        public ushort Reserved3;
        public uint MxCsr;
        public uint MxCsr_Mask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public M128A[] FloatRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public M128A[] XmmRegisters;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 96)]
        public byte[] Reserved4;
    }

    /// <summary>
    /// x64
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct CONTEXT64
    {
        public ulong P1Home;
        public ulong P2Home;
        public ulong P3Home;
        public ulong P4Home;
        public ulong P5Home;
        public ulong P6Home;

        public CONTEXT_FLAGS ContextFlags;
        public uint MxCsr;

        public ushort SegCs;
        public ushort SegDs;
        public ushort SegEs;
        public ushort SegFs;
        public ushort SegGs;
        public ushort SegSs;
        public uint EFlags;

        public ulong Dr0;
        public ulong Dr1;
        public ulong Dr2;
        public ulong Dr3;
        public ulong Dr6;
        public ulong Dr7;

        public ulong Rax;
        public ulong Rcx;
        public ulong Rdx;
        public ulong Rbx;
        public ulong Rsp;
        public ulong Rbp;
        public ulong Rsi;
        public ulong Rdi;
        public ulong R8;
        public ulong R9;
        public ulong R10;
        public ulong R11;
        public ulong R12;
        public ulong R13;
        public ulong R14;
        public ulong R15;
        public ulong Rip;

        public XSAVE_FORMAT64 DUMMYUNIONNAME;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 26)]
        public M128A[] VectorRegister;
        public ulong VectorControl;

        public ulong DebugControl;
        public ulong LastBranchToRip;
        public ulong LastBranchFromRip;
        public ulong LastExceptionToRip;
        public ulong LastExceptionFromRip;
    }
}
