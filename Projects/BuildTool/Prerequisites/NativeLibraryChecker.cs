using System.Runtime.InteropServices;
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
                    "Debian/Ubuntu:  sudo apt-get install -y libdeflate0 libargon2-1 libicuNN tzdata",
                    "                (libicuNN varies by release, e.g. libicu76 — run build-tool --check-prereqs there for the exact name)",
                    "                (add tzdata-legacy if the shard is configured with an alias such as US/Eastern)",
                    "Fedora/RHEL:    sudo dnf install -y libdeflate libargon2 libicu tzdata",
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
            // Check exact match or versioned variant (e.g. "icu4c@78" matches "icu4c")
            var installed = installedFormulae.Contains(formula) ||
                installedFormulae.Any(f => f.StartsWith($"{formula}@", StringComparison.OrdinalIgnoreCase));
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

    /// <summary>
    /// Native libraries the server needs from the system on Linux, and the SONAME range to accept
    /// for each. Rationale and per-distro package names: dev-docs/platform-prerequisites.md.
    /// </summary>
    private static readonly (string Name, int MinSoVersion, int MaxSoVersion)[] _linuxLibraries =
    [
        ("libicuuc", 60, 120),
        ("libicui18n", 60, 120),
        ("libdeflate", 0, 9),
        ("libargon2", 0, 9)
    ];

    private static List<PrerequisiteResult> CheckLinux(PlatformInfo platform)
    {
        var results = new List<PrerequisiteResult>();
        var missing = new List<string>();

        foreach (var (name, minSoVersion, maxSoVersion) in _linuxLibraries)
        {
            var found = CanLoad(name, minSoVersion, maxSoVersion);

            if (!found)
            {
                missing.Add(name);
            }

            results.Add(new PrerequisiteResult
            {
                Name = name,
                Passed = found,
                Details = found ? "Found" : "Not found"
            });
        }

        var hasTimeZoneData = HasTimeZoneData();
        if (!hasTimeZoneData)
        {
            missing.Add("tzdata");
        }

        results.Add(new PrerequisiteResult
        {
            Name = "tzdata",
            Passed = hasTimeZoneData,
            Details = hasTimeZoneData ? "Found" : "Not found — every zone except UTC will throw"
        });

        if (missing.Count == 0)
        {
            return results;
        }

        if (platform.DistroId?.Equals("centos", StringComparison.OrdinalIgnoreCase) == true)
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

        results.Add(new PrerequisiteResult
        {
            Name = "Install all missing",
            Passed = false,
            IsWarning = true,
            Details = "Install the missing dependencies. The -dev/-devel packages are not required:",
            InstallCommand = BuildInstallCommand(platform, missing)
        });

        return results;
    }

    /// <summary>
    /// tzdata is data, not a library, so no loader probe finds it. Asking the runtime rather than
    /// stat'ing a path keeps TZDIR honoured, and the count is still accurate under
    /// InvariantGlobalization, which this tool runs with — only display names degrade there.
    /// </summary>
    private static bool HasTimeZoneData()
    {
        try
        {
            return TimeZoneInfo.GetSystemTimeZones().Count > 1;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Asks the loader directly rather than querying a package database or scanning ldconfig's
    /// cache, both of which answer a different question and can disagree with what dlopen will do.
    /// Mirrors the binding packages' own probing: the unversioned name first, then libfoo.so.N
    /// descending. Bare names go through the full loader search path, so LD_LIBRARY_PATH and
    /// /etc/ld.so.conf.d still apply.
    /// </summary>
    private static bool CanLoad(string library, int minSoVersion, int maxSoVersion)
    {
        if (TryLoadAndFree($"{library}.so"))
        {
            return true;
        }

        for (var soVersion = maxSoVersion; soVersion >= minSoVersion; soVersion--)
        {
            if (TryLoadAndFree($"{library}.so.{soVersion}"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryLoadAndFree(string candidate)
    {
        if (!NativeLibrary.TryLoad(candidate, out var handle))
        {
            return false;
        }

        NativeLibrary.Free(handle);
        return true;
    }

    private static string BuildInstallCommand(PlatformInfo platform, List<string> missing)
    {
        switch (platform.PackageManager)
        {
            case PackageManager.Apt:
                {
                    // Distinct because the two ICU libraries resolve to the same package, and
                    // ResolveAptIcuPackage shells out, so it is memoized rather than called per name.
                    var packages = missing.Select(
                        library => library switch
                        {
                            "libdeflate" => "libdeflate0",
                            "libargon2"  => "libargon2-1",
                            "tzdata"     => "tzdata",
                            _            => _aptIcuPackage ??= ResolveAptIcuPackage()
                        }
                    ).Distinct();

                    return $"sudo apt-get install -y {string.Join(' ', packages)}";
                }
            case PackageManager.Dnf:
                {
                    var packages = missing.Select(
                        library => library switch
                        {
                            "libdeflate" => "libdeflate",
                            "libargon2"  => "libargon2",
                            "tzdata"     => "tzdata",
                            _            => "libicu"
                        }
                    ).Distinct();

                    return $"sudo dnf install -y {string.Join(' ', packages)}";
                }
            default:
                return $"Install your distribution's runtime packages for: {string.Join(", ", missing)}";
        }
    }

    private static string _aptIcuPackage;

    /// <summary>
    /// ICU's apt package carries the ABI version in its name and there is no stable alias, so ask
    /// apt which one this release actually ships instead of printing a name that rots.
    /// </summary>
    private static string ResolveAptIcuPackage()
    {
        var result = ProcessRunner.RunCaptured("apt-cache", "search --names-only ^libicu[0-9]+$");
        if (!result.Success)
        {
            return "libicu";
        }

        var best = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', 2)[0].Trim())
            .Where(name => name.StartsWith("libicu", StringComparison.Ordinal))
            .OrderBy(name => int.TryParse(name.AsSpan(6), out var version) ? version : 0)
            .LastOrDefault();

        return best ?? "libicu";
    }
}
