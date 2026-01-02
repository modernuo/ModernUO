using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Server.Commands.Generic;
using Server.Json;

namespace Server.Commands;

public static class GenCommands
{
    public static void Configure()
    {
        CommandSystem.Register("GenCommands", AccessLevel.Developer, GenCommandsWebpage_OnCommand);
    }

    [Usage("GenCommands")]
    [Aliases("GenCmdWeb")]
    [Description("Generates a webpage to the web folder with a list of all commands.")]
    private static void GenCommandsWebpage_OnCommand(CommandEventArgs e)
    {
        var allCommands = HelpInfo.SortedHelpInfo;
        var allModifiers = BaseCommandImplementor.Implementors;

        Dictionary<string, BaseCommandImplementor> modifiersByAccessor = [];

        foreach (var modifier in allModifiers)
        {
            modifiersByAccessor[modifier.Accessors[0]] = modifier;
        }

        Dictionary<AccessLevel, List<CommandInfo>> commandsByAccessLevel = [];

        foreach (var command in allCommands)
        {
            if (!commandsByAccessLevel.TryGetValue(command.AccessLevel, out var list))
            {
                commandsByAccessLevel[command.AccessLevel] = list = [];
            }

            list.Add(command);
        }

        // Serialization options with camel case naming policy
        var options = JsonConfig.GetOptions();
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Write modifiers as JSON. All keys must be lowercase.
        var modifiersJson = JsonSerializer.Serialize(
            modifiersByAccessor,
            options
        );

        // Write commands as JSON. All keys must be lowercase.
        var commandsJson = JsonSerializer.Serialize(
            commandsByAccessLevel,
            options
        );

        // Template is in Distribution/Data/commands
        var templatePath = Path.Combine(Core.BaseDirectory, "Data", "commands", "template.html");

        var template = File.ReadAllText(templatePath)
            .Replace("<!-- COMMANDS -->", commandsJson.EscapeHtml())
            .Replace("<!-- MODIFIERS -->", modifiersJson.EscapeHtml());

        // Output to Distribution/web folder (typically ignored by servers for external systems)
        var webFolder = Path.Combine(Core.BaseDirectory, "web");
        Directory.CreateDirectory(webFolder);
        var outputPath = Path.Combine(webFolder, "commands.html");
        File.WriteAllText(outputPath, template, Encoding.UTF8);
        e.Mobile.SendMessage($"Commands webpage generated at {outputPath}.");
    }
}
