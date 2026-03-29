using BuildTool.Platform;
using BuildTool.Prerequisites;
using BuildTool.Publishing;
using Spectre.Console;

namespace BuildTool.Interactive;

/// <summary>
/// Handles interactive mode when the BuildTool is run without arguments.
/// Provides a polished menu-driven experience with proper Ctrl+C navigation.
/// Uses ModernUO brand gold color palette.
/// </summary>
public static class GuidedMode
{
    // Brand gold as markup string for inline use
    private const string GoldMarkup = "rgb(213,191,116)";
    private const string GoldLightMarkup = "rgb(223,198,136)";

    public static int Run(PlatformInfo platform, string repoRoot)
    {
        return ShowMainMenu(platform, repoRoot);
    }

    private static int ShowMainMenu(PlatformInfo platform, string repoRoot)
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Panel(new Text(Branding.Logo, Branding.GoldStyle))
                .Border(BoxBorder.Rounded)
                .BorderColor(Branding.Gold)
                .Padding(0, 0));
            AnsiConsole.Write(new Text($"  {Branding.Subtitle}\n", Branding.DimGoldStyle));
            AnsiConsole.WriteLine();

            // Show environment info
            var sdkVersion = GetDotNetSdkVersion();
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Branding.GoldDim)
                .HideHeaders()
                .AddColumn("Key")
                .AddColumn("Value");

            table.AddRow(
                new Text("Platform", Branding.DimGoldStyle),
                new Markup($"[white]{Markup.Escape(platform.OsName)} {platform.ArchRid}[/]")
            );
            table.AddRow(
                new Text(".NET SDK", Branding.DimGoldStyle),
                sdkVersion is not null
                    ? new Markup($"[white]{Markup.Escape(sdkVersion)}[/]")
                    : new Markup("[red]Not found[/]")
            );

            if (platform.DistroName is not null)
            {
                table.AddRow(
                    new Text("Distribution", Branding.DimGoldStyle),
                    new Markup($"[white]{Markup.Escape(platform.DistroName)}[/]")
                );
            }

            if (platform.KernelVersion is not null)
            {
                table.AddRow(
                    new Text("Kernel", Branding.DimGoldStyle),
                    new Markup($"[white]{Markup.Escape(platform.KernelVersion)}[/]")
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var choices = new[]
            {
                ":hammer: Publish Server",
                ":stethoscope: Check Prerequisites",
                "",
                ":cross_mark: Exit"
            };

            string selection;
            try
            {
                var prompt = new SelectionPrompt<string>()
                    .Title($"[{GoldMarkup}]What would you like to do?[/]")
                    .PageSize(10)
                    .HighlightStyle(Branding.HighlightStyle)
                    .AddChoices(choices.Where(c => !string.IsNullOrEmpty(c)));

                selection = prompt.Show(AnsiConsole.Console);
            }
            catch (OperationCanceledException)
            {
                InteractiveCancellation.Instance.Reset();

                if (CancellationTracker.Instance.IsDoublePress())
                {
                    AnsiConsole.MarkupLine("\n[grey]Exiting...[/]");
                    return 0;
                }

                AnsiConsole.Write(new Text("\nPress Ctrl+C again to exit\n", Branding.DimGoldStyle));
                Thread.Sleep(100);
                continue;
            }

            if (selection.Contains("Publish"))
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold {GoldMarkup}]:hammer: PUBLISH SERVER[/]\n");
                var exitCode = RunPublish(platform, repoRoot);
                if (exitCode != 0)
                {
                    return exitCode;
                }
                WaitForKey();
            }
            else if (selection.Contains("Prerequisites"))
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold {GoldMarkup}]:stethoscope: PREREQUISITE CHECK[/]\n");
                PrerequisiteChecker.CheckAll(platform, repoRoot, interactive: true);
                WaitForKey();
            }
            else if (selection.Contains("Exit"))
            {
                return 0;
            }
        }
    }

    private static int RunPublish(PlatformInfo platform, string repoRoot)
    {
        // 1. Prerequisite checks
        var prereqsPassed = PrerequisiteChecker.CheckAll(platform, repoRoot, interactive: true);
        if (!prereqsPassed)
        {
            return 1;
        }

        // 2. Select configuration
        string config;
        try
        {
            config = new SelectionPrompt<string>()
                .Title($"[{GoldMarkup}]Configuration:[/]")
                .HighlightStyle(Branding.HighlightStyle)
                .AddChoices("Release (Recommended)", "Debug")
                .Show(AnsiConsole.Console);

            config = config.Contains("Release") ? "Release" : "Debug";
        }
        catch (OperationCanceledException)
        {
            InteractiveCancellation.Instance.Reset();
            return 0; // Back to main menu
        }

        // 3. Confirm platform
        var rid = platform.Rid;
        string platformChoice;
        try
        {
            platformChoice = new SelectionPrompt<string>()
                .Title($"[{GoldMarkup}]Target platform:[/] [{GoldLightMarkup}]{rid}[/] [grey](auto-detected)[/]")
                .HighlightStyle(Branding.HighlightStyle)
                .AddChoices(
                    $":check_mark: Use {rid}",
                    ":wrench: Choose different platform"
                )
                .Show(AnsiConsole.Console);
        }
        catch (OperationCanceledException)
        {
            InteractiveCancellation.Instance.Reset();
            return 0;
        }

        if (platformChoice.Contains("Choose"))
        {
            try
            {
                var os = new SelectionPrompt<string>()
                    .Title($"[{GoldMarkup}]Operating System:[/]")
                    .HighlightStyle(Branding.HighlightStyle)
                    .AddChoices("win   (Windows)", "osx   (macOS)", "linux (Linux)")
                    .Show(AnsiConsole.Console);

                os = os.Split(' ')[0].Trim();

                var arch = new SelectionPrompt<string>()
                    .Title($"[{GoldMarkup}]Architecture:[/]")
                    .HighlightStyle(Branding.HighlightStyle)
                    .AddChoices("x64   (Intel/AMD 64-bit)", "arm64 (ARM 64-bit)")
                    .Show(AnsiConsole.Console);

                arch = arch.Split(' ')[0].Trim();

                rid = $"{os}-{arch}";
            }
            catch (OperationCanceledException)
            {
                InteractiveCancellation.Instance.Reset();
                return 0;
            }
        }

        // 4. Run publish
        AnsiConsole.WriteLine();
        var exitCode = PublishOrchestrator.Run(config, rid, interactive: true);

        if (exitCode == 0)
        {
            CheckFirstTimeSetup(repoRoot);
        }

        return exitCode;
    }

    private static void CheckFirstTimeSetup(string repoRoot)
    {
        var configPath = Path.Combine(repoRoot, "Distribution", "Configuration", "modernuo.json");
        if (!File.Exists(configPath))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Text(
                ":light_bulb: Tip: The server will prompt you for game data file locations on first run.\n",
                Branding.DimGoldStyle));
        }
    }

    private static string? GetDotNetSdkVersion()
    {
        var result = ProcessRunner.RunCaptured("dotnet", "--version");
        return result.Success ? result.StandardOutput.Trim() : null;
    }

    private static void WaitForKey()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
