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

    /// <summary>
    /// Tracks menu depth. 0 = root menu, >0 = submenu.
    /// Used by ShowPrompt to automatically add Back option in submenus.
    /// </summary>
    private static int _menuDepth;

    public static int Run(PlatformInfo platform, string repoRoot)
    {
        return ShowMainMenu(platform, repoRoot);
    }

    /// <summary>
    /// Shows a selection prompt with automatic Back support.
    /// When _menuDepth > 0, appends a ":left_arrow: Back" choice.
    /// Returns null if the user chose Back or pressed Ctrl+C in a submenu.
    /// At the root menu, Ctrl+C requires double-press to exit (returns null on double-press).
    /// </summary>
    private static string? ShowPrompt(string title, params string[] choices)
    {
        var allChoices = choices.Where(c => !string.IsNullOrEmpty(c)).ToList();

        if (_menuDepth > 0)
        {
            allChoices.Add(":left_arrow: Back");
        }

        var prompt = new SelectionPrompt<string>()
            .Title($"[{GoldMarkup}]{title}[/]")
            .PageSize(10)
            .HighlightStyle(Branding.HighlightStyle)
            .AddChoices(allChoices);

        string selection;
        try
        {
            selection = prompt.Show(AnsiConsole.Console);
        }
        catch (OperationCanceledException)
        {
            InteractiveCancellation.Instance.Reset();

            if (_menuDepth > 0)
            {
                return null; // Back
            }

            // Root menu: require double-press to exit
            if (CancellationTracker.Instance.IsDoublePress())
            {
                AnsiConsole.MarkupLine("\n[grey]Exiting...[/]");
                return ":cross_mark: Exit";
            }

            AnsiConsole.Write(new Text("\nPress Ctrl+C again to exit\n", Branding.DimGoldStyle));
            Thread.Sleep(100);
            return ""; // Signal to redraw menu
        }

        if (selection.Contains("Back"))
        {
            return null;
        }

        return selection;
    }

    private static int ShowMainMenu(PlatformInfo platform, string repoRoot)
    {
        while (true)
        {
            AnsiConsole.Clear();

            // Write the true-color ANSI logo directly (bypasses Spectre markup parsing)
            Console.Write(Branding.UOLogoAnsi);
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

            var selection = ShowPrompt(
                "What would you like to do?",
                ":hammer: Publish Server",
                ":stethoscope: Check Prerequisites",
                "",
                ":cross_mark: Exit"
            );

            if (string.IsNullOrEmpty(selection))
            {
                continue; // Redraw (single Ctrl+C)
            }

            if (selection.Contains("Publish"))
            {
                _menuDepth++;
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold {GoldMarkup}]:hammer: PUBLISH SERVER[/]\n");
                var result = RunPublish(platform, repoRoot);
                _menuDepth--;

                if (result is not null) // null = Back, skip WaitForKey
                {
                    WaitForKey();
                }
            }
            else if (selection.Contains("Prerequisites"))
            {
                _menuDepth++;
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine($"[bold {GoldMarkup}]:stethoscope: PREREQUISITE CHECK[/]\n");
                PrerequisiteChecker.CheckAll(platform, repoRoot, interactive: true);
                _menuDepth--;
                WaitForKey();
            }
            else if (selection.Contains("Exit"))
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Returns null if user navigated Back to main menu, 0 on success, non-zero on error.
    /// Uses a step loop so Back goes to the previous step, not the main menu.
    /// </summary>
    private static int? RunPublish(PlatformInfo platform, string repoRoot)
    {
        // Step 0: Prerequisites (auto-runs, no back)
        var prereqsPassed = PrerequisiteChecker.CheckAll(platform, repoRoot, interactive: true);
        if (!prereqsPassed)
        {
            return 1;
        }

        var config = "Release";
        var rid = platform.Rid;
        var customPlatform = false;
        var os = platform.OsRid;
        var arch = platform.ArchRid;

        // Steps 1+: interactive prompts with back navigation
        var step = 1;
        while (step <= 4)
        {
            switch (step)
            {
                case 1: // Configuration
                {
                    var choice = ShowPrompt("Configuration:", "Release (Recommended)", "Debug");
                    if (choice is null)
                    {
                        return null; // Back from first prompt = back to main menu
                    }

                    config = choice.Contains("Release") ? "Release" : "Debug";
                    step++;
                    break;
                }
                case 2: // Platform confirmation
                {
                    var choice = ShowPrompt(
                        $"Target platform: [{GoldLightMarkup}]{platform.Rid}[/] [grey](auto-detected)[/]",
                        $":check_mark: Use {platform.Rid}",
                        ":wrench: Choose different platform"
                    );

                    if (choice is null)
                    {
                        step--;
                        break;
                    }

                    customPlatform = choice.Contains("Choose");
                    if (!customPlatform)
                    {
                        rid = platform.Rid;
                        step = 5; // Skip OS/arch selection, go to publish
                    }
                    else
                    {
                        step++;
                    }
                    break;
                }
                case 3: // OS selection (only if custom platform)
                {
                    var choice = ShowPrompt(
                        "Operating System:",
                        "win   (Windows)",
                        "osx   (macOS)",
                        "linux (Linux)"
                    );

                    if (choice is null)
                    {
                        step--;
                        break;
                    }

                    os = choice.Split(' ')[0].Trim();
                    step++;
                    break;
                }
                case 4: // Architecture selection (only if custom platform)
                {
                    var choice = ShowPrompt(
                        "Architecture:",
                        "x64   (Intel/AMD 64-bit)",
                        "arm64 (ARM 64-bit)"
                    );

                    if (choice is null)
                    {
                        step--;
                        break;
                    }

                    arch = choice.Split(' ')[0].Trim();
                    rid = $"{os}-{arch}";
                    step++;
                    break;
                }
            }
        }

        // Run publish
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
