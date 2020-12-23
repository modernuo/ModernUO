using Server.Mobiles;
using Server.Network;

namespace Server.Items
{
    public class PlagueBeastInnard : Item, IScissorable, ICarvable
    {
        public PlagueBeastInnard(int itemID, int hue) : base(itemID)
        {
            Hue = hue;
            Movable = false;
            Weight = 1.0;
        }

        public PlagueBeastInnard(Serial serial) : base(serial)
        {
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
            else if (owner.OpenedBy != null && owner.OpenedBy != check)                         // TODO check
            {
                owner.PrivateOverheadMessage(
                    MessageType.Label,
                    0x3B2,
                    500365,
                    check.NetState
                ); // That is being used by someone else
            }
            else if (owner.Frozen)
            {
                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            var owner = Owner;

            if (owner?.Alive != true)
            {
                Delete();
            }
        }
    }

    public class PlagueBeastComponent : PlagueBeastInnard
    {
        public PlagueBeastComponent(int itemID, int hue, bool movable = false) : base(itemID, hue) => Movable = movable;

        public PlagueBeastComponent(Serial serial) : base(serial)
        {
        }

        public PlagueBeastOrgan Organ { get; set; }

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
            if (Organ?.OnDropped(from, dropped, this) == true && dropped is PlagueBeastComponent component)
            {
                Organ.Components.Add(component);
            }

            return true;
        }

        public override bool OnDragLift(Mobile from)
        {
            if (IsAccessibleTo(from))
            {
                if (Organ?.OnLifted(from, this) == true)
                {
                    from.SendLocalizedMessage(
                        IsGland ? 1071895 : 1071914,
                        null
                    ); // * You rip the organ out of the plague beast's flesh *

                    if (Organ.Components.Contains(this))
                    {
                        Organ.Components.Remove(this);
                    }

                    Organ = null;
                    from.PlaySound(0x1CA);
                }

                return true;
            }

            return false;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Organ);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Organ = reader.ReadEntity<PlagueBeastOrgan>();
        }
    }
}
