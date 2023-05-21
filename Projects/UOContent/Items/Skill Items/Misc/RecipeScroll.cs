using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RecipeScroll : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _recipeID;

    public RecipeScroll(Recipe r) : this(r.ID)
    {
    }

    [Constructible]
    public RecipeScroll(int recipeID) : base(0x2831) => _recipeID = recipeID;

    public override int LabelNumber => 1074560; // recipe scroll

    public Recipe Recipe
    {
        get
        {
            Recipe.Recipes.TryGetValue(_recipeID, out var recipe);
            return recipe;
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var r = Recipe;

        if (r != null)
        {
            if (r.TextDefinition.Number > 0)
            {
                list.Add(1049644, r.TextDefinition.Number); // [~1_stuff~]
            }
            else
            {
                list.Add(1049644, r.TextDefinition.String); // [~1_stuff~]
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        var r = Recipe;

        if (r != null && from is PlayerMobile pm)
        {
            if (!pm.HasRecipe(r))
            {
                var chance = r.CraftItem.GetSuccessChance(pm, null, r.CraftSystem, false, out var allRequiredSkills);

                if (allRequiredSkills && chance >= 0.0)
                {
                    // You have learned a new recipe: ~1_RECIPE~
                    pm.SendLocalizedMessage(1073451, r.TextDefinition.ToString());
                    pm.AcquireRecipe(r);
                    Delete();
                }
                else
                {
                    pm.SendLocalizedMessage(1044153); // You don't have the required skills to attempt this item.
                }
            }
            else
            {
                pm.SendLocalizedMessage(1073427); // You already know this recipe.
            }
        }
    }
}
