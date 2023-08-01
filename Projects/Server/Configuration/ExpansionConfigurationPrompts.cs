using Server.Maps;
using System;
using System.Globalization;

namespace Server;

public static class ExpansionConfigurationPrompts
{
    internal static Expansion GetExpansion()
    {
        Console.WriteLine("Please choose an expansion by typing the number or short name:");
        var expansions = ExpansionInfo.Table;

        for (var i = 0; i < expansions.Length; i++)
        {
            var info = expansions[i];
            Console.WriteLine(" - {0,2}: {1} ({2})", i, ((Expansion)info.Id).ToString(), info.Name);
        }

        var maxExpansion = (Expansion)expansions[^1].Id;
        var maxExpansionName = maxExpansion.ToString();

        do
        {
            Console.Write("[enter for {0}]> ", maxExpansionName);
            var input = Console.ReadLine();
            Expansion expansion;

            if (string.IsNullOrWhiteSpace(input))
            {
                expansion = maxExpansion;
            }
            else if (int.TryParse(input, NumberStyles.Integer, null, out var number) &&
                     number >= 0 && number < expansions.Length)
            {
                expansion = (Expansion)number;
            }
            else if (!Enum.TryParse(input, out expansion))
            {
                Utility.PushColor(ConsoleColor.Red);
                Console.Write(input);
                Utility.PopColor();
                Console.WriteLine(" is an invalid expansion option.");
                continue;
            }

            Console.Write("Expansion set to ");
            Utility.PushColor(ConsoleColor.Green);
            Console.Write(ExpansionInfo.GetInfo(expansion).Name);
            Utility.PopColor();
            Console.WriteLine(".");
            return expansion;
        } while (true);
    }

    internal static void OutputSelectedMaps(Expansion expansion, MapSelectionFlags selectedMaps)
    {
        Console.WriteLine("Selected maps:");

        var i = 0;
        foreach (var flag in MapSelection.EnumFromExpansion(expansion))
        {
            Console.WriteLine($"{i + 1}. {flag} [{(selectedMaps.Includes(flag) ? "*" : "")}]");
            i++;
        }

        Console.WriteLine();
        Console.WriteLine($"[1-{i} and enter to toggle, or enter to finish]");
    }

    internal static MapSelectionFlags GetSelectedMaps(Expansion expansion)
    {
        var expansionMaps = ExpansionInfo.GetInfo(expansion).MapSelectionFlags;
        var selectedMaps = expansionMaps;

        string lastInput;
        do
        {
            OutputSelectedMaps(expansion, selectedMaps);
            lastInput = Console.ReadLine()?.TrimEnd();

            if (string.IsNullOrWhiteSpace(lastInput))
            {
                break;
            }

            if (!int.TryParse(lastInput, out var selectedNumber))
            {
                Console.WriteLine("You need to choose a number, or press ENTER on its own to accept");
                continue;
            }

            if (selectedNumber is < 1 or > 31)
            {
                Console.WriteLine("That number was not an option. Please try again...");
                continue;
            }

            var selectedFlag = (MapSelectionFlags)(1 << (selectedNumber - 1));

            if (!expansionMaps.Includes(selectedFlag))
            {
                Console.WriteLine("That number was not an option. Please try again...");
                continue;
            }

            selectedMaps.Toggle(selectedFlag);
        } while (lastInput != "");

        Console.WriteLine("These maps will be populated and moongates will not lead to other maps: ");
        Utility.PushColor(ConsoleColor.Green);
        Console.WriteLine(selectedMaps.ToCommaDelimitedString());
        Utility.PopColor();
        Console.WriteLine($"To change the selected maps, modify {ExpansionInfo.ExpansionConfigurationPath}.");

        return selectedMaps;
    }
}
