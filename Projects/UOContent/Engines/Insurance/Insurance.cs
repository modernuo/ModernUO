using System.Collections.Generic;
using Server.Collections;
using Server.Factions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Insurance;

public static class Insurance
{
    public static bool Enabled => ServerFeatureFlags.InsuranceEnabled;
    private static readonly Dictionary<Mobile, InsuranceContext> _insuranceContexts = [];

    public static void Configure()
    {
        // Legacy, but not expected to be used anymore in favor of the feature flag system
        ServerFeatureFlags.InsuranceEnabled = ServerConfiguration.GetSetting("insurance.enable", Core.AOS);
    }

    public static int GetInsuranceCost(Mobile from, Item item) => 600;

    public static void CheckInsuranceBeforeDeath(Mobile from)
    {
        if (!Enabled)
        {
            return;
        }

        var recentDamager = from.FindMostRecentDamager(false);
        if (recentDamager == null)
        {
            return;
        }

        if (recentDamager is BaseCreature creature)
        {
            recentDamager = creature.GetMaster();
        }

        if (recentDamager != from && recentDamager is PlayerMobile insuranceReward)
        {
            _insuranceContexts[from] = new InsuranceContext(insuranceReward);
        }
    }

    public static void CheckInsuranceOnDeath(Mobile from)
    {
        if (!Enabled || !_insuranceContexts.Remove(from, out var context))
        {
            return;
        }

        if (context.MissedAutoRenewal)
        {
            from.SendLocalizedMessage(1061115); // You do not have the gold to automatically reinsure all your items.
        }

        if (context.InsuranceReward != null && context.InsuranceBonus > 0 &&
            Banker.Deposit(context.InsuranceReward, context.InsuranceBonus))
        {
            // ~1_AMOUNT~ gold has been deposited into your bank box.
            context.InsuranceReward.SendLocalizedMessage(1060397, $"{context.InsuranceBonus}");
        }
    }

    public static bool CheckItemInsuranceOnDeath(PlayerMobile from, Item item)
    {
        if (!Enabled || !item.Insured)
        {
            return false;
        }

        if (from.DuelContext?.Registered == true && from.DuelContext.Started && from.DuelPlayer?.Eliminated != true)
        {
            return true;
        }

        if (!_insuranceContexts.TryGetValue(from, out var context))
        {
            _insuranceContexts[from] = context = new InsuranceContext();
        }

        if (from.AutoRenewInsurance)
        {
            var cost = Insurance.GetInsuranceCost(from, item);

            if (context.InsuranceReward != null)
            {
                cost /= 2;
            }

            if (Banker.Withdraw(from, cost))
            {
                item.PaidInsurance = true;
                // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                from.SendLocalizedMessage(1060398, $"{cost}");
            }
            else
            {
                // TODO: Should this spam?
                // from.SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
                item.PaidInsurance = false;
                item.Insured = false;
                context.MissedAutoRenewal = true;
            }
        }
        else
        {
            item.PaidInsurance = false;
            item.Insured = false;
        }

        context.InsuranceBonus += 300;
        return true;
    }

    public static bool CanInsure(Mobile from, Item item)
    {
        if (!Enabled)
        {
            return false;
        }

        if (item is Container && item is not BaseQuiver || item is BagOfSending or KeyRing or PotionKeg or Sigil)
        {
            return false;
        }

        if (item.Stackable)
        {
            return false;
        }

        if (item.LootType == LootType.Cursed)
        {
            return false;
        }

        if (item.ItemID == 0x204E) // death shroud
        {
            return false;
        }

        if (item.Layer == Layer.Mount)
        {
            return false;
        }

        return item.LootType != LootType.Blessed && item.LootType != LootType.Newbied && item.BlessedFor != from;
    }

    public static void ToggleItemInsurance(Mobile from)
    {
        if (!from.CheckAlive())
        {
            return;
        }

        from.BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance);
        from.SendLocalizedMessage(1060868); // Target the item you wish to toggle insurance status on <ESC> to cancel
    }

    public static void ToggleItemInsurance(Mobile from, object obj)
    {
        if (!from.CheckAlive())
        {
            return;
        }

        ToggleItemInsurance(from, obj as Item, true);
    }

    public static void ToggleItemInsurance(Mobile from, Item item, bool target)
    {
        if (item?.IsChildOf(from) != true)
        {
            if (target)
            {
                from.BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance);
            }

            // You can only insure items that you have equipped or that are in your backpack
            from.SendLocalizedMessage(1060871, "", 0x23);
        }
        else if (item.Insured)
        {
            item.Insured = false;

            from.SendLocalizedMessage(1060874, "", 0x35); // You cancel the insurance on the item

            if (target)
            {
                from.BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance);
                // Target the item you wish to toggle insurance status on <ESC> to cancel
                from.SendLocalizedMessage(1060868, "", 0x23);
            }
        }
        else if (!CanInsure(from, item))
        {
            if (target)
            {
                from.BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance);
            }

            from.SendLocalizedMessage(1060869, "", 0x23); // You cannot insure that
        }
        else
        {
            if (!item.PaidInsurance)
            {
                var cost = GetInsuranceCost(from, item);

                if (Banker.Withdraw(from, cost))
                {
                    // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                    from.SendLocalizedMessage(1060398, $"{cost}");
                    item.PaidInsurance = true;
                }
                else
                {
                    from.SendLocalizedMessage(1061079, "", 0x23); // You lack the funds to purchase the insurance
                    return;
                }
            }

            item.Insured = true;

            from.SendLocalizedMessage(1060873, "", 0x23); // You have insured the item

            if (target)
            {
                from.BeginTarget(-1, false, TargetFlags.None, ToggleItemInsurance);
                // Target the item you wish to toggle insurance status on <ESC> to cancel
                from.SendLocalizedMessage(1060868, "", 0x23);
            }
        }
    }

    public static void AutoRenewInventoryInsurance(Mobile from)
    {
        if (!from.CheckAlive())
        {
            return;
        }

        // You have selected to automatically reinsure all insured items upon death
        from.SendLocalizedMessage(1060881, "", 0x23);
        (from as PlayerMobile)?.AutoRenewInsurance = true;
    }

    public static void CancelRenewInventoryInsurance(Mobile from)
    {
        if (!from.CheckAlive())
        {
            return;
        }

        if (Core.SE)
        {
            from.SendGump(new CancelRenewInventoryInsuranceGump(null));
        }
        else
        {
            // You have cancelled automatically reinsuring all insured items upon death
            from.SendLocalizedMessage(1061075, "", 0x23);
            (from as PlayerMobile)?.AutoRenewInsurance = false;
        }
    }

    public static void OpenItemInsuranceMenu(Mobile from)
    {
        if (!from.CheckAlive() || from.NetState == null)
        {
            return;
        }

        using var queue = PooledRefQueue<Item>.Create(128);

        foreach (var item in from.Items)
        {
            if (DisplayInItemInsuranceGump(from, item))
            {
                queue.Enqueue(item);
            }
        }

        var pack = from.Backpack;

        if (pack != null)
        {
            foreach (var item in pack.FindItems())
            {
                if (DisplayInItemInsuranceGump(from, item))
                {
                    queue.Enqueue(item);
                }
            }
        }

        if (queue.Count == 0)
        {
            // None of your current items meet the requirements for insurance.
            from.SendLocalizedMessage(1114915, "", 0x35);
        }
        else if (from is PlayerMobile pm)
        {
            // TODO: Investigate item sorting
            from.SendGump(new ItemInsuranceMenuGump(pm, queue.ToArray()));
        }
    }

    private static bool DisplayInItemInsuranceGump(Mobile from, Item item) =>
        (item.Visible || from.AccessLevel >= AccessLevel.GameMaster) && (item.Insured || CanInsure(from, item));

    private class InsuranceContext(PlayerMobile insuranceReward = null)
    {
        public PlayerMobile InsuranceReward = insuranceReward;
        public int InsuranceBonus { get; set; }
        public bool MissedAutoRenewal { get; set; }
    }
}
