using ModernUO.Serialization;
using Server.Accounting;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SecureTradeContainer : Container
{
    public SecureTradeContainer(SecureTrade trade) : base(0x1E5E)
    {
        Trade = trade;

        Movable = false;
    }

    public SecureTrade Trade { get; }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (item == Trade.From.VirtualCheck || item == Trade.To.VirtualCheck)
        {
            return true;
        }

        var to = Trade.From.Container != this ? Trade.From.Mobile : Trade.To.Mobile;

        return m.CheckTrade(to, item, this, message, checkItems, plusItems, plusWeight);
    }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    {
        reject = LRReason.CannotLift;
        return false;
    }

    public override bool IsAccessibleTo(Mobile check) =>
        IsChildOf(check) && Trade?.Valid == true && base.IsAccessibleTo(check);

    public override void OnItemAdded(Item item)
    {
        if (item is not VirtualCheck)
        {
            ClearChecks();
        }
    }

    public override void OnItemRemoved(Item item)
    {
        if (item is not VirtualCheck)
        {
            ClearChecks();
        }
    }

    public override void OnSubItemAdded(Item item)
    {
        if (item is not VirtualCheck)
        {
            ClearChecks();
        }
    }

    public override void OnSubItemRemoved(Item item)
    {
        if (item is not VirtualCheck)
        {
            ClearChecks();
        }
    }

    public void ClearChecks()
    {
        if (Trade == null)
        {
            return;
        }

        if (Trade.From?.IsDisposed == false)
        {
            Trade.From.Accepted = false;
        }

        if (Trade.To?.IsDisposed == false)
        {
            Trade.To.Accepted = false;
        }

        Trade.Update();
    }

    public override bool IsChildVisibleTo(Mobile m, Item child) =>
        child is VirtualCheck
            ? AccountGold.Enabled && m.NetState is not { NewSecureTrading: true }
            : base.IsChildVisibleTo(m, child);
}
