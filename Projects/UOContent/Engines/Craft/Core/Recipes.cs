using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Craft;

public class Recipe
{
    private TextDefinition _td;

    public Recipe(int id, CraftSystem system, CraftItem item)
    {
        ID = id;
        CraftSystem = system;
        CraftItem = item;

        if (!Recipes.TryAdd(id, this))
        {
            throw new Exception("Attempting to create recipe with preexisting ID.");
        }

        LargestRecipeID = Math.Max(id, LargestRecipeID);
    }

    public static Dictionary<int, Recipe> Recipes { get; } = new();

    public static int LargestRecipeID { get; private set; }

    public CraftSystem CraftSystem { get; set; }

    public CraftItem CraftItem { get; set; }

    public int ID { get; }

    public TextDefinition TextDefinition => _td ??= new TextDefinition(CraftItem.NameNumber, CraftItem.NameString);

    public static void Initialize()
    {
        CommandSystem.Register("LearnAllRecipes", AccessLevel.GameMaster, LearnAllRecipes_OnCommand);
        CommandSystem.Register("ForgetAllRecipes", AccessLevel.GameMaster, ForgetAllRecipes_OnCommand);
    }

    [Usage("LearnAllRecipes"), Description("Teaches a player all available recipes.")]
    private static void LearnAllRecipes_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        m.SendMessage("Target a player to teach them all of the recipes.");

        m.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is PlayerMobile mobile)
                {
                    foreach (var kvp in Recipes)
                    {
                        mobile.AcquireRecipe(kvp.Key);
                    }

                    from.SendMessage("You teach them all of the recipes.");
                }
                else
                {
                    from.SendMessage("That is not a player!");
                }
            }
        );
    }

    [Usage("ForgetAllRecipes"), Description("Makes a player forget all the recipes they've learned.")]
    private static void ForgetAllRecipes_OnCommand(CommandEventArgs e)
    {
        var m = e.Mobile;
        m.SendMessage("Target a player to have them forget all of the recipes they've learned.");

        m.BeginTarget(
            -1,
            false,
            TargetFlags.None,
            (from, targeted) =>
            {
                if (targeted is PlayerMobile mobile)
                {
                    mobile.ResetRecipes();

                    from.SendMessage("They forget all their recipes.");
                }
                else
                {
                    from.SendMessage("That is not a player!");
                }
            }
        );
    }
}
