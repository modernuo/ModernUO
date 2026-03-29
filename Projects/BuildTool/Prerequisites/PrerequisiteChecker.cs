using BuildTool.Platform;
using Spectre.Console;

namespace BuildTool.Prerequisites;

public static class PrerequisiteChecker
{
    /// <summary>
    /// Runs all prerequisite checks and displays results.
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

        var allPassed = true;

        // 1. Check .NET SDK
        var sdkResult = DotNetSdkManager.CheckSdk(repoRoot);
        DisplayResult(sdkResult);

        if (!sdkResult.Passed)
        {
            var installed = DotNetSdkManager.OfferInstall(platform, interactive);
            if (!installed)
            {
                return false;
            }

            // Re-check after install
            sdkResult = DotNetSdkManager.CheckSdk(repoRoot);
            if (!sdkResult.Passed)
            {
                AnsiConsole.MarkupLine("[red]SDK installation may require a terminal restart.[/]");
                return false;
            }

            DisplayResult(sdkResult);
        }

        // 2. Check native libraries
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

        // Display aggregate install commands
        if (hasMissing)
        {
            allPassed = false;
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
        }
        else
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]:check_mark_button: All prerequisites satisfied.[/]");
        }

        AnsiConsole.WriteLine();
        return allPassed || interactive;
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
