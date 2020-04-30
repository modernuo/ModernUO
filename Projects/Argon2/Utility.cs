using System.Runtime.InteropServices;

namespace Server
{
  internal static class RuntimeUtility
  {
    public static bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsDarwin = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    public static bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsFreeBSD;
    public static bool Unix = IsDarwin || IsFreeBSD || IsLinux;
  }
}
