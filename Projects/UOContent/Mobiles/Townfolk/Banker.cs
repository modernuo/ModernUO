using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Accounting;
using Server.ContextMenus;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Banker : BaseVendor
{
    private readonly List<SBInfo> m_SBInfos = new();

    [Constructible]
    public Banker() : base("the banker")
    {
    }

    protected override List<SBInfo> SBInfos => m_SBInfos;

    public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;

    public override void InitSBInfo()
    {
        m_SBInfos.Add(new SBBanker());
    }

    public static int GetBalance(Mobile m)
    {
        long balance = 0;

        if (AccountGold.Enabled && m.Account != null)
        {
            balance = m.Account.GetTotalGold();
            if (balance >= int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        Container bank = m.FindBankNoCreate();

        if (bank != null)
        {
            foreach (var gold in bank.FindItemsByType<Gold>())
            {
                balance += gold.Amount;
            }

            if (balance >= int.MaxValue)
            {
                return int.MaxValue;
            }

            foreach (var check in bank.FindItemsByType<BankCheck>())
            {
                balance += check.Worth;
            }
        }

        return (int)Math.Clamp(balance, 0, int.MaxValue);
    }

    public static int GetBalance(Mobile m, out List<Item> gold, out List<Item> checks)
    {
        long balance = 0;
        gold = checks = new List<Item>();

        if (AccountGold.Enabled && m.Account != null)
        {
            balance = m.Account.GetTotalGold();

            if (balance >= int.MaxValue)
            {
                return int.MaxValue;
            }
        }

        Container bank = m.FindBankNoCreate();

        if (bank != null)
        {
            foreach (var g in bank.FindItemsByType<Gold>())
            {
                balance += g.Amount;
                gold.Add(g);
            }

            if (balance >= int.MaxValue)
            {
                return int.MaxValue;
            }

            foreach (var bc in bank.FindItemsByType<BankCheck>())
            {
                balance += bc.Worth;
                checks.Add(bc);
            }
        }

        return (int)Math.Clamp(balance, 0, int.MaxValue);
    }

    public static bool Withdraw(Mobile from, int amount)
    {
        // If for whatever reason the TOL checks fail, we should still try old methods for withdrawing currency.
        if (AccountGold.Enabled && from.Account?.WithdrawGold(amount) == true)
        {
            return true;
        }

        var balance = GetBalance(from, out var gold, out var checks);

        if (balance < amount)
        {
            return false;
        }

        for (var i = 0; amount > 0 && i < gold.Count; ++i)
        {
            if (gold[i].Amount <= amount)
            {
                amount -= gold[i].Amount;
                gold[i].Delete();
            }
            else
            {
                gold[i].Amount -= amount;
                amount = 0;
            }
        }

        for (var i = 0; amount > 0 && i < checks.Count; ++i)
        {
            var check = (BankCheck)checks[i];

            if (check.Worth <= amount)
            {
                amount -= check.Worth;
                check.Delete();
            }
            else
            {
                check.Worth -= amount;
                amount = 0;
            }
        }

        return true;
    }

    public static bool Deposit(Mobile from, int amount)
    {
        // If for whatever reason the TOL checks fail, we should still try old methods for depositing currency.
        if (AccountGold.Enabled && from.Account?.DepositGold(amount) == true)
        {
            return true;
        }

        var box = from.FindBankNoCreate();

        if (box == null)
        {
            return false;
        }

        var items = new List<Item>();

        while (amount > 0)
        {
            Item item;
            if (amount < 5000)
            {
                item = new Gold(amount);
                amount = 0;
            }
            else if (amount <= 1000000)
            {
                item = new BankCheck(amount);
                amount = 0;
            }
            else
            {
                item = new BankCheck(1000000);
                amount -= 1000000;
            }

            if (box.TryDropItem(from, item, false))
            {
                items.Add(item);
            }
            else
            {
                item.Delete();
                foreach (var curItem in items)
                {
                    curItem.Delete();
                }

                return false;
            }
        }

        return true;
    }

    public static int DepositUpTo(Mobile from, int amount)
    {
        // If for whatever reason the TOL checks fail, we should still try old methods for depositing currency.
        if (AccountGold.Enabled && from.Account?.DepositGold(amount) == true)
        {
            return amount;
        }

        var box = from.FindBankNoCreate();

        if (box == null)
        {
            return 0;
        }

        var amountLeft = amount;
        while (amountLeft > 0)
        {
            Item item;
            int amountGiven;

            if (amountLeft < 5000)
            {
                item = new Gold(amountLeft);
                amountGiven = amountLeft;
            }
            else if (amountLeft <= 1000000)
            {
                item = new BankCheck(amountLeft);
                amountGiven = amountLeft;
            }
            else
            {
                item = new BankCheck(1000000);
                amountGiven = 1000000;
            }

            if (box.TryDropItem(from, item, false))
            {
                amountLeft -= amountGiven;
            }
            else
            {
                item.Delete();
                break;
            }
        }

        return amount - amountLeft;
    }

    public static void Deposit(Container cont, int amount)
    {
        while (amount > 0)
        {
            Item item;

            if (amount < 5000)
            {
                item = new Gold(amount);
                amount = 0;
            }
            else if (amount <= 1000000)
            {
                item = new BankCheck(amount);
                amount = 0;
            }
            else
            {
                item = new BankCheck(1000000);
                amount -= 1000000;
            }

            cont.DropItem(item);
        }
    }

    public override bool HandlesOnSpeech(Mobile from) => from.InRange(Location, 12) || base.HandlesOnSpeech(from);

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!e.Handled && e.Mobile.InRange(Location, 12))
        {
            for (var i = 0; i < e.Keywords.Length; ++i)
            {
                var keyword = e.Keywords[i];

                switch (keyword)
                {
                    case 0x0000: // *withdraw*
                        {
                            e.Handled = true;

                            if (e.Mobile.Criminal)
                            {
                                Say(500389); // I will not do business with a criminal!
                                break;
                            }

                            var split = e.Speech.Split(' ');

                            if (split.Length >= 2)
                            {
                                var pack = e.Mobile.Backpack;

                                if (!int.TryParse(split[1], out var amount))
                                {
                                    break;
                                }

                                if (!Core.ML && amount > 5000 || Core.ML && amount > 60000)
                                {
                                    Say(500381); // Thou canst not withdraw so much at one time!
                                }
                                else if (pack?.Deleted != false || !(pack.TotalWeight < pack.MaxWeight) ||
                                         !(pack.TotalItems < pack.MaxItems))
                                {
                                    Say(1048147); // Your backpack can't hold anything else.
                                }
                                else if (amount > 0)
                                {
                                    var box = e.Mobile.FindBankNoCreate();

                                    if (box == null || !Withdraw(e.Mobile, amount))
                                    {
                                        Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                                    }
                                    else
                                    {
                                        pack.DropItem(new Gold(amount));

                                        Say(1010005); // Thou hast withdrawn gold from thy account.
                                    }
                                }
                            }

                            break;
                        }
                    case 0x0001: // *balance*
                        {
                            e.Handled = true;

                            if (e.Mobile.Criminal)
                            {
                                Say(500389); // I will not do business with a criminal!
                                break;
                            }

                            if (AccountGold.Enabled && e.Mobile.Account != null)
                            {
                                Say(
                                    1155855,
                                    $"{e.Mobile.Account.TotalPlat:#,0}\t{e.Mobile.Account.TotalGold:#,0}"
                                ); // Thy current bank balance is ~1_AMOUNT~ platinum and ~2_AMOUNT~ gold.
                            }
                            else
                            {
                                // Thy current bank balance is ~1_AMOUNT~ gold.
                                Say(1042759, $"{GetBalance(e.Mobile):N0}");
                            }

                            break;
                        }
                    case 0x0002: // *bank*
                        {
                            e.Handled = true;

                            if (e.Mobile.Criminal)
                            {
                                Say(500378); // Thou art a criminal and cannot access thy bank box.
                                break;
                            }

                            e.Mobile.BankBox.Open();

                            break;
                        }
                    case 0x0003: // *check*
                        {
                            e.Handled = true;

                            if (AccountGold.Enabled)
                            {
                                break;
                            }

                            if (e.Mobile.Criminal)
                            {
                                Say(500389); // I will not do business with a criminal!
                                break;
                            }

                            var split = e.Speech.Split(' ');

                            if (split.Length >= 2)
                            {
                                if (!int.TryParse(split[1], out var amount))
                                {
                                    break;
                                }

                                if (amount < 5000)
                                {
                                    Say(1010006); // We cannot create checks for such a paltry amount of gold!
                                }
                                else if (amount > 1000000)
                                {
                                    Say(1010007); // Our policies prevent us from creating checks worth that much!
                                }
                                else
                                {
                                    var check = new BankCheck(amount);

                                    var box = e.Mobile.BankBox;

                                    if (!box.TryDropItem(e.Mobile, check, false))
                                    {
                                        Say(500386); // There's not enough room in your bankbox for the check!
                                        check.Delete();
                                    }
                                    else if (!box.ConsumeTotal(typeof(Gold), amount))
                                    {
                                        Say(500384); // Ah, art thou trying to fool me? Thou hast not so much gold!
                                        check.Delete();
                                    }
                                    else
                                    {
                                        Say(
                                            1042673, // Into your bank box I have placed a check in the amount of:
                                            AffixType.Append,
                                            amount.ToString(),
                                            ""
                                        );
                                    }
                                }
                            }

                            break;
                        }
                }
            }
        }

        base.OnSpeech(e);
    }

    public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
    {
        if (from.Alive)
        {
            list.Add(new OpenBankEntry(this));
        }

        base.AddCustomContextEntries(from, list);
    }
}
