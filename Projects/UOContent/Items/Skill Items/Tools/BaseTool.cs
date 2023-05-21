using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items;

public enum ToolQuality
{
    Low,
    Regular,
    Exceptional
}

[SerializationGenerator(2, false)]
public abstract partial class BaseTool : Item, IUsesRemaining, ICraftable
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafter;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    public BaseTool(int itemID) : this(Utility.RandomMinMax(25, 75), itemID)
    {
    }

    public BaseTool(int uses, int itemID) : base(itemID)
    {
        _usesRemaining = uses;
        _quality = ToolQuality.Regular;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1)]
    public ToolQuality Quality
    {
        get => _quality;
        set
        {
            UnscaleUses();
            _quality = value;
            ScaleUses();
            this.MarkDirty();
        }
    }

    public virtual bool BreakOnDepletion => true;

    public abstract CraftSystem CraftSystem { get; }

    private bool ShowUsesRemaining { get; set; } = true;

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
        get => ShowUsesRemaining;
        set => ShowUsesRemaining = value;
    }

    public void ScaleUses()
    {
        UsesRemaining = _usesRemaining * GetUsesScalar() / 100;
        InvalidateProperties();
    }

    public void UnscaleUses()
    {
        UsesRemaining = _usesRemaining * 100 / GetUsesScalar();
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

    public static bool CheckAccessible(Item tool, Mobile m) => tool.IsChildOf(m) || tool.Parent == m;

    public static bool CheckTool(Item tool, Mobile m)
    {
        var check = m.FindItemOnLayer(Layer.OneHanded);

        if (check is BaseTool && check != tool && check is not AncientSmithyHammer)
        {
            return false;
        }

        check = m.FindItemOnLayer(Layer.TwoHanded);

        return check is not BaseTool || check == tool || check is AncientSmithyHammer;
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
            var system = CraftSystem;

            var num = system.CanCraft(from, this, null);

            // Blacksmithing shows the gump regardless of proximity of an anvil and forge after SE
            if (num > 0 && (num != 1044267 || !Core.SE))
            {
                from.SendLocalizedMessage(num);
            }
            else
            {
                from.SendGump(new CraftGump(from, system, this, null));
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        Timer.DelayCall((item, crafter) => item._crafter = crafter?.RawName, this, reader.ReadEntity<Mobile>());
        _quality = (ToolQuality)reader.ReadInt();
        _usesRemaining = reader.ReadInt();
    }
}
