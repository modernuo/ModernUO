using System;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Engines.Quests.Haven;
using Server.Engines.Quests.Necro;
using Server.Mobiles;
using Server.Network;
using CashBankCheckObjective = Server.Engines.Quests.Necro.CashBankCheckObjective;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BankCheck : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _worth;

    [Constructible]
    public BankCheck(int worth) : base(0x14F0)
    {
        Hue = 0x34;
        LootType = LootType.Blessed;

        _worth = worth;
    }

    public override double DefaultWeight => 1.0;

    public override bool DisplayLootType => Core.AOS;

    public override int LabelNumber => 1041361; // A bank check

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        if (Core.ML)
        {
            list.Add(1060738, $"{_worth:N0}"); // value: ~1_val~
        }
        else
        {
            list.Add(1060738, _worth); // value: ~1_val~
        }
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (!AccountGold.Enabled)
        {
            return;
        }

        Mobile owner = null;
        SecureTradeInfo tradeInfo = null;

        var root = parent as Container;

        while (root?.Parent is Container container)
        {
            root = container;
        }

        parent = root ?? parent;

        if (parent is SecureTradeContainer trade && AccountGold.ConvertOnTrade)
        {
            if (trade.Trade.From.Container == trade)
            {
                tradeInfo = trade.Trade.From;
                owner = tradeInfo.Mobile;
            }
            else if (trade.Trade.To.Container == trade)
            {
                tradeInfo = trade.Trade.To;
                owner = tradeInfo.Mobile;
            }
        }
        else if (parent is BankBox box && AccountGold.ConvertOnBank)
        {
            owner = box.Owner;
        }

        if (owner?.Account?.DepositGold(_worth) != true)
        {
            return;
        }

        if (tradeInfo != null)
        {
            if (owner.NetState?.NewSecureTrading == false)
            {
                var plat = Math.DivRem(_worth, AccountGold.CurrencyThreshold, out var gold);

                tradeInfo.Plat += plat;
                tradeInfo.Gold += gold;
            }

            tradeInfo.VirtualCheck?.UpdateTrade(tradeInfo.Mobile);
        }

        owner.SendLocalizedMessage(1042763, $"{_worth:N0}");

        Delete();

        ((Container)parent).UpdateTotals();
    }

    public override void OnSingleClick(Mobile from)
    {
        from.NetState.SendMessageLocalizedAffix(
            Serial,
            ItemID,
            MessageType.Label,
            0x3B2,
            3,
            1041361, // A bank check:
            "",
            AffixType.Append,
            $" {_worth}"
        );
    }

    public override void OnDoubleClick(Mobile from)
    {
        // This probably isn't OSI accurate, but we can't just make the quests redundant.
        // Double-clicking the BankCheck in your pack will now credit your account.
        var box = AccountGold.Enabled ? from.Backpack : from.FindBankNoCreate();

        if (box == null || !IsChildOf(box))
        {
            from.SendLocalizedMessage(AccountGold.Enabled ? 1080058 : 1047026);
            // This must be in your backpack to use it. : That must be in your bank box to use it.
            return;
        }

        if (AccountGold.Enabled)
        {
            if (from.Account?.DepositGold(_worth) != true)
            {
                return;
            }

            Delete();

            // Gold was deposited in your account:
            from.SendLocalizedMessage(1042672, true, $"{_worth:N0}");
        }
        else
        {
            // Internalize the check to free its slot for deposit.
            // reserveSlots: 1 conditionally keeps a slot free for the check to bounce back
            // only if the full amount won't fit — if it fits, no reservation is needed.
            RecordBounce();
            Internalize();

            var deposited = Banker.DepositUpTo(from, _worth, false, 1);

            if (deposited >= _worth)
            {
                Delete();
            }
            else if (deposited > 0)
            {
                Worth -= deposited;
                Bounce(from);
            }
            else
            {
                Bounce(from);
                from.SendLocalizedMessage(500390); // Your bank box is full.
                return;
            }

            // Gold was deposited in your account:
            from.SendLocalizedMessage(1042672, true, $"{deposited:N0}");
        }

        if (from is PlayerMobile pm)
        {
            var qs = pm.Quest;

            if (qs is DarkTidesQuest)
            {
                var obj = qs.FindObjective<CashBankCheckObjective>();

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
            }

            if (qs is UzeraanTurmoilQuest)
            {
                var obj = qs.FindObjective(typeof(Engines.Quests.Haven.CashBankCheckObjective));

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
            }
        }
    }
}
