using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;

namespace Server.Multis
{
    public class HouseSign : Item
    {
        public HouseSign(BaseHouse owner) : base(0xBD2)
        {
            Owner = owner;
            OriginalOwner = Owner.Owner;
            Movable = false;
        }

        public HouseSign(Serial serial) : base(serial)
        {
        }

        public BaseHouse Owner { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestrictDecay
        {
            get => Owner?.RestrictDecay == true;
            set
            {
                if (Owner != null)
                {
                    Owner.RestrictDecay = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile OriginalOwner { get; private set; }

        public override bool ForceShowProperties => ObjectPropertyList.Enabled;

        public bool GettingProperties { get; private set; }

        public string GetName() => Name ?? "An Unnamed House";

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (Owner?.Deleted == false)
            {
                Owner.Delete();
            }
        }

        public override void AddNameProperty(IPropertyList list)
        {
            list.Add(1061638); // A House Sign
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(1061639, Utility.FixHtml(GetName()));                         // Name: ~1_NAME~
            list.Add(1061640, Owner?.Owner == null ? "nobody" : Owner.Owner.Name); // Owner: ~1_OWNER~

            if (Owner != null)
            {
                list.Add(Owner.Public ? 1061641 : 1061642); // This House is Open to the Public : This is a Private Home

                GettingProperties = true;
                var level = Owner.DecayLevel;
                GettingProperties = false;

                if (level == DecayLevel.DemolitionPending)
                {
                    list.Add(1062497); // Demolition Pending
                }
                else if (level != DecayLevel.Ageless)
                {
                    if (level == DecayLevel.Collapsed)
                    {
                        level = DecayLevel.IDOC;
                    }

                    list.AddLocalized(1062028, 1043009 + (int)level); // Condition: This structure is ...
                }
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (BaseHouse.DecayEnabled && Owner != null && Owner.DecayPeriod != TimeSpan.Zero)
            {
                var message = Owner.DecayLevel switch
                {
                    DecayLevel.Ageless  => "ageless",
                    DecayLevel.Fairly   => "fairly worn",
                    DecayLevel.Greatly  => "greatly worn",
                    DecayLevel.LikeNew  => "like new",
                    DecayLevel.Slightly => "slightly worn",
                    DecayLevel.Somewhat => "somewhat worn",
                    _                   => "in danger of collapsing"
                };

                LabelTo(from, "This house is {0}.", message);
            }

            base.OnSingleClick(from);
        }

        public void ShowSign(Mobile m)
        {
            if (Owner != null)
            {
                if (Owner.IsFriend(m) && m.AccessLevel < AccessLevel.GameMaster)
                {
                    if (Core.ML && Owner.IsOwner(m) || !Core.ML)
                    {
                        Owner.RefreshDecay();
                    }

                    if (!Core.AOS)
                    {
                        m.SendLocalizedMessage(501293); // Welcome back to the house, friend!
                    }
                }

                if (Owner.IsAosRules)
                {
                    m.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Information, m, Owner));
                }
                else
                {
                    m.SendGump(new HouseGump(m, Owner));
                }
            }
        }

        public void ClaimGump_Callback(Mobile from, bool okay)
        {
            if (okay && Owner != null && Owner.Owner == null && Owner.DecayLevel != DecayLevel.DemolitionPending)
            {
                var canClaim = Owner.CoOwners?.Count > 0 && Owner.IsCoOwner(from) || Owner.IsFriend(from);

                if (canClaim && !BaseHouse.HasAccountHouse(from))
                {
                    Owner.Owner = from;
                    Owner.LastTraded = Core.Now;
                }
            }

            ShowSign(from);
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (Owner == null)
            {
                return;
            }

            if (m.AccessLevel < AccessLevel.GameMaster && Owner.Owner == null &&
                Owner.DecayLevel != DecayLevel.DemolitionPending)
            {
                var canClaim = Owner.IsCoOwner(m) || Owner.IsFriend(m);

                if (canClaim && !BaseHouse.HasAccountHouse(m))
                {
                    m.SendGump(
                        new WarningGump(501036, 32512, 1049719, 32512, 420, 280, okay => ClaimGump_Callback(m, okay))
                    );
                }
            }

            ShowSign(m);
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (!BaseHouse.NewVendorSystem || !from.Alive || Owner?.IsAosRules != true)
            {
                return;
            }

            if (Owner.AreThereAvailableVendorsFor(from))
            {
                list.Add(new VendorsEntry(this));
            }

            if (Owner.VendorInventories.Count > 0)
            {
                list.Add(new ReclaimVendorInventoryEntry(this));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Owner);
            writer.Write(OriginalOwner);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Owner = reader.ReadEntity<BaseHouse>();
                        OriginalOwner = reader.ReadEntity<Mobile>();

                        break;
                    }
            }

            if (Name == "a house sign")
            {
                Name = null;
            }
        }

        private class VendorsEntry : ContextMenuEntry
        {
            private readonly HouseSign m_Sign;

            public VendorsEntry(HouseSign sign) : base(6211) => m_Sign = sign;

            public override void OnClick()
            {
                var from = Owner.From;

                if (!from.CheckAlive() || m_Sign.Deleted || m_Sign.Owner?.AreThereAvailableVendorsFor(from) != true)
                {
                    return;
                }

                if (from.Map != m_Sign.Map || !from.InRange(m_Sign, 5))
                {
                    from.SendLocalizedMessage(
                        1062429
                    ); // You must be within five paces of the house sign to use this option.
                }
                else
                {
                    from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Vendors, from, m_Sign.Owner));
                }
            }
        }

        private class ReclaimVendorInventoryEntry : ContextMenuEntry
        {
            private readonly HouseSign m_Sign;

            public ReclaimVendorInventoryEntry(HouseSign sign) : base(6213) => m_Sign = sign;

            public override void OnClick()
            {
                var from = Owner.From;

                if (m_Sign.Deleted || m_Sign.Owner == null || m_Sign.Owner.VendorInventories.Count == 0 ||
                    !from.CheckAlive())
                {
                    return;
                }

                if (from.Map != m_Sign.Map || !from.InRange(m_Sign, 5))
                {
                    from.SendLocalizedMessage(
                        1062429
                    ); // You must be within five paces of the house sign to use this option.
                }
                else
                {
                    from.CloseGump<VendorInventoryGump>();
                    from.SendGump(new VendorInventoryGump(m_Sign.Owner, from));
                }
            }
        }
    }
}
