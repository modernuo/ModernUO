using ModernUO.Serialization;
using Server.Multis;
using Server.Prompts;
using Server.Regions;

namespace Server.Items;

[Flippable(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
[SerializationGenerator(2, false)]
public partial class RecallRune : Item
{
    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    private string _description;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    private Point3D _target;

    [Constructible]
    public RecallRune() : base(0x1F14)
    {
        Weight = 1.0;
        CalculateHue();
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public BaseHouse House
    {
        get
        {
            if (_house?.Deleted == true)
            {
                House = null;
            }

            return _house;
        }
        set
        {
            _house = value;
            CalculateHue();
            InvalidateProperties();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public bool Marked
    {
        get => _marked;
        set
        {
            if (_marked != value)
            {
                _marked = value;
                CalculateHue();
                InvalidateProperties();
            }
        }
    }

    [SerializableProperty(4)]
    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public Map TargetMap
    {
        get => _targetMap;
        set
        {
            if (_targetMap != value)
            {
                _targetMap = value;
                CalculateHue();
                InvalidateProperties();
            }
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        switch (version)
        {
            case 1:
                {
                    _house = reader.ReadEntity<BaseHouse>();
                    goto case 0;
                }
            case 0:
                {
                    _description = reader.ReadString();
                    _marked = reader.ReadBool();
                    Target = reader.ReadPoint3D();
                    _targetMap = reader.ReadMap();

                    CalculateHue();
                    break;
                }
        }
    }

    private void CalculateHue()
    {
        if (!_marked)
        {
            Hue = 0;
        }
        else if (_targetMap == Map.Trammel)
        {
            Hue = House != null ? 0x47F : 50;
        }
        else if (_targetMap == Map.Felucca)
        {
            Hue = House != null ? 0x66D : 0;
        }
        else if (_targetMap == Map.Ilshenar)
        {
            Hue = House != null ? 0x55F : 1102;
        }
        else if (_targetMap == Map.Malas)
        {
            Hue = House != null ? 0x55F : 1102;
        }
        else if (_targetMap == Map.Tokuno)
        {
            Hue = House != null ? 0x47F : 1154;
        }
    }

    public void Mark(Mobile m)
    {
        _marked = true;

        var setDesc = false;
        if (Core.AOS)
        {
            _house = BaseHouse.FindHouseAt(m);

            if (_house == null)
            {
                Target = m.Location;
                _targetMap = m.Map;
            }
            else
            {
                var sign = _house.Sign;

                _description = (sign?.Name?.Trim()).DefaultIfNullOrEmpty("an unnamed house");

                setDesc = true;

                var x = _house.BanLocation.X;
                var y = _house.BanLocation.Y + 2;
                var z = _house.BanLocation.Z;

                var map = _house.Map;

                if (map?.CanFit(x, y, z, 16, false, false) == false)
                {
                    z = map.GetAverageZ(x, y);
                }

                Target = new Point3D(x, y, z);
                _targetMap = map;
            }
        }
        else
        {
            _house = null;
            Target = m.Location;
            _targetMap = m.Map;
        }

        if (!setDesc)
        {
            _description = BaseRegion.GetRuneNameFor(Region.Find(Target, _targetMap));
        }

        CalculateHue();
        InvalidateProperties();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_marked)
        {
            string desc;

            if ((desc = _description) == null || (desc = desc.Trim()).Length == 0)
            {
                desc = "an unknown location";
            }

            if (_targetMap == Map.Tokuno)
            {
                list.Add(House != null ? 1063260 : 1063259, $"a recall rune for {desc}"); // ~1_val~ (Tokuno Islands)[(House)]
            }
            else if (_targetMap == Map.Malas)
            {
                list.Add(House != null ? 1062454 : 1060804, $"a recall rune for {desc}"); // ~1_val~ (Malas)[(House)]
            }
            else if (_targetMap == Map.Felucca)
            {
                list.Add(House != null ? 1062452 : 1060805, $"a recall rune for {desc}"); // ~1_val~ (Felucca)[(House)]
            }
            else if (_targetMap == Map.Trammel)
            {
                list.Add(House != null ? 1062453 : 1060806, $"a recall rune for {desc}"); // ~1_val~ (Trammel)[(House)]
            }
            else if (House != null)
            {
                list.Add($"a recall rune for {desc} ({_targetMap})(House)");
            }
            else
            {
                list.Add($"a recall rune for {desc} ({_targetMap})");
            }
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_marked)
        {
            var desc = (_description?.Trim()).DefaultIfNullOrEmpty("an unknown location");

            if (_targetMap == Map.Tokuno)
            {
                LabelTo(
                    from,
                    House != null ? 1063260 : 1063259, // ~1_val~ (Tokuno Islands)[(House)]
                    $"a recall rune for {desc}"
                );
            }
            else if (_targetMap == Map.Malas)
            {
                LabelTo(
                    from,
                    House != null ? 1062454 : 1060804, // ~1_val~ (Malas)[(House)]
                    $"a recall rune for {desc}"
                );
            }
            else if (_targetMap == Map.Felucca)
            {
                LabelTo(
                    from,
                    House != null ? 1062452 : 1060805, // ~1_val~ (Felucca)[(House)]
                    $"a recall rune for {desc}"
                );
            }
            else if (_targetMap == Map.Trammel)
            {
                LabelTo(
                    from,
                    House != null ? 1062453 : 1060806, // ~1_val~ (Trammel)[(House)]
                    $"a recall rune for {desc}"
                );
            }
            else
            {
                if (House != null)
                {
                    LabelTo(from, $"a recall rune for {desc} ({_targetMap})(House)");
                }
                else
                {
                    LabelTo(from, $"a recall rune for {desc} ({_targetMap})");
                }
            }
        }
        else
        {
            LabelTo(from, "an unmarked recall rune");
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        int number;

        if (!IsChildOf(from.Backpack))
        {
            number = 1042001; // That must be in your pack for you to use it.
        }
        else if (House != null)
        {
            number = 1062399; // You cannot edit the description for this rune.
        }
        else if (_marked)
        {
            number = 501804; // Please enter a description for this marked object.

            from.Prompt = new RenamePrompt(this);
        }
        else
        {
            number = 501805; // That rune is not yet marked.
        }

        from.SendLocalizedMessage(number);
    }

    private class RenamePrompt : Prompt
    {
        private RecallRune _rune;

        public RenamePrompt(RecallRune rune) => _rune = rune;

        public override void OnResponse(Mobile from, string text)
        {
            if (_rune.House == null && _rune.Marked)
            {
                _rune.Description = text;
                from.SendLocalizedMessage(1010474); // The etching on the rune has been changed.
            }
        }
    }
}
