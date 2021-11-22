using System;
using Server.Items;
using Server.Utilities;

namespace Server.Spells.First
{
    public class CreateFoodSpell : MagerySpell
    {
        private static readonly SpellInfo _info = new(
            "Create Food",
            "In Mani Ylem",
            224,
            9011,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.MandrakeRoot
        );

        private static readonly FoodInfo[] m_Food =
        {
            new(typeof(Grapes), "a grape bunch"),
            new(typeof(Ham), "a ham"),
            new(typeof(CheeseWedge), "a wedge of cheese"),
            new(typeof(Muffins), "muffins"),
            new(typeof(FishSteak), "a fish steak"),
            new(typeof(Ribs), "cut of ribs"),
            new(typeof(CookedBird), "a cooked bird"),
            new(typeof(Sausage), "sausage"),
            new(typeof(Apple), "an apple"),
            new(typeof(Peach), "a peach")
        };

        public CreateFoodSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                var foodInfo = m_Food.RandomElement();
                var food = foodInfo.Create();

                if (food != null)
                {
                    Caster.AddToBackpack(food);

                    // You magically create food in your backpack:
                    Caster.SendLocalizedMessage(1042695, true, $" {foodInfo.Name}");

                    Caster.FixedParticles(0, 10, 5, 2003, EffectLayer.RightHand);
                    Caster.PlaySound(0x1E2);
                }
            }

            FinishSequence();
        }
    }

    public class FoodInfo
    {
        public FoodInfo(Type type, string name)
        {
            Type = type;
            Name = name;
        }

        public Type Type { get; set; }

        public string Name { get; set; }

        public Item Create()
        {
            try
            {
                return Type.CreateInstance<Item>();
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}
