using System.Runtime.InteropServices;
using BuildTool.Publishing;

namespace BuildTool.Platform;

public static class PlatformDetector
{
    // Windows 11 starts at build 22000
    private const int Windows11MinBuild = 22000;

    public static PlatformInfo Detect()
    {
        var osRid = GetOsRid();
        var archRid = GetArchRid();
        var (distroId, distroName) = GetLinuxDistroInfo();
        var kernelVersion = GetKernelVersion(osRid);
        var osName = GetOsDisplayName(osRid, distroName, kernelVersion);
        var packageManager = DetectPackageManager(osRid, distroId);

        return new PlatformInfo
        {
            OsName = osName,
            OsRid = osRid,
            ArchRid = archRid,
            DistroId = distroId,
            DistroName = distroName,
            KernelVersion = kernelVersion,
            PackageManager = packageManager
        };
    }

    private static string GetOsDisplayName(string osRid, string? distroName, string? kernelVersion)
    {
        if (OperatingSystem.IsWindows())
        {
            var version = Environment.OSVersion.Version;
            var windowsVersion = version.Build >= Windows11MinBuild ? "11" : "10";
            return $"Windows {windowsVersion} (Build {version.Build})";
        }

        if (OperatingSystem.IsMacOS())
        {
            var version = Environment.OSVersion.Version;
            var macosName = GetMacOSCodename(version.Major);
            return macosName is not null
                ? $"macOS {version.Major}.{version.Minor} {macosName}"
                : $"macOS {version.Major}.{version.Minor}";
        }

        // Linux: show distro name with kernel version
        if (distroName is not null && kernelVersion is not null)
        {
            return $"{distroName} (kernel {kernelVersion})";
        }

        return distroName ?? (kernelVersion is not null ? $"Linux (kernel {kernelVersion})" : "Linux");
    }

    private static string? GetMacOSCodename(int majorVersion) =>
        majorVersion switch
        {
            15 => "Sequoia",
            14 => "Sonoma",
            13 => "Ventura",
            12 => "Monterey",
            _ => null
        };

    private static string GetOsRid()
    {
        if (OperatingSystem.IsWindows())
        {
            return "win";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx";
        }

        return "linux";
    }

    private static string GetArchRid() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => "x64"
        };

    private static string? GetKernelVersion(string osRid)
    {
        if (osRid != "linux")
        {
            return null;
        }

        // Try /proc/version first (most reliable)
        if (File.Exists("/proc/version"))
        {
            try
            {
                var procVersion = File.ReadAllText("/proc/version");
                // Format: "Linux version 6.5.0-44-generic ..."
                var parts = procVersion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0] == "Linux" && parts[1] == "version")
                {
                    return parts[2];
                }
            }
            catch
            {
                // Fall through to uname
            }
        }

        // Fall back to uname -r
        var result = ProcessRunner.RunCaptured("uname", "-r");
        return result.Success ? result.StandardOutput.Trim() : null;
    }

    private static (string? Id, string? Name) GetLinuxDistroInfo()
    {
        if (!OperatingSystem.IsLinux())
        {
            return (null, null);
        }

        const string osReleasePath = "/etc/os-release";
        if (!File.Exists(osReleasePath))
        {
            return (null, null);
        }

        string? id = null;
        string? name = null;

        foreach (var line in File.ReadLines(osReleasePath))
        {
            if (line.StartsWith("ID=", StringComparison.Ordinal))
            {
                id = line[3..].Trim('"');
            }
            else if (line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))
            {
                name = line[12..].Trim('"');
            }

            if (id is not null && name is not null)
            {
                break;
            }
        }

        return (id, name);
    }

    private static PackageManager DetectPackageManager(string osRid, string? distroId)
    {
        if (osRid == "osx")
        {
            return PackageManager.Brew;
        }

        if (osRid != "linux")
        {
            return PackageManager.Unknown;
        }

        // Check distro ID first for accuracy
        return distroId?.ToLowerInvariant() switch
        {
            "ubuntu" or "debian" or "linuxmint" or "pop" or "elementary" or "zorin" => PackageManager.Apt,
            "fedora" or "centos" or "rhel" or "rocky" or "alma" or "ol" => PackageManager.Dnf,
            "opensuse" or "opensuse-leap" or "opensuse-tumbleweed" or "sles" => PackageManager.Zypper,
            "arch" or "manjaro" or "endeavouros" => PackageManager.Pacman,
            "alpine" => PackageManager.Apk,
            _ => DetectPackageManagerFromBinaries()
        };
    }

    private static PackageManager DetectPackageManagerFromBinaries()
    {
        if (File.Exists("/usr/bin/apt-get") || File.Exists("/usr/bin/apt"))
        {
            return PackageManager.Apt;
        }

        if (File.Exists("/usr/bin/dnf"))
        {
            return PackageManager.Dnf;
        }

        if (File.Exists("/usr/bin/zypper"))
        {
            return PackageManager.Zypper;
        }

        if (File.Exists("/usr/bin/pacman"))
        {
            return PackageManager.Pacman;
        }

        if (File.Exists("/sbin/apk"))
        {
            return PackageManager.Apk;
        }

        return PackageManager.Unknown;
    }
}
