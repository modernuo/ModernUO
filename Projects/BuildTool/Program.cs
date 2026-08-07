using System.Text;
using BuildTool;
using BuildTool.Interactive;
using BuildTool.Platform;
using BuildTool.Prerequisites;
using BuildTool.Publishing;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;

var repoRoot = FindRepoRoot();
if (repoRoot is null)
{
    Console.Error.WriteLine("Error: Could not find ModernUO.slnx. Run this tool from the repository root.");
    return 1;
}

var options = ParseArguments(args);

// Interactive mode: no args provided
if (options.Interactive)
{
    // Handle Ctrl+C by signaling our cancellation token (not killing the process)
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        InteractiveCancellation.Instance.Cancel();
    };

    var platform = PlatformDetector.Detect();
    return GuidedMode.Run(platform, repoRoot);
}

// Non-interactive mode
var detectedPlatform = PlatformDetector.Detect();
options.Os ??= detectedPlatform.OsRid;
options.Arch ??= detectedPlatform.ArchRid;
var rid = $"{options.Os}-{options.Arch}";

if (options.CheckPrereqsOnly)
{
    // Same renderer the guided menu uses, so the two cannot drift. Spectre drops ANSI styling by
    // itself when stdout is not a terminal, which is the case this flag exists for, but it also
    // falls back to an 80 column width and folds anything longer. The install hints we print are
    // shell commands — the CentOS one is 95 characters — and a fold puts a newline in the middle of
    // a command that someone is meant to copy. Widen the profile so they stay on one line.
    if (Console.IsOutputRedirected)
    {
        AnsiConsole.Profile.Width = 200;
    }

    // Exit code is the machine-readable half: 0 when everything resolves, 1 when anything is missing.
    return PrerequisiteChecker.CheckNativeLibraries(detectedPlatform, interactive: false) ? 0 : 1;
}

// Run prerequisite checks unless skipped
if (!options.SkipPrereqs)
{
    var sdkResult = DotNetSdkManager.CheckSdk(repoRoot);
    if (!sdkResult.Passed)
    {
        Console.Error.WriteLine($"Error: {sdkResult.Details}");
        if (sdkResult.DownloadUrl is not null)
        {
            Console.Error.WriteLine($"Download: {sdkResult.DownloadUrl}");
        }
        return 1;
    }
}

return options.Action switch
{
    BuildAction.Publish => PublishOrchestrator.Run(options.Config, rid, interactive: false),
    BuildAction.Migrate => SchemaMigrator.Run(interactive: false),
    _ => 1
};

static BuildOptions ParseArguments(string[] args)
{
    var options = new BuildOptions();

    if (args.Length == 0)
    {
        options.Interactive = true;
        return options;
    }

    // Check for named arguments first
    var hasNamedArgs = false;
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--config" when i + 1 < args.Length:
                {
                    options.Config = NormalizeConfig(args[++i]);
                    hasNamedArgs = true;
                    break;
                }
            case "--os" when i + 1 < args.Length:
                {
                    options.Os = args[++i].ToLowerInvariant();
                    hasNamedArgs = true;
                    break;
                }
            case "--arch" when i + 1 < args.Length:
                {
                    options.Arch = args[++i].ToLowerInvariant();
                    hasNamedArgs = true;
                    break;
                }
            case "--action" when i + 1 < args.Length:
                {
                    options.Action = args[++i].ToLowerInvariant() switch
                    {
                        "migrate" => BuildAction.Migrate,
                        _         => BuildAction.Publish
                    };
                    hasNamedArgs = true;
                    break;
                }
            case "--skip-prereqs":
                {
                    options.SkipPrereqs = true;
                    hasNamedArgs = true;
                    break;
                }
            case "--check-prereqs":
                {
                    options.CheckPrereqsOnly = true;
                    hasNamedArgs = true;
                    break;
                }
            case "--interactive":
                {
                    options.Interactive = true;
                    hasNamedArgs = true;
                    break;
                }
        }
    }

    if (hasNamedArgs)
    {
        return options;
    }

    // Positional argument parsing (backward compat: [config] [os] [arch])
    if (args.Length >= 1)
    {
        options.Config = NormalizeConfig(args[0]);
    }

    if (args.Length >= 2)
    {
        options.Os = args[1].ToLowerInvariant();
    }

    if (args.Length >= 3)
    {
        options.Arch = args[2].ToLowerInvariant();
    }

    return options;
}

static string NormalizeConfig(string config) =>
    config.ToLowerInvariant() switch
    {
        "release" => "Release",
        "debug" => "Debug",
        _ => char.ToUpperInvariant(config[0]) + config[1..].ToLowerInvariant()
    };

static string? FindRepoRoot()
{
    // Check current directory first
    var current = Directory.GetCurrentDirectory();
    if (File.Exists(Path.Combine(current, "ModernUO.slnx")))
    {
        return current;
    }

    // Walk up to find the solution file
    var dir = new DirectoryInfo(current);
    while (dir?.Parent is not null)
    {
        dir = dir.Parent;
        if (File.Exists(Path.Combine(dir.FullName, "ModernUO.slnx")))
        {
            return dir.FullName;
        }
    }

    return null;
}
