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
                    "Debian/Ubuntu:  sudo apt-get install -y libdeflate0 libargon2-1 libicuNN",
                    "                (libicuNN varies by release, e.g. libicu76 — run build-tool --check-prereqs there for the exact name)",
                    "Fedora/RHEL:    sudo dnf install -y libdeflate libargon2 libicu",
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
    /// Libraries the server needs from the system on Linux.
    ///
    /// zstd is absent because ZstdNet bundles libzstd for every RID. liburing is absent because
    /// IORingGroup issues io_uring syscalls directly and imports only libc, libSystem.dylib,
    /// kernel32.dll, kernelbase.dll and ws2_32.dll.
    ///
    /// ICU is here because the server does not set InvariantGlobalization and its runtimeconfig
    /// sets System.Globalization.PredefinedCulturesOnly to false, so it genuinely needs ICU.
    ///
    /// MaxSoVersion bounds the dlopen fallback used when ldconfig cannot answer. It is per library
    /// because the SONAME digit is: libdeflate is .so.0 and libargon2 is .so.1 on the same machine,
    /// while ICU tracks its own release train and was .so.74 on Ubuntu 24.04, .so.76 on Alpine and
    /// .so.77 on Fedora. A single small bound silently reports ICU missing when it is installed.
    /// </summary>
    private static readonly (string Name, int MaxSoVersion)[] _linuxLibraries =
    [
        ("libicuuc", 99),
        ("libdeflate", 9),
        ("libargon2", 9)
    ];

    private static List<PrerequisiteResult> CheckLinux(PlatformInfo platform)
    {
        // Ask whether the loader can find each library rather than whether a named package is
        // installed. Package names were why the -dev packages were mandated, and no hardcoded name
        // works for ICU anyway: its apt package is release-specific (libicu72, libicu74, ...).
        // ldconfig -p is the loader's own cache, so matching on the "libfoo.so" prefix covers
        // libdeflate.so.0, libargon2.so.1 and libicuuc.so.76 alike.
        //
        // It is only ever a fast *positive* signal. musl's ldconfig exits 0 while producing no
        // usable cache, so trusting a negative from it reports every library missing on Alpine even
        // when all of them are installed. A cache can also be stale or omit LD_LIBRARY_PATH.
        // Anything it does not vouch for gets dlopen'd for real before being called missing.
        var ldResult = ProcessRunner.RunCaptured("ldconfig", "-p");
        var cache = ldResult.Success ? ldResult.StandardOutput : null;

        var results = new List<PrerequisiteResult>();
        var missing = new List<string>();

        foreach (var (name, maxSoVersion) in _linuxLibraries)
        {
            var found = cache?.Contains($"{name}.so", StringComparison.Ordinal) == true ||
                CanLoad(name, maxSoVersion);

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
            Details = "Install the runtime libraries. The -dev/-devel packages are not required:",
            InstallCommand = BuildInstallCommand(platform, missing)
        });

        return results;
    }

    /// <summary>
    /// Asks the loader directly, for when ldconfig cannot answer. Mirrors the binding packages'
    /// own probing: the unversioned name first, then libfoo.so.N descending. Bare names go through
    /// the full loader search path, so LD_LIBRARY_PATH and /etc/ld.so.conf.d still apply.
    /// </summary>
    private static bool CanLoad(string library, int maxSoVersion)
    {
        if (TryLoadAndFree($"{library}.so"))
        {
            return true;
        }

        for (var soVersion = maxSoVersion; soVersion >= 0; soVersion--)
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
                    var packages = missing.Select(
                        library => library switch
                        {
                            "libdeflate" => "libdeflate0",
                            "libargon2"  => "libargon2-1",
                            _            => ResolveAptIcuPackage()
                        }
                    );

                    return $"sudo apt-get install -y {string.Join(' ', packages)}";
                }
            case PackageManager.Dnf:
                {
                    var packages = missing.Select(
                        library => library switch
                        {
                            "libdeflate" => "libdeflate",
                            "libargon2"  => "libargon2",
                            _            => "libicu"
                        }
                    );

                    return $"sudo dnf install -y {string.Join(' ', packages)}";
                }
            default:
                return $"Install your distribution's runtime packages for: {string.Join(", ", missing)}";
        }
    }

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
