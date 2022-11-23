using System;
using ModernUO.Serialization;
using Server.Network;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseMagicFish : Item
{
    public BaseMagicFish(int hue) : base(0xDD6) => Hue = hue;

    public virtual int Bonus => 0;
    public virtual StatType Type => StatType.Str;

    public override double DefaultWeight => 1.0;

    public virtual bool Apply(Mobile from)
    {
        var applied = SpellHelper.AddStatOffset(from, Type, Bonus, TimeSpan.FromMinutes(1.0));

        if (!applied)
        {
            from.SendLocalizedMessage(502173); // You are already under a similar effect.
        }

        return applied;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (Apply(from))
        {
            from.FixedEffect(0x375A, 10, 15);
            from.PlaySound(0x1E7);
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501774); // You swallow the fish whole!
            Delete();
        }
    }
}

[SerializationGenerator(0, false)]
public partial class PrizedFish : BaseMagicFish
{
    [Constructible]
    public PrizedFish() : base(51)
    {
    }

    public override int Bonus => 5;
    public override StatType Type => StatType.Int;

    public override int LabelNumber => 1041073; // prized fish
}

[SerializationGenerator(0, false)]
public partial class WondrousFish : BaseMagicFish
{
    [Constructible]
    public WondrousFish() : base(86)
    {
    }

    public override int Bonus => 5;
    public override StatType Type => StatType.Dex;

    public override int LabelNumber => 1041074; // wondrous fish
}

[SerializationGenerator(0, false)]
public partial class TrulyRareFish : BaseMagicFish
{
    [Constructible]
    public TrulyRareFish() : base(76)
    {
    }

    public override int Bonus => 5;
    public override StatType Type => StatType.Str;

    public override int LabelNumber => 1041075; // truly rare fish
}

[SerializationGenerator(0, false)]
public partial class PeculiarFish : BaseMagicFish
{
    [Constructible]
    public PeculiarFish() : base(66)
    {
    }

    public override int LabelNumber => 1041076; // highly peculiar fish

    public override bool Apply(Mobile from)
    {
        from.Stam += 10;
        return true;
    }
}