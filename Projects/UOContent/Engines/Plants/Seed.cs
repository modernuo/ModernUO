using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Engines.Plants;

[SerializationGenerator(3, false)]
public partial class Seed : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlantType _plantType;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _showType;

    [Constructible]
    public Seed() : this(PlantTypeInfo.RandomFirstGeneration(), PlantHueInfo.RandomFirstGeneration())
    {
    }

    [Constructible]
    public Seed(PlantType plantType, PlantHue plantHue, bool showType = false) : base(0xDCF)
    {
        Weight = 1.0;
        Stackable = Core.SA;

        _plantType = plantType;
        _plantHue = plantHue;
        _showType = showType;

        Hue = PlantHueInfo.GetInfo(plantHue).Hue;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    [SerializableProperty(1)]
    public PlantHue PlantHue
    {
        get => _plantHue;
        set
        {
            _plantHue = value;
            Hue = PlantHueInfo.GetInfo(value).Hue;
            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override int LabelNumber => 1060810; // seed

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public static Seed RandomBonsaiSeed() => RandomBonsaiSeed(0.5);

    public static Seed RandomBonsaiSeed(double increaseRatio) => new(
        PlantTypeInfo.RandomBonsai(increaseRatio),
        PlantHue.Plain
    );

    public static Seed RandomPeculiarSeed(int group)
    {
        return group switch
        {
            1 => new Seed(PlantTypeInfo.RandomPeculiarGroupOne(), PlantHue.Plain),
            2 => new Seed(PlantTypeInfo.RandomPeculiarGroupTwo(), PlantHue.Plain),
            3 => new Seed(PlantTypeInfo.RandomPeculiarGroupThree(), PlantHue.Plain),
            _ => new Seed(PlantTypeInfo.RandomPeculiarGroupFour(), PlantHue.Plain)
        };
    }

    private int GetLabel(out string args)
    {
        var typeInfo = PlantTypeInfo.GetInfo(_plantType);
        var hueInfo = PlantHueInfo.GetInfo(_plantHue);

        int title;

        if (_showType || typeInfo.PlantCategory == PlantCategory.Default)
        {
            title = hueInfo.Name;
        }
        else
        {
            title = (int)typeInfo.PlantCategory;
        }

        if (Amount == 1)
        {
            if (_showType)
            {
                args = $"#{title}\t#{typeInfo.Name}";
                return typeInfo.GetSeedLabel(hueInfo);
            }

            args = $"#{title}";
            return hueInfo.IsBright() ? 1060839 : 1060838; // [bright] ~1_val~ seed
        }

        if (_showType)
        {
            args = $"{Amount}\t#{title}\t#{typeInfo.Name}";
            return typeInfo.GetSeedLabelPlural(hueInfo);
        }

        args = $"{Amount}\t#{title}";
        return hueInfo.IsBright() ? 1113491 : 1113490; // ~1_amount~ [bright] ~2_val~ seeds
    }

    public override void AddNameProperty(IPropertyList list)
    {
        var typeInfo = PlantTypeInfo.GetInfo(_plantType);
        var hueInfo = PlantHueInfo.GetInfo(_plantHue);

        int title;

        if (_showType || typeInfo.PlantCategory == PlantCategory.Default)
        {
            title = hueInfo.Name;
        }
        else
        {
            title = (int)typeInfo.PlantCategory;
        }

        if (Amount == 1)
        {
            if (_showType)
            {
                list.Add(typeInfo.GetSeedLabel(hueInfo), $"{title:#}\t{typeInfo.Name:#}");
                return;
            }

            list.Add(hueInfo.IsBright() ? 1060839 : 1060838, $"{title:#}");
            return;
        }

        if (_showType)
        {
            list.Add(typeInfo.GetSeedLabelPlural(hueInfo), $"{Amount}\t{title:#}\t{typeInfo.Name:#}");
            return;
        }

        list.Add(hueInfo.IsBright() ? 1113491 : 1113490, $"{Amount}\t{title:#}");
    }

    public override void OnSingleClick(Mobile from)
    {
        LabelTo(from, GetLabel(out var args), args);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            return;
        }

        from.Target = new InternalTarget(this);
        LabelTo(from, 1061916); // Choose a bowl of dirt to plant this seed in.
    }

    public override bool StackWith(Mobile from, Item dropped, bool playSound) =>
        dropped is Seed other && other.PlantType == _plantType && other.PlantHue == _plantHue &&
        other.ShowType == _showType && base.StackWith(from, other, playSound);

    public override void OnAfterDuped(Item newItem)
    {
        if (newItem is not Seed newSeed)
        {
            return;
        }

        newSeed.PlantType = _plantType;
        newSeed.PlantHue = _plantHue;
        newSeed.ShowType = _showType;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _plantType = (PlantType)reader.ReadInt();
        _plantHue = (PlantHue)reader.ReadInt();
        _showType = reader.ReadBool();
    }

    private class InternalTarget : Target
    {
        private readonly Seed _seed;

        public InternalTarget(Seed seed) : base(-1, false, TargetFlags.None)
        {
            _seed = seed;
            CheckLOS = false;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_seed.Deleted)
            {
                return;
            }

            if (!_seed.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                return;
            }

            if (targeted is PlantItem plant)
            {
                plant.PlantSeed(from, _seed);
            }
            else if (targeted is Item item)
            {
                item.LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
            }
            else
            {
                from.SendLocalizedMessage(1061919); // You must use a seed on a bowl of dirt!
            }
        }
    }
}
