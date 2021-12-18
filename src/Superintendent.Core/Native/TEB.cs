using System;
using System.Runtime.InteropServices;

namespace Superintendent.Core.Native
{
    // Ported from wine codebase

    [StructLayout(LayoutKind.Sequential)]
    public struct ClientID
    {
        IntPtr A;
        IntPtr B;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ActivationContextStack
    {
        IntPtr A;
        IntPtr B;
        IntPtr C;
        IntPtr D;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NT_TIB
    {
        /* 0x0000 */
        IntPtr ExceptionList;
        /* 0x0008 */
        IntPtr StackBase;
        /* 0x0010 */
        IntPtr StackLimit;
        /* 0x0018 */
        IntPtr SubSystemTib;
        /* 0x0020 */
        IntPtr FiberDataOrVersion;
        /* 0x0028 */
        IntPtr ArbitraryUserPointer;
        /* 0x0030 */
        IntPtr Self;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GDI_TEB_BATCH
    {
        uint Offset;
        IntPtr HDC;
        fixed uint Buffer[0x136];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TEB
    {                                                           /* win32/win64 */
        public NT_TIB Tib;                                      /* 000/0000 */
        public IntPtr EnvironmentPointer;                       /* 01c/0038 */
        public ClientID ClientId;                               /* 020/0040 */
        public IntPtr ActiveRpcHandle;                          /* 028/0050 */
        public IntPtr ThreadLocalStoragePointer;                /* 02c/0058 */
        public IntPtr Peb;                                      /* 030/0060 */
        public uint LastErrorValue;                             /* 034/0068 */
        public uint CountOfOwnedCriticalSections;               /* 038/006c */
        public IntPtr CsrClientThread;                          /* 03c/0070 */
        public IntPtr Win32ThreadInfo;                          /* 040/0078 */
        public fixed uint Win32ClientInfo[31];                  /* 044/0080 used for user32 private data in Wine */
        public IntPtr WOW32Reserved;                            /* 0c0/0100 */
        public uint CurrentLocale;                              /* 0c4/0108 */
        public uint FpSoftwareStatusRegister;                   /* 0c8/010c */
        public fixed ulong SystemReserved1[54];                 /* 0cc/0110 used for kernel32 private data in Wine */
        public long ExceptionCode;                              /* 1a4/02c0 */
        public ActivationContextStack ActivationContextStack;   /* 1a8/02c8 */
        public fixed byte SpareBytes1[24];                      /* 1bc/02e8 */
        public fixed ulong SystemReserved2[10];                 /* 1d4/0300 used for ntdll platform-specific private data in Wine */
        public GDI_TEB_BATCH GdiTebBatch;                       /* 1fc/0350 used for ntdll private data in Wine */
        public IntPtr gdiRgn;                                   /* 6dc/0838 */
        public IntPtr gdiPen;                                   /* 6e0/0840 */
        public IntPtr gdiBrush;                                 /* 6e4/0848 */
        public ClientID RealClientId;                           /* 6e8/0850 */
        public IntPtr GdiCachedProcessHandle;                   /* 6f0/0860 */
        public uint GdiClientPID;                               /* 6f4/0868 */
        public uint GdiClientTID;                               /* 6f8/086c */
        public IntPtr GdiThreadLocaleInfo;                      /* 6fc/0870 */
        public fixed uint UserReserved[5];                      /* 700/0878 */
        public fixed ulong glDispatchTable[280];                /* 714/0890 */
        public fixed ulong glReserved1[26];                     /* b74/1150 */
        public IntPtr glReserved2;                              /* bdc/1220 */
        public IntPtr glSectionInfo;                            /* be0/1228 */
        public IntPtr glSection;                                /* be4/1230 */
        public IntPtr glTable;                                  /* be8/1238 */
        public IntPtr glCurrentRC;                              /* bec/1240 */
        public IntPtr glContext;                                /* bf0/1248 */
        public uint LastStatusValue;                            /* bf4/1250 */
        public UnicodeString StaticUnicodeString;              /* bf8/1258 used by advapi32 */
        public fixed char StaticUnicodeBuffer[261];             /* c00/1268 used by advapi32 */
        public IntPtr DeallocationStack;                        /* e0c/1478 */
        public fixed ulong TlsSlots[64];                        /* e10/1480 */
        public ClientID TlsLinks;                               /* f10/1680 */
        public IntPtr Vdm;                                      /* f18/1690 */
        public IntPtr ReservedForNtRpc;                         /* f1c/1698 */
        public fixed ulong DbgSsReserved[2];                    /* f20/16a0 */
        public uint HardErrorDisabled;                          /* f28/16b0 */
        public fixed ulong Instrumentation[16];                 /* f2c/16b8 */
        public IntPtr WinSockData;                              /* f6c/1738 */
        public uint GdiBatchCount;                              /* f70/1740 */
        public uint Spare2;                                     /* f74/1744 */
        public uint GuaranteedStackBytes;                       /* f78/1748 */
        public IntPtr ReservedForPerf;                          /* f7c/1750 */
        public IntPtr ReservedForOle;                           /* f80/1758 */
        public uint WaitingOnLoaderLock;                        /* f84/1760 */
        public fixed ulong Reserved5[3];                        /* f88/1768 */
        public IntPtr TlsExpansionSlots;                        /* f94/1780 */
        public IntPtr DeallocationBStore;                       /*    /1788 */
        public IntPtr BStoreLimit;                              /*    /1790 */
        public ulong ImpersonationLocale;                       /* f98/1798 */
        public ulong IsImpersonating;                           /* f9c/179c */
        public IntPtr NlsCache;                                 /* fa0/17a0 */
        public IntPtr ShimData;                                 /* fa4/17a8 */
        public ulong HeapVirtualAffinity;                       /* fa8/17b0 */
        public IntPtr CurrentTransactionHandle;                 /* fac/17b8 */
        public IntPtr ActiveFrame;                              /* fb0/17c0 */
        public IntPtr FlsSlots;                                 /* fb4/17c8 */
    }
}
