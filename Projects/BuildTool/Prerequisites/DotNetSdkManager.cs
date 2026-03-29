using System.Text.Json;
using System.Text.RegularExpressions;
using BuildTool.Json;
using BuildTool.Platform;
using BuildTool.Publishing;
using Spectre.Console;

namespace BuildTool.Prerequisites;

public static partial class DotNetSdkManager
{
    private const string DotNetLinuxInstallUrl = "https://learn.microsoft.com/en-us/dotnet/core/install/linux";

    /// <summary>
    /// Returns the .NET download page URL for the required SDK's major.minor channel.
    /// </summary>
    private static string GetDownloadUrl(Version version) =>
        $"https://dotnet.microsoft.com/download/dotnet/{version.Major}.{version.Minor}";

    /// <summary>
    /// Returns the direct installer download URL for a specific SDK version + platform.
    /// Windows uses .exe, macOS uses .pkg.
    /// </summary>
    private static string GetInstallerUrl(Version version, string rid, string extension) =>
        $"https://builds.dotnet.microsoft.com/dotnet/Sdk/{version}/dotnet-sdk-{version}-{rid}.{extension}";

    public static PrerequisiteResult CheckSdk(string repoRoot)
    {
        var requiredVersion = ReadRequiredVersion(repoRoot);
        var downloadUrl = GetDownloadUrl(requiredVersion);

        // Check if dotnet is on PATH
        var result = ProcessRunner.RunCaptured("dotnet", "--list-sdks");
        if (!result.Success)
        {
            return new PrerequisiteResult
            {
                Name = ".NET SDK",
                Passed = false,
                Details = $".NET SDK {requiredVersion}+ is not installed",
                DownloadUrl = downloadUrl
            };
        }

        // Parse installed SDK versions — find the best match
        Version? bestVersion = null;
        string? bestVersionStr = null;

        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = SdkVersionRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var versionStr = match.Groups[1].Value;
            if (!Version.TryParse(versionStr, out var version))
            {
                continue;
            }

            if (bestVersion is null || version > bestVersion)
            {
                bestVersion = version;
                bestVersionStr = versionStr;
            }
        }

        if (bestVersion is null || bestVersion < requiredVersion)
        {
            return new PrerequisiteResult
            {
                Name = ".NET SDK",
                Passed = false,
                Details = bestVersionStr is not null
                    ? $".NET SDK {bestVersionStr} found, but {requiredVersion}+ is required"
                    : $".NET SDK {requiredVersion}+ is required but no SDK was found",
                DownloadUrl = downloadUrl
            };
        }

        return new PrerequisiteResult
        {
            Name = ".NET SDK",
            Passed = true,
            Details = $".NET SDK {bestVersionStr}"
        };
    }

    public static bool OfferInstall(PlatformInfo platform, string repoRoot, bool interactive)
    {
        var requiredVersion = ReadRequiredVersion(repoRoot);
        var downloadUrl = GetDownloadUrl(requiredVersion);

        if (!interactive)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] .NET SDK is not installed.");
            AnsiConsole.MarkupLine($"  Download from: [link]{downloadUrl}[/]");
            return false;
        }

        if (platform.IsWindows)
        {
            return OfferWindowsInstall(platform, requiredVersion, downloadUrl);
        }

        if (platform.IsMacOS)
        {
            return OfferMacOsInstall(platform, requiredVersion, downloadUrl);
        }

        // Linux — just show instructions
        AnsiConsole.MarkupLine($"\n[yellow]Install the .NET SDK using your package manager:[/]");
        AnsiConsole.MarkupLine($"  [link]{DotNetLinuxInstallUrl}[/]");
        AnsiConsole.WriteLine();
        return false;
    }

    private static bool OfferWindowsInstall(PlatformInfo platform, Version requiredVersion, string downloadUrl)
    {
        var rid = $"win-{platform.ArchRid}";
        var installerUrl = GetInstallerUrl(requiredVersion, rid, "exe");

        var install = AnsiConsole.Confirm(
            "[yellow].NET SDK is required. Download and run the installer?[/]",
            defaultValue: true
        );

        if (!install)
        {
            AnsiConsole.MarkupLine($"\n  Download manually from: [link]{downloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("\n[blue]Opening .NET SDK installer...[/]");

        var result = ProcessRunner.RunPassthrough("cmd", $"/c start \"\" \"{installerUrl}\"");
        if (result != 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to open installer.[/]");
            AnsiConsole.MarkupLine($"  Download manually from: [link]{downloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("[yellow]Install the SDK, then restart your terminal and run this tool again.[/]");
        return false; // User needs to restart after installing
    }

    private static bool OfferMacOsInstall(PlatformInfo platform, Version requiredVersion, string downloadUrl)
    {
        var rid = $"osx-{platform.ArchRid}";
        var installerUrl = GetInstallerUrl(requiredVersion, rid, "pkg");

        var install = AnsiConsole.Confirm(
            "[yellow].NET SDK is required. Download and run the installer?[/]",
            defaultValue: true
        );

        if (!install)
        {
            AnsiConsole.MarkupLine($"\n  Download manually from: [link]{downloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("\n[blue]Opening .NET SDK installer...[/]");

        var result = ProcessRunner.RunPassthrough("open", installerUrl);
        if (result != 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to open installer.[/]");
            AnsiConsole.MarkupLine($"  Download manually from: [link]{downloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("[yellow]Install the SDK, then restart your terminal and run this tool again.[/]");
        return false; // User needs to restart after installing
    }

    /// <summary>
    /// Reads the required SDK version from global.json.
    /// Falls back to 10.0 if global.json is missing or unparseable.
    /// </summary>
    private static Version ReadRequiredVersion(string repoRoot)
    {
        var globalJsonPath = Path.Combine(repoRoot, "global.json");
        if (File.Exists(globalJsonPath))
        {
            var json = File.ReadAllText(globalJsonPath);
            var globalJson = JsonSerializer.Deserialize(json, GlobalJsonContext.Default.GlobalJson);
            if (globalJson?.Sdk?.Version is { } versionStr && Version.TryParse(versionStr, out var version))
            {
                return version;
            }
        }

        return new Version(10, 0);
    }

    [GeneratedRegex(@"^(\d+\.\d+\.\d+\S*)")]
    private static partial Regex SdkVersionRegex();
}
