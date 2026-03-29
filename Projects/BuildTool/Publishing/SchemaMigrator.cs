using Spectre.Console;

namespace BuildTool.Publishing;

public static class SchemaMigrator
{
    public static int Run(bool interactive)
    {
        if (interactive)
        {
            return RunInteractive();
        }

        return RunNonInteractive();
    }

    private static int RunInteractive()
    {
        var panel = new Panel("[bold]Running schema migration[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Branding.Gold)
            .Padding(1, 0);
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Tool restore
        {
            var exitCode = 0;
            string errorOutput = "";

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Branding.GoldStyle)
                .Start("[grey](1/2)[/] Restoring tools...", _ =>
                {
                    var result = ProcessRunner.RunCaptured("dotnet", "tool restore");
                    exitCode = result.ExitCode;
                    errorOutput = result.StandardError;
                });

            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine("  [green]:check_mark:[/] [grey](1/2)[/] Tools restored");
            }
            else
            {
                AnsiConsole.MarkupLine("  [red]:cross_mark:[/] [grey](1/2)[/] Tool restore [red](failed)[/]");
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    AnsiConsole.Write(new Panel(Markup.Escape(errorOutput.Trim()))
                        .Header("[red]Error[/]")
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Red));
                }

                return exitCode;
            }
        }

        // Schema generation
        {
            var exitCode = 0;
            string errorOutput = "";

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Branding.GoldStyle)
                .Start("[grey](2/2)[/] Generating serialization schema...", _ =>
                {
                    var result = ProcessRunner.RunCaptured(
                        "dotnet",
                        "tool run ModernUOSchemaGenerator -- ModernUO.sln"
                    );
                    exitCode = result.ExitCode;
                    errorOutput = result.StandardError;
                });

            if (exitCode == 0)
            {
                AnsiConsole.MarkupLine("  [green]:check_mark:[/] [grey](2/2)[/] Schema generated");
            }
            else
            {
                AnsiConsole.MarkupLine("  [red]:cross_mark:[/] [grey](2/2)[/] Schema generation [red](failed)[/]");
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    AnsiConsole.Write(new Panel(Markup.Escape(errorOutput.Trim()))
                        .Header("[red]Error[/]")
                        .Border(BoxBorder.Rounded)
                        .BorderColor(Color.Red));
                }

                return exitCode;
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel("[green bold]:check_mark_button: Schema migration complete.[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0));
        AnsiConsole.WriteLine();
        return 0;
    }

    private static int RunNonInteractive()
    {
        Console.WriteLine("dotnet tool restore");
        var exitCode = ProcessRunner.RunPassthrough("dotnet", "tool restore");
        if (exitCode != 0)
        {
            Console.Error.WriteLine($"Error: tool restore failed with exit code {exitCode}");
            return exitCode;
        }

        Console.WriteLine("Generating serialization migration schema...");
        exitCode = ProcessRunner.RunPassthrough("dotnet", "tool run ModernUOSchemaGenerator -- ModernUO.sln");
        if (exitCode != 0)
        {
            Console.Error.WriteLine($"Error: schema generation failed with exit code {exitCode}");
            return exitCode;
        }

        return 0;
    }
}
