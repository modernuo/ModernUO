using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class EnhancedBandage : Bandage
{
    [Constructible]
    public EnhancedBandage(int amount = 1) : base(amount) => Hue = 0x8A5;

    // TODO: On BandageContext, check if enhanced, and add this value
    public const int HealingBonus = 10;

    public override int LabelNumber => 1152441; // enhanced bandage

    public override bool Dye(Mobile from, DyeTub sender) => false;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1075216); // these bandages have been enhanced
    }
}

[Flippable(0x2AC0, 0x2AC3)]
[SerializationGenerator(1)]
public partial class FountainOfLife : BaseAddonContainer
{
    public const int MaxCharges = 10;

    [SerializableField(1)]
    private Timer _timer;

    [Constructible]
    public FountainOfLife(int charges = MaxCharges) : base(0x2AC0)
    {
        _charges = charges;
    }

    [DeserializeTimerField(1)]
    private void DeserializeTimer(TimeSpan delay)
    {
        _timer = Timer.DelayCall(Utility.Max(delay, TimeSpan.Zero), RechargeTime, Recharge);
    }

    public override BaseAddonContainerDeed Deed => new FountainOfLifeDeed(_charges);

    public virtual TimeSpan RechargeTime => TimeSpan.FromDays(1);

    public override int LabelNumber => 1075197; // Fountain of Life
    public override int DefaultGumpID => 0x484;
    public override int DefaultDropSound => 66;
    public override int DefaultMaxItems => 125;

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = Math.Min(value, MaxCharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override bool OnDragLift(Mobile from) => false;

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is not Bandage)
        {
            from.SendLocalizedMessage(1075209); // Only bandages may be dropped into the fountain.
            return false;
        }

        if (base.OnDragDrop(from, dropped))
        {
            Enhance(from);
            return true;
        }

        return false;
    }

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
    {
        if (item is not Bandage)
        {
            from.SendLocalizedMessage(1075209); // Only bandages may be dropped into the fountain.
            return false;
        }

        if (base.OnDragDropInto(from, item, p))
        {
            Enhance(from);
            return true;
        }

        return false;
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1075217, _charges); // ~1_val~ charges remaining
    }

    public override void OnDelete()
    {
        _timer?.Stop();
        base.OnDelete();
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _charges = reader.ReadInt();

        var next = reader.ReadDateTime();

        var now = Core.Now;

        DeserializeTimer(next - now);
    }

    public void Recharge()
    {
        Charges = MaxCharges;
        Enhance(null);
    }

    public void Enhance(Mobile from)
    {
        for (var i = Items.Count - 1; i >= 0 && _charges > 0; --i)
        {
            if (Items[i] is EnhancedBandage)
            {
                continue;
            }

            if (Items[i] is Bandage bandage)
            {
                Item enhanced;

                if (bandage.Amount > _charges)
                {
                    bandage.Amount -= _charges;
                    enhanced = new EnhancedBandage(_charges);
                    Charges = 0;
                }
                else
                {
                    enhanced = new EnhancedBandage(bandage.Amount);
                    Charges -= bandage.Amount;
                    bandage.Delete();
                }

                if (from == null || !TryDropItem(from, enhanced, false)) // try stacking first
                {
                    DropItem(enhanced);
                }
            }
        }

        InvalidateProperties();
    }
}

[SerializationGenerator(0)]
public partial class FountainOfLifeDeed : BaseAddonContainerDeed
{
    [Constructible]
    public FountainOfLifeDeed(int charges = FountainOfLife.MaxCharges)
    {
        LootType = LootType.Blessed;
        _charges = charges;
    }

    public override int LabelNumber => 1075197; // Fountain of Life
    public override BaseAddonContainer Addon => new FountainOfLife(_charges);

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges
    {
        get => _charges;
        set
        {
            _charges = Math.Min(value, FountainOfLife.MaxCharges);
            InvalidateProperties();
            this.MarkDirty();
        }
    }
}
