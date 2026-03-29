using BuildTool.Platform;
using Spectre.Console;

namespace BuildTool.Publishing;

public static class PublishOrchestrator
{
    // Target the Application project (not the solution) to avoid building BuildTool and test projects
    private const string AppProject = "Projects/Application/Application.csproj";

    private static readonly (string Description, string Command, string Arguments)[] BuildSteps =
    [
        ("Restoring tools", "dotnet", "tool restore"),
        ("Cleaning project", "dotnet", $"clean {AppProject} --verbosity quiet"),
        ("Restoring packages", "dotnet", $"restore {AppProject} --force-evaluate --source https://api.nuget.org/v3/index.json"),
    ];

    public static int Run(string config, PlatformInfo platform, bool interactive)
    {
        return Run(config, platform.Rid, interactive);
    }

    public static int Run(string config, string rid, bool interactive, bool isCrossCompile = false)
    {
        if (interactive)
        {
            return RunInteractive(config, rid, isCrossCompile);
        }

        return RunNonInteractive(config, rid);
    }

    private static int RunInteractive(string config, string rid, bool isCrossCompile)
    {
        var panel = new Panel($"[bold]Publishing[/] [rgb(223,198,136)]{config}[/] for [rgb(223,198,136)]{rid}[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Branding.Gold)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        var allSteps = new List<(string Description, string Command, string Arguments)>(BuildSteps)
        {
            ($"Publishing ({config}, {rid})", "dotnet", $"publish {AppProject} -c {config} -r {rid} --no-restore --self-contained=false"),
            ("Generating serialization schema", "dotnet", "tool run ModernUOSchemaGenerator -- ModernUO.sln")
        };

        var completed = 0;
        var total = allSteps.Count;

        foreach (var (description, command, arguments) in allSteps)
        {
            completed++;
            var exitCode = RunStepInteractive(description, command, arguments, completed, total);
            if (exitCode != 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel("[red bold]Build failed.[/] See error output above for details.")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Red)
                    .Padding(1, 0));
                return exitCode;
            }
        }

        AnsiConsole.WriteLine();
        DisplaySuccess(rid, isCrossCompile);
        return 0;
    }

    private static int RunStepInteractive(string description, string command, string arguments, int step, int total)
    {
        var exitCode = 0;
        string errorOutput = "";

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Branding.GoldStyle)
            .Start($"[grey]({step}/{total})[/] {description}...", _ =>
            {
                var result = ProcessRunner.RunCaptured(command, arguments);
                exitCode = result.ExitCode;
                errorOutput = result.StandardError;
            });

        if (exitCode == 0)
        {
            AnsiConsole.MarkupLine($"  [green]:check_mark:[/] [grey]({step}/{total})[/] {description}");
        }
        else
        {
            AnsiConsole.MarkupLine($"  [red]:cross_mark:[/] [grey]({step}/{total})[/] {description} [red](failed)[/]");

            if (!string.IsNullOrWhiteSpace(errorOutput))
            {
                AnsiConsole.WriteLine();
                AnsiConsole.Write(new Panel(Markup.Escape(errorOutput.Trim()))
                    .Header("[red]Error Output[/]")
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Red)
                    .Expand());
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [grey]Command: {command} {arguments}[/]");
        }

        return exitCode;
    }

    private static int RunNonInteractive(string config, string rid)
    {
        // Run common build steps
        foreach (var (description, command, arguments) in BuildSteps)
        {
            Console.WriteLine($"{command} {arguments}");
            var exitCode = ProcessRunner.RunPassthrough(command, arguments);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Error: '{description}' failed with exit code {exitCode}");
                return exitCode;
            }
        }

        // Publish step
        {
            var publishArgs = $"publish {AppProject} -c {config} -r {rid} --no-restore --self-contained=false";
            Console.WriteLine($"dotnet {publishArgs}");
            var exitCode = ProcessRunner.RunPassthrough("dotnet", publishArgs);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Error: 'publish' failed with exit code {exitCode}");
                return exitCode;
            }
        }

        // Schema generation
        {
            Console.WriteLine("Generating serialization migration schema...");
            const string schemaArgs = "tool run ModernUOSchemaGenerator -- ModernUO.sln";
            var exitCode = ProcessRunner.RunPassthrough("dotnet", schemaArgs);
            if (exitCode != 0)
            {
                Console.Error.WriteLine($"Error: schema generation failed with exit code {exitCode}");
                return exitCode;
            }
        }

        return 0;
    }

    internal static void DisplaySuccess(string rid, bool isCrossCompile = false)
    {
        var isWindows = rid.StartsWith("win", StringComparison.OrdinalIgnoreCase);
        var runCommand = isWindows ? "ModernUO.exe" : "dotnet ModernUO.dll";

        var rows = new List<Spectre.Console.Rendering.IRenderable>
        {
            new Markup("[green bold]:check_mark_button: Build complete![/]"),
            new Markup("")
        };

        if (isCrossCompile)
        {
            rows.Add(new Markup($"Copy the [rgb(213,191,116)]Distribution[/] folder to your [rgb(213,191,116)]{rid}[/] server, then run:"));
        }
        else
        {
            rows.Add(new Markup("Run the server from the [rgb(213,191,116)]Distribution[/] directory:"));
        }

        rows.Add(new Markup(""));
        rows.Add(new Markup("  [white on grey23] cd Distribution [/]"));
        rows.Add(new Markup($"  [white on grey23] {runCommand} [/]"));

        AnsiConsole.Write(new Panel(new Rows(rows))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0));
        AnsiConsole.WriteLine();
    }
}
