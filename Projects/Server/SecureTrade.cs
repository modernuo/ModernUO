using System;
using Server.Accounting;
using Server.Items;
using Server.Network;

namespace Server;

public class SecureTrade
{
    public SecureTrade(Mobile from, Mobile to)
    {
        Valid = true;

        From = new SecureTradeInfo(this, from, new SecureTradeContainer(this));
        To = new SecureTradeInfo(this, to, new SecureTradeContainer(this));

        var from704565 = from.NetState?.NewSecureTrading == true;
        var to704565 = to.NetState?.NewSecureTrading == true;

        from.NetState.SendMobileStatus(from, to);
        from.NetState.SendUpdateSecureTrade(From.Container, false, false);
        from.NetState.SendSecureTradeEquip(To.Container, to);

        from.NetState.SendUpdateSecureTrade(From.Container, false, false);
        from.NetState.SendSecureTradeEquip(From.Container, from);

        from.NetState.SendDisplaySecureTrade(to, From.Container, To.Container, to.Name);
        from.NetState.SendUpdateSecureTrade(From.Container, false, false);

        if (from.Account != null && from704565)
        {
            from.NetState.SendUpdateSecureTrade(
                From.Container,
                TradeFlag.UpdateLedger,
                from.Account.TotalGold,
                from.Account.TotalPlat
            );
        }

        to.NetState.SendMobileStatus(to, from);
        to.NetState.SendUpdateSecureTrade(To.Container, false, false);
        to.NetState.SendSecureTradeEquip(From.Container, from);

        to.NetState.SendUpdateSecureTrade(To.Container, false, false);
        to.NetState.SendSecureTradeEquip(To.Container, to);

        to.NetState.SendDisplaySecureTrade(from, To.Container, From.Container, from.Name);
        to.NetState.SendUpdateSecureTrade(To.Container, false, false);

        if (to.Account != null && to704565)
        {
            to.NetState.SendUpdateSecureTrade(
                To.Container,
                TradeFlag.UpdateLedger,
                to.Account.TotalGold,
                to.Account.TotalPlat
            );
        }
    }

    public SecureTradeInfo From { get; }

    public SecureTradeInfo To { get; }

    public bool Valid { get; private set; }

    public void Cancel()
    {
        if (!Valid)
        {
            return;
        }

        var list = From.Container.Items;

        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (i < list.Count)
            {
                var item = list[i];

                if (item == From.VirtualCheck)
                {
                    continue;
                }

                item.OnSecureTrade(From.Mobile, To.Mobile, From.Mobile, false);

                if (!item.Deleted)
                {
                    From.Mobile.AddToBackpack(item);
                }
            }
        }

        list = To.Container.Items;

        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (i < list.Count)
            {
                var item = list[i];

                if (item == To.VirtualCheck)
                {
                    continue;
                }

                item.OnSecureTrade(To.Mobile, From.Mobile, To.Mobile, false);

                if (!item.Deleted)
                {
                    To.Mobile.AddToBackpack(item);
                }
            }
        }

        Close();
    }

    public void Close()
    {
        if (!Valid)
        {
            return;
        }

        From.Mobile.NetState.SendCloseSecureTrade(From.Container);
        To.Mobile.NetState.SendCloseSecureTrade(To.Container);

        Valid = false;

        var ns = From.Mobile.NetState;

        ns?.RemoveTrade(this);

        ns = To.Mobile.NetState;

        ns?.RemoveTrade(this);

        Timer.StartTimer(From.Dispose);
        Timer.StartTimer(To.Dispose);
    }

    public void UpdateFromCurrency()
    {
        UpdateCurrency(From, To);
    }

    public void UpdateToCurrency()
    {
        UpdateCurrency(To, From);
    }

    private static void UpdateCurrency(SecureTradeInfo left, SecureTradeInfo right)
    {
        if (left.Mobile.NetState?.NewSecureTrading == true)
        {
            var plat = left.Mobile.Account.TotalPlat;
            var gold = left.Mobile.Account.TotalGold;

            left.Mobile.NetState.SendUpdateSecureTrade(left.Container, TradeFlag.UpdateLedger, gold, plat);
        }

        if (right.Mobile.NetState?.NewSecureTrading == true)
        {
            right.Mobile.NetState.SendUpdateSecureTrade(right.Container, TradeFlag.UpdateGold, left.Gold, left.Plat);
        }
    }

    public void Update()
    {
        if (!Valid)
        {
            return;
        }

        if (!From.IsDisposed && From.Accepted && !To.IsDisposed && To.Accepted)
        {
            var list = From.Container.Items;

            var allowed = true;

            for (var i = list.Count - 1; allowed && i >= 0; --i)
            {
                if (i < list.Count)
                {
                    var item = list[i];

                    if (item == From.VirtualCheck)
                    {
                        continue;
                    }

                    if (!item.AllowSecureTrade(From.Mobile, To.Mobile, To.Mobile, true))
                    {
                        allowed = false;
                    }
                }
            }

            list = To.Container.Items;

            for (var i = list.Count - 1; allowed && i >= 0; --i)
            {
                if (i < list.Count)
                {
                    var item = list[i];

                    if (item == To.VirtualCheck)
                    {
                        continue;
                    }

                    if (!item.AllowSecureTrade(To.Mobile, From.Mobile, From.Mobile, true))
                    {
                        allowed = false;
                    }
                }
            }

            if (AccountGold.Enabled)
            {
                if (From.Mobile.Account != null)
                {
                    var totalPlat = From.Mobile.Account.TotalPlat;
                    var totalGold = From.Mobile.Account.TotalGold;

                    if (totalPlat < From.Plat || totalGold < From.Gold)
                    {
                        allowed = false;
                        From.Mobile.SendMessage("You do not have enough currency to complete this trade.");
                    }
                }

                if (To.Mobile.Account != null)
                {
                    var totalPlat = To.Mobile.Account.TotalPlat;
                    var totalGold = To.Mobile.Account.TotalGold;

                    if (totalPlat < To.Plat || totalGold < To.Gold)
                    {
                        allowed = false;
                        To.Mobile.SendMessage("You do not have enough currency to complete this trade.");
                    }
                }
            }

            if (!allowed)
            {
                From.Accepted = false;
                To.Accepted = false;

                From.Mobile.NetState.SendUpdateSecureTrade(From.Container, From.Accepted, To.Accepted);
                To.Mobile.NetState.SendUpdateSecureTrade(To.Container, To.Accepted, From.Accepted);

                return;
            }

            if (AccountGold.Enabled && From.Mobile.Account != null && To.Mobile.Account != null)
            {
                HandleAccountGoldTrade();
            }

            list = From.Container.Items;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    var item = list[i];

                    if (item == From.VirtualCheck)
                    {
                        continue;
                    }

                    item.OnSecureTrade(From.Mobile, To.Mobile, To.Mobile, true);

                    if (!item.Deleted)
                    {
                        To.Mobile.AddToBackpack(item);
                    }
                }
            }

            list = To.Container.Items;

            for (var i = list.Count - 1; i >= 0; --i)
            {
                if (i < list.Count)
                {
                    var item = list[i];

                    if (item == To.VirtualCheck)
                    {
                        continue;
                    }

                    item.OnSecureTrade(To.Mobile, From.Mobile, From.Mobile, true);

                    if (!item.Deleted)
                    {
                        From.Mobile.AddToBackpack(item);
                    }
                }
            }

            Close();
        }
        else if (!From.IsDisposed && !To.IsDisposed)
        {
            From.Mobile.NetState.SendUpdateSecureTrade(From.Container, From.Accepted, To.Accepted);
            To.Mobile.NetState.SendUpdateSecureTrade(To.Container, To.Accepted, From.Accepted);
        }
    }

    private void HandleAccountGoldTrade()
    {
        int fromPlatSend = 0, fromGoldSend = 0, fromPlatRecv = 0, fromGoldRecv = 0;
        int toPlatSend = 0, toGoldSend = 0, toPlatRecv = 0, toGoldRecv = 0;

        if ((From.Plat > 0) & From.Mobile.Account.WithdrawPlat(From.Plat))
        {
            fromPlatSend = From.Plat;

            if (To.Mobile.Account.DepositPlat(From.Plat))
            {
                toPlatRecv = fromPlatSend;
            }
        }

        if ((From.Gold > 0) & From.Mobile.Account.WithdrawGold(From.Gold))
        {
            fromGoldSend = From.Gold;

            if (To.Mobile.Account.DepositGold(From.Gold))
            {
                toGoldRecv = fromGoldSend;
            }
        }

        if ((To.Plat > 0) & To.Mobile.Account.WithdrawPlat(To.Plat))
        {
            toPlatSend = To.Plat;

            if (From.Mobile.Account.DepositPlat(To.Plat))
            {
                fromPlatRecv = toPlatSend;
            }
        }

        if ((To.Gold > 0) & To.Mobile.Account.WithdrawGold(To.Gold))
        {
            toGoldSend = To.Gold;

            if (From.Mobile.Account.DepositGold(To.Gold))
            {
                fromGoldRecv = toGoldSend;
            }
        }

        HandleAccountGoldTrade(From.Mobile, To.Mobile, fromPlatSend, fromGoldSend, fromPlatRecv, fromGoldRecv);
        HandleAccountGoldTrade(To.Mobile, From.Mobile, toPlatSend, toGoldSend, toPlatRecv, toGoldRecv);
    }

    private static void HandleAccountGoldTrade(
        Mobile left,
        Mobile right,
        int platSend,
        int goldSend,
        int platRecv,
        int goldRecv
    )
    {
        if (platSend > 0 || goldSend > 0)
        {
            if (platSend > 0 && goldSend > 0)
            {
                left.SendMessage(
                    "You traded {0:#,0} platinum and {1:#,0} gold to {2}.",
                    platSend,
                    goldSend,
                    right.RawName
                );
            }
            else if (platSend > 0)
            {
                left.SendMessage("You traded {0:#,0} platinum to {1}.", platSend, right.RawName);
            }
            else if (goldSend > 0)
            {
                left.SendMessage("You traded {0:#,0} gold to {1}.", goldSend, right.RawName);
            }
        }

        if (platRecv > 0 || goldRecv > 0)
        {
            if (platRecv > 0 && goldRecv > 0)
            {
                left.SendMessage(
                    "You received {0:#,0} platinum and {1:#,0} gold from {2}.",
                    platRecv,
                    goldRecv,
                    right.RawName
                );
            }
            else if (platRecv > 0)
            {
                left.SendMessage("You received {0:#,0} platinum from {1}.", platRecv, right.RawName);
            }
            else if (goldRecv > 0)
            {
                left.SendMessage("You received {0:#,0} gold from {1}.", goldRecv, right.RawName);
            }
        }
    }
}

public class SecureTradeInfo : IDisposable
{
    public SecureTradeInfo(SecureTrade owner, Mobile m, SecureTradeContainer c)
    {
        Owner = owner;
        Mobile = m;
        Container = c;

        Mobile.AddItem(Container);

        VirtualCheck = new VirtualCheck();
        Container.DropItem(VirtualCheck);
    }

    public SecureTrade Owner { get; private set; }
    public Mobile Mobile { get; private set; }
    public SecureTradeContainer Container { get; private set; }
    public VirtualCheck VirtualCheck { get; private set; }

    public int Gold
    {
        get => VirtualCheck.Gold;
        set => VirtualCheck.Gold = value;
    }

    public int Plat
    {
        get => VirtualCheck.Plat;
        set => VirtualCheck.Plat = value;
    }

    public bool Accepted { get; set; }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        VirtualCheck.Delete();
        VirtualCheck = null;

        Container.Delete();
        Container = null;

        Mobile = null;
        Owner = null;

        IsDisposed = true;
    }
}
