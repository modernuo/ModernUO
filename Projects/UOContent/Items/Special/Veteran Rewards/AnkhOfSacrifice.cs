using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class AnkhOfSacrificeComponent : AddonComponent
{
    public AnkhOfSacrificeComponent(int itemID) : base(itemID)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;
    public override int LabelNumber => 1027772; // Ankh of Sacrifice

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from is PlayerMobile mobile)
        {
            list.Add(new Ankhs.LockKarmaEntry(mobile.KarmaLocked));
        }

        list.Add(new ResurrectEntry());
    }

    public static void Resurrect(PlayerMobile m, AnkhOfSacrificeAddon ankh)
    {
        if (m == null)
        {
        }
        else if (!m.InRange(ankh.GetWorldLocation(), 2))
        {
            m.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (m.Alive)
        {
            m.SendLocalizedMessage(1060197); // You are not dead, and thus cannot be resurrected!
        }
        else if (m.AnkhNextUse > Core.Now)
        {
            var delay = m.AnkhNextUse - Core.Now;

            if (delay.TotalMinutes > 0)
            {
                // You must wait ~1_minutes~ minutes before you can use this item.
                m.SendLocalizedMessage(1079265, Math.Round(delay.TotalMinutes).ToString());
            }
            else
            {
                // You must wait ~1_seconds~ seconds before you can use this item.
                m.SendLocalizedMessage(1079263, Math.Round(delay.TotalSeconds).ToString());
            }
        }
        else
        {
            m.SendGump(new AnkhResurrectGump(m, ResurrectMessage.VirtueShrine));
        }
    }

    private class ResurrectEntry : ContextMenuEntry
    {
        public ResurrectEntry() : base(6195, 2)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is AnkhOfSacrificeAddon { Deleted: false } ankh)
            {
                Resurrect(from as PlayerMobile, ankh);
            }
        }
    }

    private class AnkhResurrectGump : ResurrectGump
    {
        public AnkhResurrectGump(Mobile owner, ResurrectMessage msg) : base(owner, msg)
        {
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID is not 1 and not 2)
            {
                return;
            }

            var from = state.Mobile;

            if (from.Map?.CanFit(from.Location, 16, false, false) != true)
            {
                from.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                return;
            }

            if (from is PlayerMobile mobile)
            {
                mobile.AnkhNextUse = Core.Now + TimeSpan.FromHours(1);
            }

            base.OnResponse(state, info);
        }
    }
}

[SerializationGenerator(0)]
public partial class AnkhOfSacrificeAddon : BaseAddon, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public AnkhOfSacrificeAddon(bool east)
    {
        if (east)
        {
            AddComponent(new AnkhOfSacrificeComponent(0x1D98), 0, 0, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1D97), 0, 1, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CD6), 1, 0, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CD4), 1, 1, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CD0), 2, 0, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CCE), 2, 1, 0);
        }
        else
        {
            AddComponent(new AnkhOfSacrificeComponent(0x1E5D), 0, 0, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1E5C), 1, 0, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CD2), 0, 1, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CD8), 1, 1, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CCD), 0, 2, 0);
            AddComponent(new AnkhOfSacrificeComponent(0x1CCE), 1, 2, 0);
        }
    }

    public override bool HandlesOnMovement => true;

    public override BaseAddonDeed Deed =>
        new AnkhOfSacrificeDeed
        {
            IsRewardItem = _isRewardItem
        };

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!m.Alive && Utility.InRange(Location, m.Location, 1) && !Utility.InRange(Location, oldLocation, 1))
        {
            AnkhOfSacrificeComponent.Resurrect(m as PlayerMobile, this);
        }
    }
}

[SerializationGenerator(0)]
public partial class AnkhOfSacrificeDeed : BaseAddonDeed, IRewardItem, IRewardOption
{
    private bool _east;

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public AnkhOfSacrificeDeed(bool isRewardItem = false)
    {
        LootType = LootType.Blessed;
        _isRewardItem = isRewardItem;
    }

    public override int LabelNumber => 1080397; // Deed For An Ankh Of Sacrifice

    public override BaseAddon Addon =>
        new AnkhOfSacrificeAddon(_east)
        {
            IsRewardItem = _isRewardItem
        };

    public void GetOptions(RewardOptionList list)
    {
        list.Add(1, 1080398); // Ankh of Sacrifice South
        list.Add(2, 1080399); // Ankh of Sacrifice East
    }

    public void OnOptionSelected(Mobile from, int option)
    {
        _east = option switch
        {
            1 => false,
            2 => true,
            _ => _east
        };

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new RewardOptionGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1080457); // 10th Year Veteran Reward
        }
    }
}
