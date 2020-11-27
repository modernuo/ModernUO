using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag
{
    public enum Ingredient
    {
        SheepLiver,
        RabbitsFoot,
        MongbatWing,
        ChickenGizzard,
        RatTail,
        FrogsLeg,
        DeerHeart,
        LizardTongue,
        SlimeOoze,
        SpiritEssence,
        SwampWater,
        RedMushrooms,
        Bones,
        StarChart,
        Whiskey
    }

    public class IngredientInfo
    {
        private static readonly IngredientInfo[] m_Table =
        {
            // sheep liver
            new(1055020, 5, typeof(Sheep)),
            // rabbit's foot
            new(1055021, 5, typeof(Rabbit), typeof(JackRabbit)),
            // mongbat wing
            new(1055022, 5, typeof(Mongbat), typeof(GreaterMongbat)),
            // chicken gizzard
            new(1055023, 5, typeof(Chicken)),
            // rat tail
            new(1055024, 5, typeof(Rat), typeof(GiantRat), typeof(SewerRat)),
            // frog's leg
            new(1055025, 5, typeof(BullFrog)),
            // deer heart
            new(1055026, 5, typeof(Hind), typeof(GreatHart)),
            // lizard tongue
            new(1055027, 5, typeof(LavaLizard), typeof(Lizardman)),
            // slime ooze
            new(1055028, 5, typeof(Slime)),
            // spirit essence
            new(1055029, 5, typeof(Ghoul), typeof(Spectre), typeof(Shade), typeof(Wraith), typeof(Bogle)),
            // Swamp Water
            new(1055030, 1),
            // Freshly Cut Red Mushrooms
            new(1055031, 1),
            // Bones Buried In Hallowed Ground
            new(1055032, 1),
            // Star Chart
            new(1055033, 1),
            // Captain Blackheart's Whiskey
            new(1055034, 1)
        };

        private IngredientInfo(int name, int quantity, params Type[] creatures)
        {
            Name = name;
            Creatures = creatures;
            Quantity = quantity;
        }

        public int Name { get; }

        public Type[] Creatures { get; }

        public int Quantity { get; }

        public static IngredientInfo Get(Ingredient ingredient)
        {
            var index = (int)ingredient;

            if (index >= 0 && index < m_Table.Length)
            {
                return m_Table[index];
            }

            return m_Table[0];
        }

        public static Ingredient RandomIngredient(Ingredient[] oldIngredients)
        {
            var length = m_Table.Length - oldIngredients.Length;
            var ingredients = new Ingredient[length];

            for (int i = 0, n = 0; i < m_Table.Length && n < ingredients.Length; i++)
            {
                var currIngredient = (Ingredient)i;

                var found = false;
                for (var j = 0; !found && j < oldIngredients.Length; j++)
                {
                    if (oldIngredients[j] == currIngredient)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    ingredients[n++] = currIngredient;
                }
            }

            return ingredients.RandomElement();
        }
    }
}
