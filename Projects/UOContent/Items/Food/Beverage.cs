using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Engines.Quests;
using Server.Engines.Quests.Hag;
using Server.Engines.Quests.Matriarch;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

public enum BeverageType
{
    Ale,
    Cider,
    Liquor,
    Milk,
    Wine,
    Water
}

public interface IHasQuantity
{
    int Quantity { get; set; }
}

public interface IWaterSource : IHasQuantity
{
}

// TODO: Flippable attributes
[SerializationGenerator(0, false)]
[TypeAlias("Server.Items.BottleAle", "Server.Items.BottleLiquor", "Server.Items.BottleWine")]
public partial class BeverageBottle : BaseBeverage
{
    [Constructible]
    public BeverageBottle(BeverageType type) : base(type) => Weight = 1.0;

    public override int BaseLabelNumber => 1042959; // a bottle of Ale
    public override int MaxQuantity => 5;
    public override bool Fillable => false;

    public override int ComputeItemID()
    {
        if (IsEmpty)
        {
            return 0;
        }

        return Content switch
        {
            BeverageType.Ale    => 0x99F,
            BeverageType.Cider  => 0x99F,
            BeverageType.Liquor => 0x99B,
            BeverageType.Milk   => 0x99B,
            BeverageType.Wine   => 0x9C7,
            BeverageType.Water  => 0x99B,
            _                   => 0
        };
    }
}

[SerializationGenerator(0, false)]
public partial class Jug : BaseBeverage
{
    [Constructible]
    public Jug(BeverageType type) : base(type) => Weight = 1.0;

    public override int BaseLabelNumber => 1042965; // a jug of Ale
    public override int MaxQuantity => 10;
    public override bool Fillable => false;

    public override int ComputeItemID() => !IsEmpty ? 0x9C8 : 0;
}

[SerializationGenerator(0, false)]
public partial class CeramicMug : BaseBeverage
{
    [Constructible]
    public CeramicMug() => Weight = 1.0;

    [Constructible]
    public CeramicMug(BeverageType type) : base(type) => Weight = 1.0;

    public override int BaseLabelNumber => 1042982; // a ceramic mug of Ale
    public override int MaxQuantity => 1;

    public override int ComputeItemID() => ItemID is (< 0x995 or > 0x999) and not 0x9CA ? 0x995 : ItemID;
}

[SerializationGenerator(0, false)]
public partial class PewterMug : BaseBeverage
{
    [Constructible]
    public PewterMug() => Weight = 1.0;

    [Constructible]
    public PewterMug(BeverageType type) : base(type) => Weight = 1.0;

    public override int BaseLabelNumber => 1042994; // a pewter mug with Ale
    public override int MaxQuantity => 1;

    public override int ComputeItemID() => ItemID is >= 0xFFF and <= 0x1002 ? ItemID : 0xFFF;
}

[SerializationGenerator(0, false)]
public partial class Goblet : BaseBeverage
{
    [Constructible]
    public Goblet() => Weight = 1.0;

    [Constructible]
    public Goblet(BeverageType type) : base(type) => Weight = 1.0;

    public override int BaseLabelNumber => 1043000; // a goblet of Ale
    public override int MaxQuantity => 1;

    public override int ComputeItemID() => ItemID is 0x99A or 0x9B3 or 0x9BF or 0x9CB ? ItemID : 0x99A;
}

[TypeAlias(
    "Server.Items.MugAle",
    "Server.Items.GlassCider",
    "Server.Items.GlassLiquor",
    "Server.Items.GlassMilk",
    "Server.Items.GlassWine",
    "Server.Items.GlassWater"
)]
[SerializationGenerator(0, false)]
public partial class GlassMug : BaseBeverage
{
    [Constructible]
    public GlassMug() => Weight = 1.0;

    [Constructible]
    public GlassMug(BeverageType type) : base(type) => Weight = 1.0;

    public override int EmptyLabelNumber => 1022456; // mug
    public override int BaseLabelNumber => 1042976;  // a mug of Ale
    public override int MaxQuantity => 5;

    public override int ComputeItemID()
    {
        if (IsEmpty)
        {
            return ItemID is >= 0x1F81 and <= 0x1F84 ? ItemID : 0x1F81;
        }

        return Content switch
        {
            BeverageType.Ale    => ItemID == 0x9EF ? 0x9EF : 0x9EE,
            BeverageType.Cider  => Math.Clamp(ItemID, 0x1F7D, 0x1F80),
            BeverageType.Liquor => Math.Clamp(ItemID, 0x1F85, 0x1F88),
            BeverageType.Milk   => Math.Clamp(ItemID, 0x1F89, 0x1F8C),
            BeverageType.Wine   => Math.Clamp(ItemID, 0x1F8D, 0x1F90),
            BeverageType.Water  => Math.Clamp(ItemID, 0x1F91, 0x1F94),
            _                   => 0
        };
    }
}

[TypeAlias(
    "Server.Items.PitcherAle",
    "Server.Items.PitcherCider",
    "Server.Items.PitcherLiquor",
    "Server.Items.PitcherMilk",
    "Server.Items.PitcherWine",
    "Server.Items.PitcherWater",
    "Server.Items.GlassPitcher"
)]
[SerializationGenerator(0, false)]
public partial class Pitcher : BaseBeverage
{
    [Constructible]
    public Pitcher() => Weight = 2.0;

    [Constructible]
    public Pitcher(BeverageType type) : base(type) => Weight = 2.0;

    public override int BaseLabelNumber => 1048128; // a Pitcher of Ale
    public override int MaxQuantity => 5;

    public override int ComputeItemID()
    {
        if (IsEmpty)
        {
            return ItemID is 0x9A7 or 0xFF7 ? ItemID : 0xFF6;
        }

        return Content switch
        {
            BeverageType.Ale    => ItemID == 0x1F96 ? ItemID : 0x1F95,
            BeverageType.Cider  => ItemID == 0x1F98 ? ItemID : 0x1F97,
            BeverageType.Liquor => ItemID == 0x1F9A ? ItemID : 0x1F99,
            BeverageType.Milk   => ItemID == 0x9AD ? ItemID : 0x9F0,
            BeverageType.Wine   => ItemID == 0x1F9C ? ItemID : 0x1F9B,
            BeverageType.Water  => ItemID is 0xFF8 or 0xFF9 or 0x1F9E ? ItemID : 0x1F9D,
            _                   => 0
        };
    }
}

[SerializationGenerator(2, false)]
public abstract partial class BaseBeverage : Item, IHasQuantity
{
    private readonly int[] _swampTiles =
    {
        0x9C4, 0x9EB,
        0x3D65, 0x3D65,
        0x3DC0, 0x3DD9,
        0x3DDB, 0x3DDC,
        0x3DDE, 0x3EF0,
        0x3FF6, 0x3FF6,
        0x3FFC, 0x3FFE
    };

    private static readonly Dictionary<Mobile, Timer> m_Table = new();

    [SerializableField(0)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Poison _poison;

    [SerializableField(1)]
    [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
    private Mobile _poisoner;

    public BaseBeverage() => ItemID = ComputeItemID();

    public BaseBeverage(BeverageType type)
    {
        _content = type;
        _quantity = MaxQuantity;
        ItemID = ComputeItemID();
    }

    public override int LabelNumber =>
        IsEmpty || BaseLabelNumber == 0 ? EmptyLabelNumber : BaseLabelNumber + (int)_content;

    public virtual bool ShowQuantity => MaxQuantity > 1;
    public virtual bool Fillable => true;
    public virtual bool Pourable => true;

    public virtual int EmptyLabelNumber => base.LabelNumber;
    public virtual int BaseLabelNumber => 0;

    public abstract int MaxQuantity { get; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsEmpty => _quantity <= 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ContainsAlcohol => !IsEmpty && _content is not BeverageType.Milk and not BeverageType.Water;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsFull => _quantity >= MaxQuantity;

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public BeverageType Content
    {
        get => _content;
        set
        {
            _content = value;

            InvalidateProperties();

            var itemID = ComputeItemID();

            if (itemID > 0)
            {
                ItemID = itemID;
            }
            else
            {
                Delete();
            }
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = Math.Clamp(value, 0, MaxQuantity);

            InvalidateProperties();

            var itemID = ComputeItemID();

            if (itemID > 0)
            {
                ItemID = itemID;
            }
            else
            {
                Delete();
            }
        }
    }

    public abstract int ComputeItemID();

    public virtual int GetQuantityDescription()
    {
        return (_quantity * 100 / MaxQuantity) switch
        {
            <= 0  => 1042975,
            <= 33 => 1042974,
            <= 66 => 1042973,
            _     => 1042972
        };
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ShowQuantity)
        {
            list.Add(GetQuantityDescription());
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (ShowQuantity)
        {
            LabelTo(from, GetQuantityDescription());
        }
    }

    public virtual bool ValidateUse(Mobile from, bool message)
    {
        if (Deleted)
        {
            return false;
        }

        if (!Movable && !Fillable)
        {
            var house = BaseHouse.FindHouseAt(this);

            if (house?.HasLockedDownItem(this) != true)
            {
                if (message)
                {
                    from.SendLocalizedMessage(502946, "", 0x59); // That belongs to someone else.
                }

                return false;
            }
        }

        if (from.Map != Map || !from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
        {
            if (message)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }

            return false;
        }

        return true;
    }

    public virtual void Fill_OnTarget(Mobile from, object targ)
    {
        if (!IsEmpty || !Fillable || !ValidateUse(from, false))
        {
            return;
        }

        if (targ is BaseBeverage bev)
        {
            if (bev.IsEmpty || !bev.ValidateUse(from, true))
            {
                return;
            }

            Content = bev.Content;
            Poison = bev.Poison;
            Poisoner = bev.Poisoner;

            if (bev.Quantity > MaxQuantity)
            {
                Quantity = MaxQuantity;
                bev.Quantity -= MaxQuantity;
            }
            else
            {
                Quantity += bev.Quantity;
                bev.Quantity = 0;
            }
        }
        else if (targ is BaseWaterContainer bwc)
        {
            if (Quantity == 0 || Content == BeverageType.Water && !IsFull)
            {
                var iNeed = Math.Min(MaxQuantity - Quantity, bwc.Quantity);

                if (iNeed > 0 && !bwc.IsEmpty && !IsFull)
                {
                    bwc.Quantity -= iNeed;
                    Quantity += iNeed;
                    Content = BeverageType.Water;

                    from.PlaySound(0x4E);
                }
            }
        }
        else if (targ is Item item)
        {
            var src = item as IWaterSource;

            if (src == null && item is AddonComponent component)
            {
                src = component.Addon as IWaterSource;
            }

            if (src is not { Quantity: > 0 })
            {
                return;
            }

            if (from.Map != item.Map || !from.InRange(item.GetWorldLocation(), 2) || !from.InLOS(item))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            Content = BeverageType.Water;
            Poison = null;
            Poisoner = null;

            if (src.Quantity > MaxQuantity)
            {
                Quantity = MaxQuantity;
                src.Quantity -= MaxQuantity;
            }
            else
            {
                Quantity += src.Quantity;
                src.Quantity = 0;
            }

            from.SendLocalizedMessage(1010089); // You fill the container with water.
        }
        else if (targ is Cow cow)
        {
            if (cow.TryMilk(from))
            {
                Content = BeverageType.Milk;
                Quantity = MaxQuantity;
                from.SendLocalizedMessage(1080197); // You fill the container with milk.
            }
        }
        else if (targ is LandTarget target)
        {
            var tileID = target.TileID;

            if (from is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is not WitchApprenticeQuest)
                {
                    return;
                }

                var obj = qs.FindObjective<FindIngredientObjective>();

                if (obj?.Completed == true && obj.Ingredient == Ingredient.SwampWater)
                {
                    var contains = false;

                    for (var i = 0; !contains && i < _swampTiles.Length; i += 2)
                    {
                        contains = tileID >= _swampTiles[i] && tileID <= _swampTiles[i + 1];
                    }

                    if (contains)
                    {
                        Delete();

                        // You dip the container into the disgusting swamp water, collecting enough for the Hag's vile stew.
                        player.SendLocalizedMessage(1055035);
                        obj.Complete();
                    }
                }
            }
        }
    }

    public virtual void Pour_OnTarget(Mobile from, object targ)
    {
        if (IsEmpty || !Pourable || !ValidateUse(from, false))
        {
            return;
        }

        if (targ is BaseBeverage bev)
        {
            if (!bev.ValidateUse(from, true))
            {
                return;
            }

            if (bev.IsFull && bev.Content == Content)
            {
                from.SendLocalizedMessage(500848); // Couldn't pour it there.  It was already full.
            }
            else if (!bev.IsEmpty)
            {
                from.SendLocalizedMessage(500846); // Can't pour it there.
            }
            else
            {
                bev.Content = Content;
                bev.Poison = Poison;
                bev.Poisoner = Poisoner;

                if (Quantity > bev.MaxQuantity)
                {
                    bev.Quantity = bev.MaxQuantity;
                    Quantity -= bev.MaxQuantity;
                }
                else
                {
                    bev.Quantity += Quantity;
                    Quantity = 0;
                }

                from.PlaySound(0x4E);
            }
        }
        else if (from == targ)
        {
            if (from.Thirst < 20)
            {
                from.Thirst += 1;
            }

            if (ContainsAlcohol)
            {
                var bac = Content switch
                {
                    BeverageType.Ale    => 1,
                    BeverageType.Wine   => 2,
                    BeverageType.Cider  => 3,
                    BeverageType.Liquor => 4,
                    _                   => 0
                };

                from.BAC = Math.Min(from.BAC + bac, 60);

                CheckHeaveTimer(from);
            }

            from.PlaySound(Utility.RandomList(0x30, 0x2D6));

            if (Poison != null)
            {
                from.ApplyPoison(Poisoner, Poison);
            }

            --Quantity;
        }
        else if (targ is BaseWaterContainer bwc)
        {
            if (Content != BeverageType.Water)
            {
                from.SendLocalizedMessage(500842); // Can't pour that in there.
            }
            else if (bwc.Items.Count != 0)
            {
                from.SendLocalizedMessage(500841); // That has something in it.
            }
            else
            {
                var itNeeds = Math.Min(bwc.MaxQuantity - bwc.Quantity, Quantity);

                if (itNeeds > 0)
                {
                    bwc.Quantity += itNeeds;
                    Quantity -= itNeeds;

                    from.PlaySound(0x4E);
                }
            }
        }
        else if (targ is PlantItem item)
        {
            item.Pour(from, this);
        }
        else if (targ is AddonComponent component &&
                 component.Addon is WaterVatEast or WaterVatSouth &&
                 Content == BeverageType.Water)
        {
            if (from is PlayerMobile { Quest: SolenMatriarchQuest qs })
            {
                QuestObjective obj = qs.FindObjective<GatherWaterObjective>();

                if (obj?.Completed == false)
                {
                    var vat = component.Addon;

                    if (vat.X > 5784 && vat.X < 5814 && vat.Y > 1903 && vat.Y < 1934 &&
                        (qs.RedSolen && vat.Map == Map.Trammel || !qs.RedSolen && vat.Map == Map.Felucca))
                    {
                        if (obj.CurProgress + Quantity > obj.MaxProgress)
                        {
                            var delta = obj.MaxProgress - obj.CurProgress;

                            Quantity -= delta;
                            obj.CurProgress = obj.MaxProgress;
                        }
                        else
                        {
                            obj.CurProgress += Quantity;
                            Quantity = 0;
                        }
                    }
                }
            }
        }
        else
        {
            from.SendLocalizedMessage(500846); // Can't pour it there.
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsEmpty)
        {
            if (!Fillable || !ValidateUse(from, true))
            {
                return;
            }

            from.BeginTarget(-1, true, TargetFlags.None, Fill_OnTarget);
            SendLocalizedMessageTo(from, 500837); // Fill from what?
        }
        else if (Pourable && ValidateUse(from, true))
        {
            from.BeginTarget(-1, true, TargetFlags.None, Pour_OnTarget);
            from.SendLocalizedMessage(1010086); // What do you want to use this on?
        }
    }

    public static bool ConsumeTotal(Container pack, BeverageType content, int quantity) =>
        ConsumeTotal(pack, typeof(BaseBeverage), content, quantity);

    public static bool ConsumeTotal(Container pack, Type itemType, BeverageType content, int quantity)
    {
        var items = pack.FindItemsByType(itemType);

        // First pass, compute total
        var total = 0;

        for (var i = 0; i < items.Length; ++i)
        {
            if (items[i] is BaseBeverage bev && bev.Content == content && !bev.IsEmpty)
            {
                total += bev.Quantity;
            }
        }

        if (total >= quantity)
        {
            // We've enough, so consume it

            var need = quantity;

            for (var i = 0; i < items.Length; ++i)
            {
                if (items[i] is not BaseBeverage bev || bev.Content != content || bev.IsEmpty)
                {
                    continue;
                }

                var theirQuantity = bev.Quantity;

                if (theirQuantity < need)
                {
                    bev.Quantity = 0;
                    need -= theirQuantity;
                }
                else
                {
                    bev.Quantity -= need;
                    return true;
                }
            }
        }

        return false;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _poison = reader.ReadPoison();
        _poisoner = reader.ReadEntity<Mobile>();
        _content = (BeverageType)reader.ReadInt();
        _quantity = reader.ReadInt();
    }

    public static void Initialize()
    {
        EventSink.Login += EventSink_Login;
    }

    private static void EventSink_Login(Mobile m)
    {
        CheckHeaveTimer(m);
    }

    public static void CheckHeaveTimer(Mobile from)
    {
        if (from.BAC > 0 && from.Map != Map.Internal && !from.Deleted)
        {
            if (m_Table.ContainsKey(from))
            {
                return;
            }

            if (from.BAC > 60)
            {
                from.BAC = 60;
            }

            m_Table[from] = new HeaveTimer(from).Start();
        }
        else if (m_Table.Remove(from, out var t))
        {
            t.Stop();

            from.SendLocalizedMessage(500850); // You feel sober.
        }
    }

    private class HeaveTimer : Timer
    {
        private readonly Mobile m_Drunk;

        public HeaveTimer(Mobile drunk) : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0)) =>
            m_Drunk = drunk;

        protected override void OnTick()
        {
            if (m_Drunk.Deleted || m_Drunk.Map == Map.Internal)
            {
                Stop();
                m_Table.Remove(m_Drunk);
            }
            else if (m_Drunk.Alive)
            {
                if (m_Drunk.BAC > 60)
                {
                    m_Drunk.BAC = 60;
                }

                // chance to get sober
                if (Utility.Random(100) < 10)
                {
                    --m_Drunk.BAC;
                }

                // lose some stats
                m_Drunk.Stam -= 1;
                m_Drunk.Mana -= 1;

                if (Utility.Random(1, 4) == 1)
                {
                    if (!m_Drunk.Mounted)
                    {
                        // turn in a random direction
                        m_Drunk.Direction = (Direction)Utility.Random(8);

                        // heave
                        m_Drunk.Animate(32, 5, 1, true, false, 0);
                    }

                    // *hic*
                    m_Drunk.PublicOverheadMessage(MessageType.Regular, 0x3B2, 500849);
                }

                if (m_Drunk.BAC <= 0)
                {
                    Stop();
                    m_Table.Remove(m_Drunk);

                    m_Drunk.SendLocalizedMessage(500850); // You feel sober.
                }
            }
        }
    }
}
