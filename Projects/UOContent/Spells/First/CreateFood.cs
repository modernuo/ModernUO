using Server.Items;

namespace Server.Spells.First;

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

    private static Item CreateRandomFood() =>
        Utility.Random(10) switch
        {
            0 => new Grapes(),
            1 => new Ham(),
            2 => new CheeseWedge(),
            3 => new Muffins(),
            4 => new FishSteak(),
            5 => new Ribs(),
            6 => new CookedBird(),
            7 => new Sausage(),
            8 => new Apple(),
            9 => new Peach(),
            _ => null
        };

    public CreateFoodSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var food = CreateRandomFood();

            if (food != null)
            {
                Caster.AddToBackpack(food);

                if (Caster.NetState != null)
                {
                    // Not translated to the casters language because affixes are ascii only
                    var foodName = Localization.GetText(food.LabelNumber);

                    // You magically create food in your backpack:
                    Caster.SendLocalizedMessage(1042695, true, $" {foodName}");
                }

                Caster.FixedParticles(0, 10, 5, 2003, EffectLayer.RightHand);
                Caster.PlaySound(0x1E2);
            }
        }

        FinishSequence();
    }
}
