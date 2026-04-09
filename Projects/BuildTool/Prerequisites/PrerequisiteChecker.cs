using BuildTool.Platform;
using Spectre.Console;

namespace BuildTool.Prerequisites;

public static class PrerequisiteChecker
{
    /// <summary>
    /// Runs all prerequisite checks (SDK + native libraries) and displays results.
    /// Returns true if all critical prerequisites pass.
    /// </summary>
    public static bool CheckAll(PlatformInfo platform, string repoRoot, bool interactive)
    {
        var panel = new Panel("[bold]Checking prerequisites[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Branding.Gold)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (!CheckSdkInternal(platform, repoRoot, interactive))
        {
            return false;
        }

        return CheckNativeLibrariesInternal(platform, interactive);
    }

    /// <summary>
    /// Checks only the .NET SDK (sufficient for cross-compilation).
    /// Returns true if the SDK is available.
    /// </summary>
    public static bool CheckSdk(PlatformInfo platform, string repoRoot, bool interactive)
    {
        var panel = new Panel("[bold]Checking .NET SDK[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Branding.Gold)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var passed = CheckSdkInternal(platform, repoRoot, interactive);
        AnsiConsole.WriteLine();
        return passed;
    }

    /// <summary>
    /// Checks native libraries for the current platform.
    /// Call this only when the publish target matches the current OS.
    /// Returns true if all pass or the user chose to continue.
    /// </summary>
    public static bool CheckNativeLibraries(PlatformInfo platform, bool interactive)
    {
        AnsiConsole.Write(new Panel("[bold]Checking native libraries[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Branding.Gold)
            .Padding(1, 0));
        AnsiConsole.WriteLine();

        var passed = CheckNativeLibrariesInternal(platform, interactive);
        AnsiConsole.WriteLine();
        return passed;
    }

    private static bool CheckSdkInternal(PlatformInfo platform, string repoRoot, bool interactive)
    {
        var sdkResult = DotNetSdkManager.CheckSdk(repoRoot);
        DisplayResult(sdkResult);

        if (!sdkResult.Passed)
        {
            var installed = DotNetSdkManager.OfferInstall(platform, repoRoot, interactive);
            if (!installed)
            {
                return false;
            }

            sdkResult = DotNetSdkManager.CheckSdk(repoRoot);
            if (!sdkResult.Passed)
            {
                AnsiConsole.MarkupLine("[red]SDK installation may require a terminal restart.[/]");
                return false;
            }

            DisplayResult(sdkResult);
        }

        return true;
    }

    private static bool CheckNativeLibrariesInternal(PlatformInfo platform, bool interactive)
    {
        var nativeResults = NativeLibraryChecker.Check(platform);
        var hasMissing = false;

        foreach (var result in nativeResults)
        {
            if (result.IsWarning)
            {
                continue;
            }

            DisplayResult(result);
            if (!result.Passed)
            {
                hasMissing = true;
            }
        }

        if (hasMissing)
        {
            AnsiConsole.WriteLine();

            foreach (var result in nativeResults)
            {
                if (!result.IsWarning)
                {
                    continue;
                }

                if (result.InstallCommand is not null)
                {
                    AnsiConsole.MarkupLine($"  [yellow]:warning: {result.Details}[/]");
                    AnsiConsole.MarkupLine($"  [white on grey23] {result.InstallCommand} [/]");
                    AnsiConsole.WriteLine();
                }
            }

            if (interactive)
            {
                var continueAnyway = AnsiConsole.Confirm(
                    "[yellow]Some prerequisites are missing. Continue anyway?[/]",
                    defaultValue: false
                );

                if (!continueAnyway)
                {
                    return false;
                }
            }

            return interactive; // In interactive mode, user chose to continue
        }

        AnsiConsole.MarkupLine("[green]:check_mark_button: All prerequisites satisfied.[/]");
        return true;
    }

    private static void DisplayResult(PrerequisiteResult result)
    {
        if (result.IsWarning)
        {
            return;
        }

        var icon = result.Passed ? "[green]:check_mark:[/]" : "[red]:cross_mark:[/]";
        var details = result.Details is not null ? $" [grey]({Markup.Escape(result.Details)})[/]" : "";
        AnsiConsole.MarkupLine($"  {icon} {Markup.Escape(result.Name)}{details}");

        if (!result.Passed && result.DownloadUrl is not null)
        {
            AnsiConsole.MarkupLine($"    [grey]Download: {result.DownloadUrl}[/]");
        }
    }
}
