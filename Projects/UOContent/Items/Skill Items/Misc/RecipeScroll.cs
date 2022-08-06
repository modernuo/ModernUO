using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class RecipeScroll : Item
    {
        private int m_RecipeID;

        public RecipeScroll(Recipe r) : this(r.ID)
        {
        }

        [Constructible]
        public RecipeScroll(int recipeID) : base(0x2831) => m_RecipeID = recipeID;

        public RecipeScroll(Serial serial)
            : base(serial)
        {
        }

        public override int LabelNumber => 1074560; // recipe scroll

        [CommandProperty(AccessLevel.GameMaster)]
        public int RecipeID
        {
            get => m_RecipeID;
            set
            {
                m_RecipeID = value;
                InvalidateProperties();
            }
        }

        public Recipe Recipe
        {
            get
            {
                Recipe.Recipes.TryGetValue(m_RecipeID, out var recipe);
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_RecipeID);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_RecipeID = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}
