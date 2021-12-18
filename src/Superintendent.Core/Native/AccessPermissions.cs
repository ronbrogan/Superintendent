using System;

namespace Superintendent.Core.Native
{
    [Flags]
    public enum AccessPermissions
    {
        Default = 0,
        Delete = 0x00010000,
        ReadControl = 0x00020000,
        WriteDac = 0x00040000,
        WriteOwner = 0x00080000,
        Synchronize = 0x00100000,
        StandardRights_Required = 0x000F0000,
        StandardRights_Read = ReadControl,
        StandardRights_Write = ReadControl,
        StandardRights_Execute = ReadControl,
        StandardRights_ALL = 0x001F0000,
        SpecificRights_ALL = 0x0000FFFF,

        ProcessTerminate = 0x0001,
        ProcessCreateThread = 0x0002,
        ProcessSetSessionID = 0x0004,
        ProcessVmOperation = 0x0008,
        ProcessVmRead = 0x0010,
        ProcessVmWrite = 0x0020,
        ProcessDupHandle = 0x0040,
        ProcessCreateProcess = 0x0080,
        ProcessSetQuota = 0x0100,
        ProcessSetInformation = 0x0200,
        ProcessQueryInformation = 0x0400,
        ProcessSuspendResume = 0x0800,
        ProcessQueryLimitedInformation = 0x1000,
        ProcessSetLimitedInformation = 0x2000,
        ProcessAllAccess = StandardRights_Required | Synchronize | 0xFFFF
    }
}
