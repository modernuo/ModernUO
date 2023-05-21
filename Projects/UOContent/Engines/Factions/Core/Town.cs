using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Factions
{
    [CustomEnum(new[] { "Britain", "Magincia", "Minoc", "Moonglow", "Skara Brae", "Trinsic", "Vesper", "Yew" })]
    public abstract class Town : IComparable<Town>
    {
        public const int SilverCaptureBonus = 10000;

        public static readonly TimeSpan TaxChangePeriod = TimeSpan.FromHours(12.0);
        public static readonly TimeSpan IncomePeriod = TimeSpan.FromDays(1.0);

        private Timer _incomeTimer;
        private TownState m_State;

        public Town()
        {
            m_State = new TownState(this);
            ConstructVendorLists();
            ConstructGuardLists();
            StartIncomeTimer();
        }

        public TownDefinition Definition { get; set; }

        public TownState State
        {
            get => m_State;
            set
            {
                m_State = value;
                ConstructGuardLists();
            }
        }

        public int Silver
        {
            get => m_State.Silver;
            set => m_State.Silver = value;
        }

        public Faction Owner
        {
            get => m_State.Owner;
            set => Capture(value);
        }

        public Mobile Sheriff
        {
            get => m_State.Sheriff;
            set => m_State.Sheriff = value;
        }

        public Mobile Finance
        {
            get => m_State.Finance;
            set => m_State.Finance = value;
        }

        public int Tax
        {
            get => m_State.Tax;
            set => m_State.Tax = value;
        }

        public DateTime LastTaxChange
        {
            get => m_State.LastTaxChange;
            set => m_State.LastTaxChange = value;
        }

        public bool TaxChangeReady => m_State.LastTaxChange + TaxChangePeriod < Core.Now;

        public int FinanceUpkeep
        {
            get
            {
                var vendorLists = VendorLists;
                var upkeep = 0;

                for (var i = 0; i < vendorLists.Count; ++i)
                {
                    upkeep += vendorLists[i].Vendors.Count * vendorLists[i].Definition.Upkeep;
                }

                return upkeep;
            }
        }

        public int SheriffUpkeep
        {
            get
            {
                var guardLists = GuardLists;
                var upkeep = 0;

                for (var i = 0; i < guardLists.Count; ++i)
                {
                    upkeep += guardLists[i].Guards.Count * guardLists[i].Definition.Upkeep;
                }

                return upkeep;
            }
        }

        public int DailyIncome => 10000 * (100 + m_State.Tax) / 100;

        public int NetCashFlow => DailyIncome - FinanceUpkeep - SheriffUpkeep;

        public TownMonolith Monolith
        {
            get
            {
                var monoliths = BaseMonolith.Monoliths;

                foreach (var monolith in monoliths)
                {
                    if (monolith is TownMonolith townMonolith && townMonolith.Town == this)
                    {
                        return townMonolith;
                    }
                }

                return null;
            }
        }

        public DateTime LastIncome
        {
            get => m_State.LastIncome;
            set => m_State.LastIncome = value;
        }

        public List<VendorList> VendorLists { get; set; }

        public List<GuardList> GuardLists { get; set; }

        public static List<Town> Towns => Reflector.Towns;

        public int CompareTo(Town other) => Definition.Sort - (other?.Definition.Sort ?? 0);

        public static Town FromRegion(Region reg)
        {
            if (reg.Map != Faction.Facet)
            {
                return null;
            }

            var towns = Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                var town = towns[i];

                if (reg.IsPartOf(town.Definition.Region))
                {
                    return town;
                }
            }

            return null;
        }

        public void BeginOrderFiring(Mobile from)
        {
            var isFinance = IsFinance(from);
            var isSheriff = IsSheriff(from);
            string type = null;

            // NOTE: Messages not OSI-accurate, intentional
            if (isFinance && isSheriff) // GM only
            {
                type = "vendor or guard";
            }
            else if (isFinance)
            {
                type = "vendor";
            }
            else if (isSheriff)
            {
                type = "guard";
            }

            from.SendMessage($"Target the {type} you wish to dismiss.");
            from.BeginTarget(12, false, TargetFlags.None, EndOrderFiring);
        }

        public void EndOrderFiring(Mobile from, object obj)
        {
            var isFinance = IsFinance(from);
            var isSheriff = IsSheriff(from);
            string type = null;

            if (isFinance && isSheriff) // GM only
            {
                type = "vendor or guard";
            }
            else if (isFinance)
            {
                type = "vendor";
            }
            else if (isSheriff)
            {
                type = "guard";
            }

            if (obj is BaseFactionVendor vendor && vendor.Town == this && isFinance)
            {
                vendor.Delete();
            }
            else if (obj is BaseFactionGuard guard && guard.Town == this && isSheriff)
            {
                guard.Delete();
            }
            else
            {
                from.SendMessage($"That is not a {type}!");
            }
        }

        public void StartIncomeTimer()
        {
            _incomeTimer ??= Timer.DelayCall(TimeSpan.FromMinutes(1.0), TimeSpan.FromMinutes(1.0), CheckIncome);
        }

        public void Delete()
        {
            _incomeTimer?.Stop();
        }

        public void CheckIncome()
        {
            if (LastIncome + IncomePeriod > Core.Now || Owner == null)
            {
                return;
            }

            ProcessIncome();
        }

        public void ProcessIncome()
        {
            LastIncome = Core.Now;

            var flow = NetCashFlow;

            if (Silver + flow < 0)
            {
                var toDelete = BuildFinanceList();

                while (Silver + flow < 0 && toDelete.Count > 0)
                {
                    var mob = toDelete.RandomElement();
                    mob.Delete();

                    toDelete.Remove(mob);
                    flow = NetCashFlow;
                }
            }

            Silver += flow;
        }

        public List<Mobile> BuildFinanceList()
        {
            var list = new List<Mobile>();

            for (var i = 0; i < VendorLists.Count; ++i)
            {
                list.AddRange(VendorLists[i].Vendors);
            }

            for (var i = 0; i < GuardLists.Count; ++i)
            {
                list.AddRange(GuardLists[i].Guards);
            }

            return list;
        }

        public void ConstructGuardLists()
        {
            var defs = Owner?.Definition.Guards ?? Array.Empty<GuardDefinition>();

            GuardLists = new List<GuardList>();

            for (var i = 0; i < defs.Length; ++i)
            {
                GuardLists.Add(new GuardList(defs[i]));
            }
        }

        public GuardList FindGuardList(Type type)
        {
            var guardLists = GuardLists;

            for (var i = 0; i < guardLists.Count; ++i)
            {
                var guardList = guardLists[i];

                if (guardList.Definition.Type == type)
                {
                    return guardList;
                }
            }

            return null;
        }

        public void ConstructVendorLists()
        {
            var defs = VendorDefinition.Definitions;

            VendorLists = new List<VendorList>();

            for (var i = 0; i < defs.Length; ++i)
            {
                VendorLists.Add(new VendorList(defs[i]));
            }
        }

        public VendorList FindVendorList(Type type)
        {
            var vendorLists = VendorLists;

            for (var i = 0; i < vendorLists.Count; ++i)
            {
                var vendorList = vendorLists[i];

                if (vendorList.Definition.Type == type)
                {
                    return vendorList;
                }
            }

            return null;
        }

        public bool RegisterGuard(BaseFactionGuard guard)
        {
            if (guard == null)
            {
                return false;
            }

            var guardList = FindGuardList(guard.GetType());

            if (guardList == null)
            {
                return false;
            }

            guardList.Guards.Add(guard);
            return true;
        }

        public bool UnregisterGuard(BaseFactionGuard guard)
        {
            if (guard == null)
            {
                return false;
            }

            var guardList = FindGuardList(guard.GetType());

            if (guardList == null)
            {
                return false;
            }

            if (!guardList.Guards.Contains(guard))
            {
                return false;
            }

            guardList.Guards.Remove(guard);
            return true;
        }

        public bool RegisterVendor(BaseFactionVendor vendor)
        {
            if (vendor == null)
            {
                return false;
            }

            var vendorList = FindVendorList(vendor.GetType());

            if (vendorList == null)
            {
                return false;
            }

            vendorList.Vendors.Add(vendor);
            return true;
        }

        public bool UnregisterVendor(BaseFactionVendor vendor)
        {
            if (vendor == null)
            {
                return false;
            }

            var vendorList = FindVendorList(vendor.GetType());

            if (vendorList == null)
            {
                return false;
            }

            if (!vendorList.Vendors.Contains(vendor))
            {
                return false;
            }

            vendorList.Vendors.Remove(vendor);
            return true;
        }

        public static void Initialize()
        {
            var towns = Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                towns[i].Sheriff = towns[i].Sheriff;
                towns[i].Finance = towns[i].Finance;
            }

            CommandSystem.Register("GrantTownSilver", AccessLevel.Administrator, GrantTownSilver_OnCommand);
        }

        public bool IsSheriff(Mobile mob) =>
            mob?.Deleted == false &&
            (mob.AccessLevel >= AccessLevel.GameMaster || mob == Sheriff);

        public bool IsFinance(Mobile mob) =>
            mob?.Deleted == false &&
            (mob.AccessLevel >= AccessLevel.GameMaster || mob == Finance);

        public void Capture(Faction f)
        {
            if (m_State.Owner == f)
            {
                return;
            }

            if (m_State.Owner == null) // going from unowned to owned
            {
                LastIncome = Core.Now;
                f.Silver += SilverCaptureBonus;
            }
            else if (f == null) // going from owned to unowned
            {
                LastIncome = DateTime.MinValue;
            }
            else // otherwise changing hands, income timer doesn't change
            {
                f.Silver += SilverCaptureBonus;
            }

            m_State.Owner = f;

            Sheriff = null;
            Finance = null;

            var monolith = Monolith;

            if (monolith != null)
            {
                monolith.Faction = f;
            }

            var vendorLists = VendorLists;

            for (var i = 0; i < vendorLists.Count; ++i)
            {
                var vendorList = vendorLists[i];
                var vendors = vendorList.Vendors;

                for (var j = vendors.Count - 1; j >= 0; --j)
                {
                    vendors[j].Delete();
                }
            }

            var guardLists = GuardLists;

            for (var i = 0; i < guardLists.Count; ++i)
            {
                var guardList = guardLists[i];
                var guards = guardList.Guards;

                for (var j = guards.Count - 1; j >= 0; --j)
                {
                    guards[j].Delete();
                }
            }

            ConstructGuardLists();
        }

        public override string ToString() => Definition.FriendlyName;

        public static void WriteReference(IGenericWriter writer, Town town)
        {
            var idx = Towns.IndexOf(town);

            writer.WriteEncodedInt(idx + 1);
        }

        public static Town ReadReference(IGenericReader reader)
        {
            var idx = reader.ReadEncodedInt() - 1;

            if (idx >= 0 && idx < Towns.Count)
            {
                return Towns[idx];
            }

            return null;
        }

        public static Town Parse(string name)
        {
            var towns = Towns;

            for (var i = 0; i < towns.Count; ++i)
            {
                var town = towns[i];

                if (town.Definition.FriendlyName.InsensitiveEquals(name))
                {
                    return town;
                }
            }

            return null;
        }

        public static void GrantTownSilver_OnCommand(CommandEventArgs e)
        {
            var town = FromRegion(e.Mobile.Region);

            if (town == null)
            {
                e.Mobile.SendMessage("You are not in a faction town.");
            }
            else if (e.Length == 0)
            {
                e.Mobile.SendMessage("Format: GrantTownSilver <amount>");
            }
            else
            {
                town.Silver += e.GetInt32(0);
                e.Mobile.SendMessage(
                    $"You have granted {e.GetInt32(0):N0} silver to the town. It now has {town.Silver:N0} silver."
                );
            }
        }
    }
}
