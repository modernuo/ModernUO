using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;

namespace Server.Items;

[Flippable(0x1EBA, 0x1EBB)]
[SerializationGenerator(0, false)]
public partial class TaxidermyKit : Item
{
    private static TrophyInfo[] _table =
    {
        new(typeof(BrownBear), 0x1E60, 1041093, 1041107),
        new(typeof(GreatHart), 0x1E61, 1041095, 1041109),
        new(typeof(BigFish), 0x1E62, 1041096, 1041110),
        new(typeof(Gorilla), 0x1E63, 1041091, 1041105),
        new(typeof(Orc), 0x1E64, 1041090, 1041104),
        new(typeof(PolarBear), 0x1E65, 1041094, 1041108),
        new(typeof(Troll), 0x1E66, 1041092, 1041106)
    };

    [Constructible]
    public TaxidermyKit() : base(0x1EBA) => Weight = 1.0;

    public override int LabelNumber => 1041279; // a taxidermy kit

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.Skills.Carpentry.Base < 90.0)
        {
            from.SendLocalizedMessage(1042594); // You do not understand how to use this.
        }
        else
        {
            from.SendLocalizedMessage(1042595); // Target the corpse to make a trophy out of.
            from.Target = new CorpseTarget(this);
        }
    }

    public class TrophyInfo
    {
        public TrophyInfo(Type type, int id, int deedNum, int addonNum)
        {
            CreatureType = type;
            NorthID = id;
            DeedNumber = deedNum;
            AddonNumber = addonNum;
        }

        public Type CreatureType { get; }

        public int NorthID { get; }

        public int DeedNumber { get; }

        public int AddonNumber { get; }
    }

    private class CorpseTarget : Target
    {
        private readonly TaxidermyKit _kit;

        public CorpseTarget(TaxidermyKit kit) : base(3, false, TargetFlags.None) => _kit = kit;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_kit.Deleted)
            {
                return;
            }

            if (!_kit.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (from.Skills.Carpentry.Base < 90.0)
            {
                from.SendLocalizedMessage(1042603); // You would not understand how to use the kit.
                return;
            }

            var corpse = targeted as Corpse;
            var fish = targeted as BigFish;

            if (corpse == null && fish == null)
            {
                from.SendLocalizedMessage(1042600); // That is not a corpse!
            }
            else if (corpse?.VisitedByTaxidermist == true)
            {
                from.SendLocalizedMessage(1042596); // That corpse seems to have been visited by a taxidermist already.
            }
            else
            {
                var obj = corpse?.Owner ?? targeted;
                var type = obj.GetType();

                foreach (var t in _table)
                {
                    if (t.CreatureType != type)
                    {
                        continue;
                    }

                    if (from.Backpack?.ConsumeTotal(typeof(Board), 10) != true)
                    {
                        from.SendLocalizedMessage(1042598); // You do not have enough boards.
                        return;
                    }

                    // You review the corpse and find it worthy of a trophy.
                    from.SendLocalizedMessage(1042278);
                    from.SendLocalizedMessage(1042602); // You use your kit up making the trophy.

                    Mobile hunter = fish?.Fisher;
                    var weight = (int)(fish?.Weight ?? 0);
                    fish?.Consume();

                    from.AddToBackpack(new TrophyDeed(t, hunter?.RawName, weight));

                    if (corpse != null)
                    {
                        corpse.VisitedByTaxidermist = true;
                    }

                    _kit.Delete();
                    return;
                }

                from.SendLocalizedMessage(1042599); // That does not look like something you want hanging on a wall.
            }
        }
    }
}

[SerializationGenerator(2, false)]
public partial class TrophyAddon : Item, IAddon
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _hunter;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _animalWeight;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _westId;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _northId;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _deedNumber;

    [InvalidateProperties]
    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _addonNumber;

    public TrophyAddon(
        Mobile from, int itemID, int westID, int northID, int deedNumber,
        int addonNumber, string hunter = null, int animalWeight = 0
    ) : base(itemID)
    {
        _westId = westID;
        _northId = northID;
        _deedNumber = deedNumber;
        _addonNumber = addonNumber;

        _hunter = hunter;
        _animalWeight = animalWeight;

        Movable = false;

        MoveToWorld(from.Location, from.Map);
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override int LabelNumber => _addonNumber;

    public bool CouldFit(IPoint3D p, Map map)
    {
        if (!map.CanFit(p.X, p.Y, p.Z, ItemData.Height))
        {
            return false;
        }

        if (ItemID == _northId)
        {
            return BaseAddon.IsWall(p.X, p.Y - 1, p.Z, map); // North wall
        }

        return BaseAddon.IsWall(p.X - 1, p.Y, p.Z, map); // West wall
    }

    public Item Deed => new TrophyDeed(_westId, _northId, DeedNumber, _addonNumber, _hunter, _animalWeight);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_animalWeight >= 20)
        {
            if (!string.IsNullOrWhiteSpace(_hunter))
            {
                list.Add(1070857, _hunter); // Caught by ~1_fisherman~
            }

            list.Add(1070858, _animalWeight); // ~1_weight~ stones
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _hunter = reader.ReadEntity<Mobile>()?.RawName;
        _animalWeight = reader.ReadInt();
        _westId = reader.ReadInt();
        _northId = reader.ReadInt();
        _deedNumber = reader.ReadInt();
        _addonNumber = reader.ReadInt();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        FixMovingCrate();
    }

    private void FixMovingCrate()
    {
        if (Deleted)
        {
            return;
        }

        if (Movable || IsLockedDown)
        {
            var deed = Deed;

            if (Parent is Item item)
            {
                item.AddItem(deed);
                deed.Location = Location;
            }
            else
            {
                deed.MoveToWorld(Location, Map);
            }

            Delete();
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house?.IsCoOwner(from) != true)
        {
            return;
        }

        if (from.InRange(GetWorldLocation(), 1))
        {
            from.AddToBackpack(Deed);
            Delete();
        }
        else
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
        }
    }
}

[Flippable(0x14F0, 0x14EF)]
[SerializationGenerator(2, false)]
public partial class TrophyDeed : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _hunter;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _animalWeight;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _westId;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _northId;

    [InvalidateProperties]
    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _deedNumber;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _addonNumber;

    public TrophyDeed(
        int westID, int northID, int deedNumber, int addonNumber,
        string hunter = null, int animalWeight = 0
    ) : base(0x14F0)
    {
        _westId = westID;
        _northId = northID;
        _deedNumber = deedNumber;
        _addonNumber = addonNumber;
        _hunter = hunter;
        _animalWeight = animalWeight;
    }

    public TrophyDeed(TaxidermyKit.TrophyInfo info, string hunter, int animalWeight)
        : this(info.NorthID + 7, info.NorthID, info.DeedNumber, info.AddonNumber, hunter, animalWeight)
    {
    }

    public override int LabelNumber => _deedNumber;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_animalWeight >= 20)
        {
            if (!string.IsNullOrWhiteSpace(_hunter))
            {
                list.Add(1070857, _hunter); // Caught by ~1_fisherman~
            }

            list.Add(1070858, _animalWeight); // ~1_weight~ stones
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _hunter = reader.ReadEntity<Mobile>()?.RawName;
        _animalWeight = reader.ReadInt();
        _westId = reader.ReadInt();
        _northId = reader.ReadInt();
        _deedNumber = reader.ReadInt();
        _addonNumber = reader.ReadInt();
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            var house = BaseHouse.FindHouseAt(from);

            if (house?.IsCoOwner(from) == true)
            {
                var northWall = BaseAddon.IsWall(from.X, from.Y - 1, from.Z, from.Map);
                var westWall = BaseAddon.IsWall(from.X - 1, from.Y, from.Z, from.Map);

                if (northWall && westWall)
                {
                    switch (from.Direction & Direction.Mask)
                    {
                        case Direction.North:
                        case Direction.South:
                            {
                                westWall = false;
                                break;
                            }

                        case Direction.East:
                        case Direction.West:
                            {
                                northWall = false;
                                break;
                            }

                        default:
                            {
                                from.SendMessage("Turn to face the wall on which to hang this trophy.");
                                return;
                            }
                    }
                }

                var itemID = 0;

                if (northWall)
                {
                    itemID = _northId;
                }
                else if (westWall)
                {
                    itemID = _westId;
                }
                else
                {
                    from.SendLocalizedMessage(1042626); // The trophy must be placed next to a wall.
                }

                if (itemID > 0)
                {
                    house.Addons.Add(
                        new TrophyAddon(
                            from,
                            itemID,
                            _westId,
                            _northId,
                            _deedNumber,
                            AddonNumber,
                            _hunter,
                            _animalWeight
                        )
                    );

                    Delete();
                }
            }
            else
            {
                from.SendLocalizedMessage(502092); // You must be in your house to do this.
            }
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }
}
