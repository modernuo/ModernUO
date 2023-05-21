using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ValentinesCard : Item
{
    private const string Unsigned = "___";

    [SerializableField(0, getter: "private", setter: "private")]
    private int _labelNumber;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _from;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _to;

    [Constructible]
    public ValentinesCard(int itemid) : base(itemid)
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDouble() < .001 ? 0x47E : 0xE8;
        _labelNumber = Utility.Random(1077589, 5);
    }

    public override string DefaultName => "a Valentine's card";

    /*
     * Five possible messages:
     * To my one true love, ~1_target_player~. Signed: ~2_player~	1077589
     * You’ve pwnd my heart, ~1_target_player~. Signed: ~2_player~	1077590
     * Happy Valentine’s Day, ~1_target_player~. Signed: ~2_player~	1077591
     * Blackrock has driven me crazy... for ~1_target_player~! Signed: ~2_player~	1077592
     * You light my Candle of Love, ~1_target_player~! Signed: ~2_player~	1077593
     *
     */

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(_labelNumber, $"{_to ?? Unsigned}\t{_from ?? Unsigned}");
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, _labelNumber, $"{_to ?? Unsigned}\t{_from ?? Unsigned}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_to == null)
        {
            if (IsChildOf(from))
            {
                from.BeginTarget(10, false, TargetFlags.None, OnTarget);

                from.SendLocalizedMessage(1077497); // To whom do you wish to give this card?
            }
            else
            {
                from.SendLocalizedMessage(1080063); // This must be in your backpack to use it.
            }
        }
    }

    public virtual void OnTarget(Mobile from, object targeted)
    {
        if (Deleted)
        {
            return;
        }

        if (targeted is not Mobile to)
        {
            from.SendLocalizedMessage(1077488); // That's not another player!
            return;
        }

        if (to is not PlayerMobile)
        {
            from.SendLocalizedMessage(1077496); // You can't possibly be THAT lonely!
            return;
        }

        if (to == from)
        {
            from.SendLocalizedMessage(1077495); // You can't give yourself a card, silly!
            return;
        }

        From = from.Name;
        To = to.Name;

        // You fill out the card. Hopefully the other person actually likes you...
        from.SendLocalizedMessage(1077498);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Utility.Intern(ref _from);
        Utility.Intern(ref _to);
    }
}

[SerializationGenerator(0, false)]
public partial class ValentinesCardSouth : ValentinesCard
{
    [Constructible]
    public ValentinesCardSouth() : base(0x0EBD)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ValentinesCardEast : ValentinesCard
{
    [Constructible]
    public ValentinesCardEast() : base(0x0EBE)
    {
    }
}
