using Server.Accounting;
using Server.Network;

namespace Server.Items;

public class BankBox : Container
{
    public BankBox(Serial serial) : base(serial)
    {
    }

    public BankBox(Mobile owner) : base(0xE7C)
    {
        Layer = Layer.Bank;
        Movable = false;
        Owner = owner;
    }

    public override int DefaultMaxWeight => 0;

    public override bool IsVirtualItem => true;

    public Mobile Owner { get; private set; }

    public bool Opened { get; private set; }

    public static bool SendDeleteOnClose { get; set; }

    public void Open()
    {
        Opened = true;

        if (Owner != null)
        {
            Owner.PrivateOverheadMessage(
                MessageType.Regular,
                0x3B2,
                true,
                $"Bank container has {TotalItems} items, {TotalWeight} stones",
                Owner.NetState
            );

            Owner.NetState?.SendEquipUpdate(this);
            DisplayTo(Owner);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0); // version

        writer.Write(Owner);
        writer.Write(Opened);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        switch (version)
        {
            case 0:
                {
                    Owner = reader.ReadEntity<Mobile>();
                    Opened = reader.ReadBool();

                    if (Owner == null)
                    {
                        Delete();
                    }

                    break;
                }
        }

        if (ItemID == 0xE41)
        {
            ItemID = 0xE7C;
        }
    }

    public void Close()
    {
        Opened = false;

        if (SendDeleteOnClose)
        {
            Owner?.NetState.SendRemoveEntity(Serial);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
    }

    public override DeathMoveResult OnParentDeath(Mobile parent) => DeathMoveResult.RemainEquipped;

    public override bool IsAccessibleTo(Mobile check) =>
        (check == Owner && Opened || check.AccessLevel >= AccessLevel.GameMaster) && base.IsAccessibleTo(check);

    public override bool OnDragDrop(Mobile from, Item dropped) =>
        (from == Owner && Opened || from.AccessLevel >= AccessLevel.GameMaster) && base.OnDragDrop(from, dropped);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) =>
        (from == Owner && Opened || from.AccessLevel >= AccessLevel.GameMaster) &&
        base.OnDragDropInto(from, item, p);

    public override int GetTotal(TotalType type)
    {
        if (AccountGold.Enabled && Owner?.Account != null && type == TotalType.Gold)
        {
            return Owner.Account.TotalGold;
        }

        return base.GetTotal(type);
    }
}
