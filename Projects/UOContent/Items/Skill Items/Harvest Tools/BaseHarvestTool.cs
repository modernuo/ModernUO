using System;
using ModernUO.Serialization;
using Server.Collections;
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

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        AddContextMenuEntries(from, this, ref list, HarvestSystem);
    }

    public static void AddContextMenuEntries(Mobile from, Item item, ref PooledRefList<ContextMenuEntry> list, HarvestSystem system)
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

        list.Add(new ContextMenuEntry(pm.ToggleMiningStone ? 6179 : 6178)
        {
            Color = 0x421F
        });

        var stoneMining = pm.StoneMining && pm.Skills.Mining.Base >= 100.0;
        list.Add(new ToggleMiningStoneEntry(false, pm.ToggleMiningStone, 6176));
        list.Add(new ToggleMiningStoneEntry(true, !pm.ToggleMiningStone && stoneMining, 6177));
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
        private readonly bool _value;

        public ToggleMiningStoneEntry(bool value, bool enabled, int number) : base(number)
        {
            _value = value;
            Enabled = enabled;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from is not PlayerMobile pm)
            {
                return;
            }

            var oldValue = pm.ToggleMiningStone;

            if (_value)
            {
                if (oldValue)
                {
                    pm.SendLocalizedMessage(1054023); // You are already set to mine both ore and stone!
                }
                else if (!pm.StoneMining || pm.Skills.Mining.Base < 100.0)
                {
                    // You have not learned how to mine stone or you do not have enough skill!
                    pm.SendLocalizedMessage(1054024);
                }
                else
                {
                    pm.ToggleMiningStone = true;
                    pm.SendLocalizedMessage(1054022); // You are now set to mine both ore and stone.
                }
            }
            else if (oldValue)
            {
                pm.ToggleMiningStone = false;
                pm.SendLocalizedMessage(1054020); // You are now set to mine only ore.
            }
            else
            {
                pm.SendLocalizedMessage(1054021); // You are already set to mine only ore!
            }
        }
    }
}
