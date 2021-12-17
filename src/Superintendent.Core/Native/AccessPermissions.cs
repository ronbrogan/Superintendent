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

        // process
        Terminate = 0x0001,
        CreateThread = 0x0002,
        SetSessionID = 0x0004,
        VmOperation = 0x0008,
        VmRead = 0x0010,
        VmWrite = 0x0020,
        DupHandle = 0x0040,
        CreateProcess = 0x0080,
        SetQuota = 0x0100,
        SetInformation = 0x0200,
        QueryInformation = 0x0400,
        SuspendResume = 0x0800,
        QueryLimitedInformation = 0x1000,
        SetLimitedInformation = 0x2000,
        AllAccess = StandardRights_Required | Synchronize | 0xFFFF
    }
}
