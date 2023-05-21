using ModernUO.Serialization;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PowderOfTemperament : Item, IUsesRemaining
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [Constructible]
    public PowderOfTemperament(int charges = 10) : base(4102)
    {
        Weight = 1.0;
        Hue = 2419;
        UsesRemaining = charges;
    }

    public override int LabelNumber => 1049082; // powder of fortifying

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public virtual void DisplayDurabilityTo(Mobile m)
    {
        LabelToAffix(m, 1017323, AffixType.Append, $": {_usesRemaining}"); // Durability
    }

    public override void OnSingleClick(Mobile from)
    {
        DisplayDurabilityTo(from);

        base.OnSingleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.Target = new InternalTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    private class InternalTarget : Target
    {
        private readonly PowderOfTemperament _powder;

        public InternalTarget(PowderOfTemperament powder) : base(2, false, TargetFlags.None) => _powder = powder;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_powder?.Deleted != false || _powder.UsesRemaining <= 0)
            {
                from.SendLocalizedMessage(1049086); // You have used up your powder of temperament.
                return;
            }

            if (targeted is not (Item item and IDurability wearable))
            {
                from.SendLocalizedMessage(1049083); // You cannot use the powder on that item.
                return;
            }

            if (!wearable.CanFortify)
            {
                from.SendLocalizedMessage(1049083); // You cannot use the powder on that item.
                return;
            }

            if (!item.IsChildOf(from.Backpack) && (!Core.ML || item.Parent != from) ||
                !_powder.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            var origMaxHP = wearable.MaxHitPoints;
            var origCurHP = wearable.HitPoints;

            if (origMaxHP <= 0)
            {
                from.SendLocalizedMessage(1049083); // You cannot use the powder on that item.
                return;
            }

            var initMaxHP = Core.AOS ? 255 : wearable.InitMaxHits;

            wearable.UnscaleDurability();

            if (wearable.MaxHitPoints >= initMaxHP)
            {
                from.SendLocalizedMessage(1049085); // The item cannot be improved any further.
                wearable.ScaleDurability();
                return;
            }

            var bonus = initMaxHP - wearable.MaxHitPoints;

            if (bonus > 10)
            {
                bonus = 10;
            }

            wearable.MaxHitPoints += bonus;
            wearable.HitPoints += bonus;

            wearable.ScaleDurability();

            if (wearable.MaxHitPoints > 255)
            {
                wearable.MaxHitPoints = 255;
            }

            if (wearable.HitPoints > 255)
            {
                wearable.HitPoints = 255;
            }

            if (wearable.MaxHitPoints > origMaxHP)
            {
                from.SendLocalizedMessage(1049084); // You successfully use the powder on the item.
                from.PlaySound(0x247);

                --_powder.UsesRemaining;

                if (_powder.UsesRemaining <= 0)
                {
                    from.SendLocalizedMessage(1049086); // You have used up your powder of fortifying.
                    _powder.Delete();
                }
            }
            else
            {
                wearable.MaxHitPoints = origMaxHP;
                wearable.HitPoints = origCurHP;
                from.SendLocalizedMessage(1049085); // The item cannot be improved any further.
            }
        }
    }
}
