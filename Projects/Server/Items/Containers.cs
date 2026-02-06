using ModernUO.Serialization;
using Server.Accounting;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BankBox : Container
{
    [SerializableField(0, setter: "private")]
    private Mobile _owner;

    [SerializableField(1, setter: "private")]
    private bool _opened;

    public BankBox(Mobile owner) : base(0xE7C)
    {
        Layer = Layer.Bank;
        Movable = false;
        Owner = owner;
    }

    public override int DefaultMaxWeight => 0;

    public override bool IsVirtualItem => true;

    public static bool SendDeleteOnClose { get; set; }

    public void Open()
    {
        if (!ServerFeatureFlags.BankAccess && Owner?.AccessLevel < AccessLevel.Administrator)
        {
            Owner.SendMessage(0x22, "Bank access is temporarily disabled.");
            return;
        }

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

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Owner == null)
        {
            Timer.DelayCall(Delete);
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

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        // This is a horrible hack.
        // TODO: Refactor this by moving BankBox out of the core.
        if (AccountGold.Enabled && AccountGold.ConvertOnBank && item.GetType().Name is "Gold" or "BankCheck")
        {
            return true;
        }

        return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
    }

    public override int GetTotal(TotalType type)
    {
        if (AccountGold.Enabled && Owner?.Account != null && type == TotalType.Gold)
        {
            return Owner.Account.TotalGold;
        }

        return base.GetTotal(type);
    }
}
