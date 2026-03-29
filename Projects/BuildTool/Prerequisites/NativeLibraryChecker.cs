using BuildTool.Platform;
using BuildTool.Publishing;

namespace BuildTool.Prerequisites;

public static class NativeLibraryChecker
{
    /// <summary>
    /// Returns the required prerequisites for a target OS (without checking the local machine).
    /// Used to inform users what they need to install on the deployment target after cross-compiling.
    /// </summary>
    public static (string Description, string[] InstallCommands) GetRequirementsForTarget(string targetOs)
    {
        return targetOs switch
        {
            "win" => (
                "Windows",
                [
                    ".NET 10 Runtime — https://dotnet.microsoft.com/download/dotnet/10.0",
                    "VC++ Redistributable v14 — https://aka.ms/vs/17/release/vc_redist.x64.exe"
                ]
            ),
            "osx" => (
                "macOS",
                [
                    ".NET 10 Runtime — https://dotnet.microsoft.com/download/dotnet/10.0",
                    "brew install icu4c libdeflate zstd argon2"
                ]
            ),
            "linux" => (
                "Linux",
                [
                    ".NET 10 Runtime — https://dotnet.microsoft.com/download/dotnet/10.0",
                    "Debian/Ubuntu:  sudo apt-get install -y libicu-dev libdeflate-dev zstd libargon2-dev liburing-dev",
                    "Fedora/RHEL:    sudo dnf install -y libicu libdeflate-devel zstd libargon2-devel liburing-devel",
                    "CentOS:         Also requires epel-release and CRB enabled"
                ]
            ),
            _ => ("Unknown", [".NET 10 Runtime — https://dotnet.microsoft.com/download/dotnet/10.0"])
        };
    }

    public static List<PrerequisiteResult> Check(PlatformInfo platform)
    {
        if (platform.IsWindows)
        {
            return CheckWindows(platform);
        }

        if (platform.IsMacOS)
        {
            return CheckMacOS();
        }

        if (platform.IsLinux)
        {
            return CheckLinux(platform);
        }

        return [];
    }

    private static List<PrerequisiteResult> CheckWindows(PlatformInfo platform)
    {
        var results = new List<PrerequisiteResult>();

        // Check VC++ Redistributable via registry
        var vcRedistInstalled = CheckVcRedist(platform.ArchRid);
        var downloadUrl = platform.ArchRid == "arm64"
            ? "https://aka.ms/vs/17/release/vc_redist.arm64.exe"
            : "https://aka.ms/vs/17/release/vc_redist.x64.exe";

        results.Add(new PrerequisiteResult
        {
            Name = "VC++ Redistributable v14",
            Passed = vcRedistInstalled,
            Details = vcRedistInstalled ? "Installed" : "Not found",
            DownloadUrl = vcRedistInstalled ? null : downloadUrl
        });

        return results;
    }

    private static bool CheckVcRedist(string arch)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        // Check multiple known registry paths for VC++ 14.x Redistributable
        string[] registryPaths =
        [
            $@"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\{arch}",
            $@"SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\{arch}"
        ];

        foreach (var path in registryPaths)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(path);
                if (key?.GetValue("Installed") is int installed && installed == 1)
                {
                    return true;
                }
            }
            catch
            {
                // Registry access may fail, continue checking
            }
        }

        return false;
    }

    private static List<PrerequisiteResult> CheckMacOS()
    {
        var results = new List<PrerequisiteResult>();

        // Check if Homebrew is installed
        var brewResult = ProcessRunner.RunCaptured("which", "brew");
        if (!brewResult.Success)
        {
            results.Add(new PrerequisiteResult
            {
                Name = "Homebrew",
                Passed = false,
                Details = "Homebrew is required to install native dependencies",
                DownloadUrl = "https://brew.sh"
            });
            return results;
        }

        // Check required Homebrew formulae
        var formulae = new[] { "icu4c", "libdeflate", "zstd", "argon2" };
        var listResult = ProcessRunner.RunCaptured("brew", "list --formula");
        var installedFormulae = listResult.Success
            ? new HashSet<string>(
                listResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var missing = new List<string>();
        foreach (var formula in formulae)
        {
            var installed = installedFormulae.Contains(formula);
            if (!installed)
            {
                missing.Add(formula);
            }

            results.Add(new PrerequisiteResult
            {
                Name = formula,
                Passed = installed,
                Details = installed ? "Installed" : "Not installed",
                InstallCommand = installed ? null : $"brew install {formula}"
            });
        }

        if (missing.Count > 0)
        {
            results.Add(new PrerequisiteResult
            {
                Name = "Install all missing",
                Passed = false,
                IsWarning = true,
                Details = "Run the following command to install all missing dependencies:",
                InstallCommand = $"brew install {string.Join(' ', missing)}"
            });
        }

        return results;
    }

    private static List<PrerequisiteResult> CheckLinux(PlatformInfo platform)
    {
        return platform.PackageManager switch
        {
            PackageManager.Apt => CheckLinuxApt(),
            PackageManager.Dnf => CheckLinuxDnf(platform),
            _ => CheckLinuxGeneric(platform)
        };
    }

    private static List<PrerequisiteResult> CheckLinuxApt()
    {
        var results = new List<PrerequisiteResult>();
        var packages = new[] { "libicu-dev", "libdeflate-dev", "zstd", "libargon2-dev", "liburing-dev" };
        var missing = new List<string>();

        foreach (var package in packages)
        {
            var result = ProcessRunner.RunCaptured("dpkg", $"-l {package}");
            var installed = result.Success && result.StandardOutput.Contains("ii");

            if (!installed)
            {
                missing.Add(package);
            }

            results.Add(new PrerequisiteResult
            {
                Name = package,
                Passed = installed,
                Details = installed ? "Installed" : "Not installed"
            });
        }

        if (missing.Count > 0)
        {
            results.Add(new PrerequisiteResult
            {
                Name = "Install all missing",
                Passed = false,
                IsWarning = true,
                Details = "Run the following command to install all missing dependencies:",
                InstallCommand = $"sudo apt-get install -y {string.Join(' ', missing)}"
            });
        }

        return results;
    }

    private static List<PrerequisiteResult> CheckLinuxDnf(PlatformInfo platform)
    {
        var results = new List<PrerequisiteResult>();
        var packages = new[] { "libicu", "libdeflate-devel", "zstd", "libargon2-devel", "liburing-devel" };
        var missing = new List<string>();

        foreach (var package in packages)
        {
            var result = ProcessRunner.RunCaptured("rpm", $"-q {package}");
            var installed = result.Success;

            if (!installed)
            {
                missing.Add(package);
            }

            results.Add(new PrerequisiteResult
            {
                Name = package,
                Passed = installed,
                Details = installed ? "Installed" : "Not installed"
            });
        }

        // Check if this is CentOS (needs EPEL)
        var isCentOs = platform.DistroId?.Equals("centos", StringComparison.OrdinalIgnoreCase) == true;
        if (isCentOs && missing.Count > 0)
        {
            results.Add(new PrerequisiteResult
            {
                Name = "EPEL Repository",
                Passed = false,
                IsWarning = true,
                Details = "CentOS requires EPEL for some packages. Enable it first:",
                InstallCommand = "sudo dnf install -y epel-release epel-next-release && sudo dnf config-manager --set-enabled crb"
            });
        }

        if (missing.Count > 0)
        {
            results.Add(new PrerequisiteResult
            {
                Name = "Install all missing",
                Passed = false,
                IsWarning = true,
                Details = "Run the following command to install all missing dependencies:",
                InstallCommand = $"sudo dnf install -y {string.Join(' ', missing)}"
            });
        }

        return results;
    }

    private static List<PrerequisiteResult> CheckLinuxGeneric(PlatformInfo platform)
    {
        var results = new List<PrerequisiteResult>();

        // Use ldconfig to check for shared libraries
        var ldResult = ProcessRunner.RunCaptured("ldconfig", "-p");
        var ldOutput = ldResult.Success ? ldResult.StandardOutput : "";

        var libraries = new Dictionary<string, string>
        {
            ["libicu"] = "libicuuc",
            ["libdeflate"] = "libdeflate",
            ["zstd"] = "libzstd",
            ["libargon2"] = "libargon2",
            ["liburing"] = "liburing"
        };

        foreach (var (name, soName) in libraries)
        {
            var found = ldOutput.Contains(soName, StringComparison.OrdinalIgnoreCase);
            results.Add(new PrerequisiteResult
            {
                Name = name,
                Passed = found,
                Details = found ? "Found" : "Not found — install using your package manager"
            });
        }

        return results;
    }
}
