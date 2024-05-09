using System;
using ModernUO.Serialization;

namespace Server;

[SerializationGenerator(0)]
public sealed partial class BloodRose : PowerFactionItem
{
    public BloodRose() : base(Utility.RandomBool() ? 6378 : 9035) => Hue = 2118;

    public override string DefaultName => "blood rose";

    public override bool Use(Mobile from)
    {
        if (from.GetStatMod("blood-rose") == null)
        {
            from.PlaySound(Utility.Random(0x3A, 3));

            if (from.Body.IsHuman && !from.Mounted)
            {
                from.Animate(34, 5, 1, true, false, 0);
            }

            var amount = Utility.Dice(3, 3, 3);
            var time = Utility.RandomMinMax(5, 30);

            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

            from.PlaySound(0x1EE);
            from.AddStatMod(new StatMod(StatType.All, "blood-rose", amount, TimeSpan.FromMinutes(time)));

            return true;
        }

        // You have eaten one of these recently and eating another would provide no benefit.
        from.SendLocalizedMessage(1062927);

        return false;
    }
}
