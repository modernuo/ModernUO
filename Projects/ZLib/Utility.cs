using System.Runtime.InteropServices;

namespace Server
{
  internal static class RuntimeUtility
  {
    internal static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    internal static readonly bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    internal static readonly bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    internal static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
    internal static readonly bool IsUnix = IsDarwin || IsFreeBSD || IsLinux;
  }
}
