using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;

namespace Server.Multis;

[SerializationGenerator(0, false)]
public partial class HouseSign : Item
{
    [SerializableField(0, setter: "private")]
    private BaseHouse _owner;

    [SerializableField(1, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _originalOwner;

    public HouseSign(BaseHouse owner) : base(0xBD2)
    {
        Owner = owner;
        OriginalOwner = Owner.Owner;
        Movable = false;
    }

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

    public int LabelNumber => 1061638; // A House Sign

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

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1061639, GetName().FixHtml());            // Name: ~1_NAME~
        list.Add(1061640, Owner?.Owner?.Name ?? "nobody"); // Owner: ~1_OWNER~

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

            LabelTo(from, $"This house is {message}.");
        }

        base.OnSingleClick(from);
    }

    public void ShowSign(Mobile m)
    {
        if (Owner == null)
        {
            return;
        }

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
                    new ClaimHouseWarningGump(okay => ClaimGump_Callback(m, okay))
                );
            }
        }

        ShowSign(m);
    }

    private class ClaimHouseWarningGump : StaticWarningGump<ClaimHouseWarningGump>
    {
        public override int Header => 501036; // Claim house
        public override int HeaderColor => 0x7F00;

        /*
         * You do not currently own any house on any shard with this account, and this house currently does not have an owner.
         * If you wish, you may choose to claim this house and become its rightful owner.
         * If you do this, it will become your Primary house and automatically refresh.
         * If you claim this house, you will be unable to place another house or have another house transferred to you for the next 7 days.
         * Do you wish to claim this house?
         */
        public override int StaticLocalizedContent => 1049719;

        public override int Width => 420;
        public override int Height => 280;

        public ClaimHouseWarningGump(Action<bool> callback) : base(callback)
        {
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (!BaseHouse.NewVendorSystem || !from.Alive || Owner?.IsAosRules != true)
        {
            return;
        }

        if (Owner.AreThereAvailableVendorsFor(from))
        {
            list.Add(new VendorsEntry());
        }

        if (Owner.VendorInventories.Count > 0)
        {
            list.Add(new ReclaimVendorInventoryEntry());
        }
    }

    private class VendorsEntry : ContextMenuEntry
    {
        public VendorsEntry() : base(6211, 5)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!from.CheckAlive() || target is not HouseSign sign || sign.Deleted || sign.Owner?.AreThereAvailableVendorsFor(from) != true)
            {
                return;
            }

            if (from.Map != sign.Map)
            {
                // You must be within five paces of the house sign to use this option.
                from.SendLocalizedMessage(1062429);
            }
            else
            {
                from.SendGump(new HouseGumpAOS(HouseGumpPageAOS.Vendors, from, sign.Owner));
            }
        }
    }

    private class ReclaimVendorInventoryEntry : ContextMenuEntry
    {
        public ReclaimVendorInventoryEntry() : base(6213, 5)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!from.CheckAlive() || target is not HouseSign sign || sign.Deleted || sign.Owner == null ||
                sign.Owner.VendorInventories.Count == 0)
            {
                return;
            }

            if (from.Map != sign.Map)
            {
                // You must be within five paces of the house sign to use this option.
                from.SendLocalizedMessage(1062429);
            }
            else
            {
                from.SendGump(new VendorInventoryGump(sign.Owner, from));
            }
        }
    }
}
