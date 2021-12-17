using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Superintendent.Core.Native
{
    public unsafe class Win32
    {
        public const int PROCESS_QUERY_INFORMATION = 0x400;
        public const int PROCESS_VM_READ = 0x10;

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
        public static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass, ref PROCESS_BASIC_INFORMATION ProcessInformation, int ProcessInformationLength, IntPtr ReturnLength);

        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationThread(IntPtr threadHandle, ThreadInformationClass ThreadInformationClass, ref THREAD_BASIC_INFORMATION ProcessInformation, int ThreadInformationLength, out IntPtr ReturnLength);

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
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType);

        /// <summary>
        /// Inject the module into the process. 
        /// This will open and close a handle to the process for use during injection
        /// </summary>
        public static bool InjectModule(int pid, string pathToModule)
        {
            var injectPermissions = AccessPermissions.CreateThread | AccessPermissions.QueryInformation | AccessPermissions.VmOperation | AccessPermissions.VmWrite | AccessPermissions.VmRead;

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
                Console.WriteLine("Waiting for injection thread to exit");
            }

            CloseHandle(threadHandle);
            VirtualFreeEx(processHandle, nameLoc, 0, AllocationType.Decommit);

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
    public struct PROCESS_BASIC_INFORMATION
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2_0;
        public IntPtr Reserved2_1;
        public IntPtr UniqueProcessId;
        public IntPtr Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct THREAD_BASIC_INFORMATION
    {
        public IntPtr ExitStatus;
        public IntPtr TebBaseAddress;
        public IntPtr ClientId;
        public IntPtr AffinityMask;
        public IntPtr Priority;
        public IntPtr BasePriority;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING
    {
        public short Length;
        public short MaximumLength;
        public IntPtr Buffer;
    }

    // for 32-bit process in a 64-bit OS only
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_BASIC_INFORMATION_WOW64
    {
        public long Reserved1;
        public long PebBaseAddress;
        public long Reserved2_0;
        public long Reserved2_1;
        public long UniqueProcessId;
        public long Reserved3;
    }

    // for 32-bit process in a 64-bit OS only
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING_WOW64
    {
        public short Length;
        public short MaximumLength;
        public long Buffer;
    }

    
}
