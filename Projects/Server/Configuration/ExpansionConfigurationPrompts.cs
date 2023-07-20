using Server.Maps;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Console.WriteLine(" - {0,2}: {1} ({2})", i, ((Expansion)info.ID).ToString(), info.Name);
        }

        var maxExpansion = (Expansion)expansions[^1].ID;
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

    internal static void OutputSelectedMaps(Expansion expansion, MapSelection selectedMaps)
    {
        var mapOptionsForExpansion = ExpansionMapSelectionFlags.FromExpansion(expansion);

        Console.WriteLine("Selected maps:");
        var mapOptionsForExpansionsCount = mapOptionsForExpansion.Count;
        for (int i=0; i< mapOptionsForExpansionsCount; i++)
        {
            Console.WriteLine("{0}. {1} [{2}]",
                i + 1,     // Because when toggling, we want to run from 1
                mapOptionsForExpansion[i].ToString(),
                selectedMaps.Includes(mapOptionsForExpansion[i]) ? "*" : "");
        }

        Console.WriteLine("Only these maps will be populated and moongates will only lead to them: ");
        Utility.PushColor(ConsoleColor.Green);
        Console.Write(selectedMaps.ToCommaDelimitedString());
        Utility.PopColor();
        Console.WriteLine();
        Console.WriteLine("[1-{0} and enter to toggle, or enter to finish]", mapOptionsForExpansionsCount);
    }

    internal static void ToggleSelectedMaps(List<MapSelectionFlags> expansionMaps, MapSelection mapSelection, int selectedNumber)
    {
        var index = selectedNumber - 1;

        if (mapSelection.Includes(expansionMaps[index]))
        {
            mapSelection.Disable(expansionMaps[index]);
        }
        else
        {
            mapSelection.Enable(expansionMaps[index]);
        }
    }

    internal static MapSelection GetSelectedMaps(Expansion expansion)
    {
        var expansionMaps = ExpansionMapSelectionFlags.FromExpansion(expansion);
        var selectedMaps = new MapSelection();
        selectedMaps.EnableAllInExpansion(expansion);
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

            if (selectedNumber <= 0 || selectedNumber > expansionMaps.Count)
            {
                Console.WriteLine("That number was not an option. Please try again...");
                continue;
            }

            ToggleSelectedMaps(expansionMaps, selectedMaps, selectedNumber);
        } while (lastInput != "");

        return selectedMaps;
    }
}
