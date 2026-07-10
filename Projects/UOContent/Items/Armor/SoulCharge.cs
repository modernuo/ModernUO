using System;

namespace Server.Items;

public static class SoulCharge
{
    internal const int ChanceCap = 50;
    internal const int ConversionPercent = 30;
    internal const int EffectCliloc = 1113636;

    public static void OnDamageTaken(Mobile defender, int damage)
    {
        if (!Core.SA || damage <= 0 || defender?.Deleted != false || !defender.Alive)
        {
            return;
        }

        var shield = defender.FindItemOnLayer<BaseShield>(Layer.TwoHanded);
        var chance = Math.Min(shield?.ArmorAttributes.SoulCharge ?? 0, ChanceCap);

        if (chance <= 0)
        {
            return;
        }

        if (chance <= Utility.Random(100))
        {
            return;
        }

        var manaGain = Math.Min(defender.ManaMax - defender.Mana, damage * ConversionPercent / 100);

        if (manaGain <= 0)
        {
            return;
        }

        defender.Mana += manaGain;
        defender.SendLocalizedMessage(EffectCliloc);
    }
}
