using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class EndlessDecanter : Pitcher
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _linked;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _linkLocation;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _linkMap;

    [Constructible]
    public EndlessDecanter() : base(BeverageType.Water)
    {
        Hue = 0x399;
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 2.0;

    public override int LabelNumber => 1115929; // Endless Decanter of Water

    public override int ComputeItemID() => 0x0FF6;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!from.Alive)
        {
            return;
        }

        list.Add(new LinkEntry(this));

        if (_linked)
        {
            list.Add(new UnlinkEntry(this));
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1115889); // Auto Water Refill

        if (_linked)
        {
            list.Add(1115893); // Linked
        }
        else
        {
            list.Add(1115894); // Unlinked
        }
    }

    /// <summary>
    /// Triggered by the <see cref="BaseBeverage.Quantity"/> setter after every quantity change.
    /// When the decanter becomes empty and the owner is within 10 tiles of the linked trough,
    /// this method sets <c>Quantity = MaxQuantity</c>, which re-enters the <c>Quantity</c> setter
    /// and calls <c>OnQuantityChanged</c> a second time. The second call is a no-op because
    /// <c>Quantity == 0</c> is false. Recursion depth is bounded to 2.
    /// </summary>
    protected override void OnQuantityChanged()
    {
        if (_linked && Content == BeverageType.Water && Quantity == 0 && RootParent is Mobile owner)
        {
            if (owner.Map == _linkMap && owner.InRange(_linkLocation, 10))
            {
                Quantity = MaxQuantity;
                owner.SendLocalizedMessage(1115901); // The decanter has automatically been filled from the linked water trough.
                owner.PlaySound(0x4E);
            }
            else
            {
                owner.SendLocalizedMessage(1115972); // The decanter's refill attempt failed because the linked water trough is not in the area.
            }
        }
    }

    public static void HandleThrow(Pitcher pitcher, WaterElemental elemental, Mobile thrower)
    {
        if (!pitcher.IsFull)
        {
            thrower.SendLocalizedMessage(1113038); // It is not full.
            return;
        }

        if (!thrower.InRange(elemental.Location, 5))
        {
            thrower.SendLocalizedMessage(500295); // You are too far away to do that.
            return;
        }

        if (!elemental.HasDecanter)
        {
            thrower.SendLocalizedMessage(1115895); // It seems that this water elemental no longer has a magical decanter...
            return;
        }

        thrower.RevealingAction();
        elemental.Damage(1, thrower);

        if (0.1 > Utility.RandomDouble())
        {
            elemental.HasDecanter = false;
            pitcher.Delete();
            thrower.AddToBackpack(new EndlessDecanter());
            thrower.SendLocalizedMessage(1115897); // The water elemental has thrown a magical decanter back to you!
        }
        else
        {
            pitcher.Delete();
            thrower.PlaySound(0x040);
            thrower.SendLocalizedMessage(1115896); // The water pitcher has shattered.
        }
    }

    private class LinkEntry : ContextMenuEntry
    {
        private readonly EndlessDecanter _decanter;

        public LinkEntry(EndlessDecanter decanter) : base(1115891, 0) => _decanter = decanter; // Link

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_decanter.Deleted || !_decanter.Movable || !from.CheckAlive() || !_decanter.CheckItemUse(from))
            {
                return;
            }

            from.SendLocalizedMessage(1115892); // Target a water trough you wish to link.
            from.BeginTarget(10, false, TargetFlags.None, Link_OnTarget, _decanter);
        }
    }

    private static void Link_OnTarget(Mobile from, object targeted, object state)
    {
        var decanter = (EndlessDecanter)state;

        if (decanter?.Deleted != false || !decanter.Movable || !from.CheckAlive() || !decanter.CheckItemUse(from))
        {
            return;
        }

        int itemID;
        Point3D location;
        Map map;

        if (targeted is StaticTarget st)
        {
            itemID = st.ItemID;
            location = st.Location;
            map = from.Map;
        }
        else if (targeted is Item item)
        {
            itemID = item.ItemID;
            location = item.Location;
            map = item.Map;
        }
        else
        {
            from.SendLocalizedMessage(1115900); // Invalid target. Please target a water trough.
            return;
        }

        if (itemID is >= 0xB41 and <= 0xB44)
        {
            decanter.Linked = true;
            decanter.LinkLocation = location;
            decanter.LinkMap = map;

            from.SendLocalizedMessage(1115899); // That water trough has been linked to this decanter.

            // If the decanter is already empty when linked, trigger an immediate refill attempt.
            // OnQuantityChanged is called directly rather than through the Quantity setter because
            // the quantity hasn't changed — we just want to evaluate the refill condition now that
            // a trough location is known.
            if (decanter.IsEmpty && decanter.Content == BeverageType.Water)
            {
                decanter.OnQuantityChanged();
            }
        }
        else
        {
            from.SendLocalizedMessage(1115900); // Invalid target. Please target a water trough.
        }
    }

    private class UnlinkEntry : ContextMenuEntry
    {
        private readonly EndlessDecanter _decanter;

        public UnlinkEntry(EndlessDecanter decanter) : base(1115930, 0) => _decanter = decanter; // Unlink

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_decanter.Deleted || !_decanter.Movable || !from.CheckAlive() || !_decanter.CheckItemUse(from))
            {
                return;
            }

            from.SendLocalizedMessage(1115898); // The link between this decanter and the water trough has been removed.
            _decanter.Linked = false;
        }
    }
}
