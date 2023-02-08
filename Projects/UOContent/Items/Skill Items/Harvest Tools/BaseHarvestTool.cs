using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public interface IUsesRemaining
{
    int UsesRemaining { get; set; }
    bool ShowUsesRemaining { get; set; }
}

[SerializationGenerator(2, false)]
public abstract partial class BaseHarvestTool : Item, IUsesRemaining, ICraftable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    public BaseHarvestTool(int itemID, int usesRemaining = 50) : base(itemID)
    {
        _usesRemaining = usesRemaining;
        _quality = ToolQuality.Regular;
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public ToolQuality Quality
    {
        get => _quality;
        set
        {
            UnscaleUses();
            _quality = value;
            InvalidateProperties();
            ScaleUses();
            this.MarkDirty();
        }
    }

    public abstract HarvestSystem HarvestSystem { get; }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        Quality = (ToolQuality)quality;

        if (makersMark)
        {
            Crafter = from?.RawName;
        }

        return quality;
    }

    bool IUsesRemaining.ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public void ScaleUses()
    {
        _usesRemaining = _usesRemaining * GetUsesScalar() / 100;
        InvalidateProperties();
    }

    public void UnscaleUses()
    {
        _usesRemaining = _usesRemaining * 100 / GetUsesScalar();
    }

    public int GetUsesScalar() => _quality == ToolQuality.Exceptional ? 200 : 100;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // Makers mark not displayed on OSI
        // if (m_Crafter != null)
        // list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

        if (_quality == ToolQuality.Exceptional)
        {
            list.Add(1060636); // exceptional
        }

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public virtual void DisplayDurabilityTo(Mobile m)
    {
        LabelToAffix(m, 1017323, AffixType.Append, $": {_usesRemaining}"); // Durability
    }

    public override void OnSingleClick(Mobile from)
    {
        DisplayDurabilityTo(from);

        base.OnSingleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack) || Parent == from)
        {
            HarvestSystem.BeginHarvesting(from, this);
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        AddContextMenuEntries(from, this, list, HarvestSystem);
    }

    public static void AddContextMenuEntries(Mobile from, Item item, List<ContextMenuEntry> list, HarvestSystem system)
    {
        if (system != Mining.System)
        {
            return;
        }

        if (!item.IsChildOf(from.Backpack) && item.Parent != from)
        {
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        var miningEntry = new ContextMenuEntry(pm.ToggleMiningStone ? 6179 : 6178);
        miningEntry.Color = 0x421F;
        list.Add(miningEntry);

        list.Add(new ToggleMiningStoneEntry(pm, false, 6176));
        list.Add(new ToggleMiningStoneEntry(pm, true, 6177));
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        var crafter = reader.ReadEntity<Mobile>();
        _quality = (ToolQuality)reader.ReadInt();
        _usesRemaining = reader.ReadInt();

        if (crafter != null)
        {
            Timer.DelayCall(
                (tool, m) =>
                {
                    tool._crafter = m.RawName;
                },
                this,
                crafter
            );
        }
    }

    private class ToggleMiningStoneEntry : ContextMenuEntry
    {
        private PlayerMobile _mobile;
        private bool _value;

        public ToggleMiningStoneEntry(PlayerMobile mobile, bool value, int number) : base(number)
        {
            _mobile = mobile;
            _value = value;

            var stoneMining = mobile.StoneMining && mobile.Skills.Mining.Base >= 100.0;

            if (mobile.ToggleMiningStone == value || value && !stoneMining)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick()
        {
            var oldValue = _mobile.ToggleMiningStone;

            if (_value)
            {
                if (oldValue)
                {
                    _mobile.SendLocalizedMessage(1054023); // You are already set to mine both ore and stone!
                }
                else if (!_mobile.StoneMining || _mobile.Skills.Mining.Base < 100.0)
                {
                    // You have not learned how to mine stone or you do not have enough skill!
                    _mobile.SendLocalizedMessage(1054024);
                }
                else
                {
                    _mobile.ToggleMiningStone = true;
                    _mobile.SendLocalizedMessage(1054022); // You are now set to mine both ore and stone.
                }
            }
            else if (oldValue)
            {
                _mobile.ToggleMiningStone = false;
                _mobile.SendLocalizedMessage(1054020); // You are now set to mine only ore.
            }
            else
            {
                _mobile.SendLocalizedMessage(1054021); // You are already set to mine only ore!
            }
        }
    }
}
