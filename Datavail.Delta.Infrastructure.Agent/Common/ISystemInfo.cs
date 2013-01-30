using System.IO;

namespace Datavail.Delta.Infrastructure.Agent.Common
{
    public interface ISystemInfo
    {
        void GetDiskFreeSpace(string path, out long totalBytes, out long totalFreeBytes);
        string[] GetLogicalDrives();
        void GetDriveInfo(string path, out DriveType driveType, out string driveFormat, out long totalSize, out string volumeLabel);
        void GetRamInfo(out ulong totalPhysicalMemory, out ulong totalVirtualMemory, out ulong availablePhysicalMemory, out ulong availableVirtualMemory);
        float GetCpuUtilization();
    }
}