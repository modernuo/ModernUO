using System.Text.Json;
using System.Text.RegularExpressions;
using BuildTool.Json;
using BuildTool.Platform;
using BuildTool.Publishing;
using Spectre.Console;

namespace BuildTool.Prerequisites;

public static partial class DotNetSdkManager
{
    private const string DotNetDownloadUrl = "https://dotnet.microsoft.com/download";
    private const string InstallScriptUrlWindows = "https://dot.net/v1/dotnet-install.ps1";
    private const string InstallScriptUrlUnix = "https://dot.net/v1/dotnet-install.sh";

    public static PrerequisiteResult CheckSdk(string repoRoot)
    {
        var requiredVersionStr = ReadRequiredVersion(repoRoot) ?? "10.0.201";

        if (!Version.TryParse(requiredVersionStr, out var requiredVersion))
        {
            requiredVersion = new Version(10, 0, 201);
        }

        // Check if dotnet is on PATH
        var result = ProcessRunner.RunCaptured("dotnet", "--list-sdks");
        if (!result.Success)
        {
            return new PrerequisiteResult
            {
                Name = ".NET SDK",
                Passed = false,
                Details = $".NET SDK {requiredVersionStr}+ is not installed",
                DownloadUrl = DotNetDownloadUrl
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
                    ? $".NET SDK {bestVersionStr} found, but {requiredVersionStr}+ is required"
                    : $".NET SDK {requiredVersionStr}+ is required but no SDK was found",
                DownloadUrl = DotNetDownloadUrl
            };
        }

        return new PrerequisiteResult
        {
            Name = ".NET SDK",
            Passed = true,
            Details = $".NET SDK {bestVersionStr}"
        };
    }

    public static bool OfferInstall(PlatformInfo platform, bool interactive)
    {
        if (!interactive)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] .NET SDK is not installed. Download from:");
            AnsiConsole.MarkupLine($"[link]{DotNetDownloadUrl}[/]");
            return false;
        }

        var install = AnsiConsole.Confirm(
            "[yellow].NET SDK is required but not found.[/] Would you like to install it now?",
            defaultValue: true
        );

        if (!install)
        {
            AnsiConsole.MarkupLine($"\nDownload the .NET SDK from: [link]{DotNetDownloadUrl}[/]");
            return false;
        }

        return RunInstallScript(platform);
    }

    private static bool RunInstallScript(PlatformInfo platform)
    {
        AnsiConsole.MarkupLine("\n[blue]Installing .NET SDK...[/]");

        if (platform.IsWindows)
        {
            return RunWindowsInstall();
        }

        return RunUnixInstall();
    }

    private static bool RunWindowsInstall()
    {
        // Download and run the official Microsoft install script
        var result = ProcessRunner.RunPassthrough(
            "powershell",
            $"-NoProfile -ExecutionPolicy Bypass -Command \"& {{ " +
            $"[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; " +
            $"$script = Invoke-WebRequest -Uri '{InstallScriptUrlWindows}' -UseBasicParsing; " +
            $"$scriptBlock = [scriptblock]::Create($script.Content); " +
            $"& $scriptBlock -Channel 10.0 }}\""
        );

        if (result != 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to install .NET SDK.[/]");
            AnsiConsole.MarkupLine($"Please install manually from: [link]{DotNetDownloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("[green]Successfully installed .NET SDK.[/]");
        AnsiConsole.MarkupLine("[yellow]Note:[/] You may need to restart your terminal for the PATH to update.");
        return true;
    }

    private static bool RunUnixInstall()
    {
        // Download and run the official Microsoft install script
        var result = ProcessRunner.RunPassthrough(
            "bash",
            $"-c \"curl -fsSL {InstallScriptUrlUnix} | bash -s -- --channel 10.0\""
        );

        if (result != 0)
        {
            AnsiConsole.MarkupLine("[red]Failed to install .NET SDK.[/]");
            AnsiConsole.MarkupLine($"Please install manually from: [link]{DotNetDownloadUrl}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("[green]Successfully installed .NET SDK.[/]");
        AnsiConsole.MarkupLine("[yellow]Note:[/] You may need to add ~/.dotnet to your PATH or restart your terminal.");
        return true;
    }

    private static string? ReadRequiredVersion(string repoRoot)
    {
        var globalJsonPath = Path.Combine(repoRoot, "global.json");
        if (!File.Exists(globalJsonPath))
        {
            return null;
        }

        var json = File.ReadAllText(globalJsonPath);
        var globalJson = JsonSerializer.Deserialize(json, GlobalJsonContext.Default.GlobalJson);
        return globalJson?.Sdk?.Version;
    }

    // Kept for reference but no longer used — Version.TryParse handles comparison now
    private static int ParseMajorVersion(string version)
    {
        var dotIndex = version.IndexOf('.');
        if (dotIndex <= 0)
        {
            return 0;
        }

        return int.TryParse(version.AsSpan(0, dotIndex), out var major) ? major : 0;
    }

    [GeneratedRegex(@"^(\d+\.\d+\.\d+\S*)")]
    private static partial Regex SdkVersionRegex();
}
