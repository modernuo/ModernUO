using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastInnard : Item, IScissorable, ICarvable
{
    public PlagueBeastInnard(int itemID, int hue) : base(itemID)
    {
        Hue = hue;
        Movable = false;
        Weight = 1.0;
    }

    public PlagueBeastLord Owner => RootParent as PlagueBeastLord;
    public override string DefaultName => "plague beast innards";

    public virtual void Carve(Mobile from, Item with)
    {
    }

    public virtual bool Scissor(Mobile from, Scissors scissors) => false;

    public virtual bool OnBandage(Mobile from) => false;

    public override bool IsAccessibleTo(Mobile check)
    {
        if ((int)check.AccessLevel >= (int)AccessLevel.GameMaster)
        {
            return true;
        }

        var owner = Owner;

        if (owner == null)
        {
            return false;
        }

        if (!owner.InRange(check, 2))
        {
            owner.PrivateOverheadMessage(MessageType.Label, 0x3B2, 500446, check.NetState); // That is too far away.
        }
        else if (owner.OpenedBy != null && owner.OpenedBy != check) // TODO check
        {
            // That is being used by someone else
            owner.PrivateOverheadMessage(MessageType.Label, 0x3B2, 500365, check.NetState);
        }
        else if (owner.Frozen)
        {
            return true;
        }

        return false;
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        var owner = Owner;

        if (owner?.Alive != true)
        {
            Delete();
        }
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastComponent : PlagueBeastInnard
{
    [SerializableField(0)]
    private PlagueBeastOrgan _organ;

    public PlagueBeastComponent(int itemID, int hue, bool movable = false) : base(itemID, hue) => Movable = movable;

    public bool IsBrain => ItemID == 0x1CF0;

    public bool IsGland => ItemID == 0x1CEF;

    public bool IsReceptacle => ItemID == 0x9DF;

    public override bool DropToItem(Mobile from, Item target, Point3D p) =>
        target is PlagueBeastBackpack && base.DropToItem(from, target, p);

    public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted) => false;

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p) => false;

    public override bool DropToWorld(Mobile from, Point3D p) => false;

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (_organ?.OnDropped(from, dropped, this) == true && dropped is PlagueBeastComponent component)
        {
            _organ.Add(_organ.Components, component);
        }

        return true;
    }

    public override bool OnDragLift(Mobile from)
    {
        if (IsAccessibleTo(from))
        {
            if (Organ?.OnLifted(from, this) == true)
            {
                // * You rip the organ out of the plague beast's flesh *
                from.SendLocalizedMessage(IsGland ? 1071895 : 1071914, null);

                _organ.Remove(_organ.Components, this);

                Organ = null;
                from.PlaySound(0x1CA);
            }

            return true;
        }

        return false;
    }
}
