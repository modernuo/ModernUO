using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PresetMap : MapItem
{
    private int _labelNumber;

    [Constructible]
    public PresetMap(PresetMapType type)
    {
        var v = (int)type;

        if (v >= 0 && v < PresetMapEntry.Table.Length)
        {
            InitEntry(PresetMapEntry.Table[v]);
        }
    }

    public PresetMap(PresetMapEntry entry)
    {
        InitEntry(entry);
    }

    [SerializableProperty(0, useField: nameof(_labelNumber))]
    public override int LabelNumber => _labelNumber == 0 ? base.LabelNumber : _labelNumber;

    public void InitEntry(PresetMapEntry entry)
    {
        _labelNumber = entry.Name;

        Width = entry.Width;
        Height = entry.Height;

        Bounds = entry.Bounds;
    }
}

public class PresetMapEntry
{
    public PresetMapEntry(int name, int width, int height, int xLeft, int yTop, int xRight, int yBottom)
    {
        Name = name;
        Width = width;
        Height = height;
        Bounds = new Rectangle2D(xLeft, yTop, xRight - xLeft, yBottom - yTop);
    }

    public int Name { get; }

    public int Width { get; }

    public int Height { get; }

    public Rectangle2D Bounds { get; }

    public static PresetMapEntry[] Table { get; } =
    {
        new(1041189, 200, 200, 1092, 1396, 1736, 1924), // map of Britain
        new(1041203, 200, 200, 0256, 1792, 1736, 2560), // map of Britain to Skara Brae
        new(1041192, 200, 200, 1024, 1280, 2304, 3072), // map of Britain to Trinsic
        new(1041183, 200, 200, 2500, 1900, 3000, 2400), // map of Buccaneer's Den
        new(1041198, 200, 200, 2560, 1792, 3840, 2560), // map of Buccaneer's Den to Magincia
        new(1041194, 200, 200, 2560, 1792, 3840, 3072), // map of Buccaneer's Den to Ocllo
        new(1041181, 200, 200, 1088, 3572, 1528, 4056), // map of Jhelom
        new(1041186, 200, 200, 3530, 2022, 3818, 2298), // map of Magincia
        new(1041199, 200, 200, 3328, 1792, 3840, 2304), // map of Magincia to Ocllo
        new(1041182, 200, 200, 2360, 0356, 2706, 0702), // map of Minoc
        new(1041190, 200, 200, 0000, 0256, 2304, 3072), // map of Minoc to Yew
        new(1041191, 200, 200, 2467, 0572, 2878, 0746), // map of Minoc to Vesper
        new(1041188, 200, 200, 4156, 0808, 4732, 1528), // map of Moonglow
        new(1041201, 200, 200, 3328, 0768, 4864, 1536), // map of Moonglow to Nujelm
        new(1041185, 200, 200, 3446, 1030, 3832, 1424), // map of Nujelm
        new(1041197, 200, 200, 3328, 1024, 3840, 2304), // map of Nujelm to Magincia
        new(1041187, 200, 200, 3582, 2456, 3770, 2742), // map of Ocllo
        new(1041184, 200, 200, 2714, 3329, 3100, 3639), // map of Serpent's Hold
        new(1041200, 200, 200, 2560, 2560, 3840, 3840), // map of Serpent's Hold to Ocllo
        new(1041180, 200, 200, 0524, 2064, 0960, 2452), // map of Skara Brae
        new(1041204, 200, 200, 0000, 0000, 5199, 4095), // map of The World
        new(1041177, 200, 200, 1792, 2630, 2118, 2952), // map of Trinsic
        new(1041193, 200, 200, 1792, 1792, 3072, 3072), // map of Trinsic to Buccaneer's Den
        new(1041195, 200, 200, 0256, 1792, 2304, 4095), // map of Trinsic to Jhelom
        new(1041178, 200, 200, 2636, 0592, 3064, 1012), // map of Vesper
        new(1041196, 200, 200, 2636, 0592, 3840, 1536), // map of Vesper to Nujelm
        new(1041179, 200, 200, 0236, 0741, 0766, 1269), // map of Yew
        new(1041202, 200, 200, 0000, 0512, 1792, 2048)  // map of Yew to Britain
    };
}

public enum PresetMapType
{
    Britain,
    BritainToSkaraBrae,
    BritainToTrinsic,
    BucsDen,
    BucsDenToMagincia,
    BucsDenToOcllo,
    Jhelom,
    Magincia,
    MaginciaToOcllo,
    Minoc,
    MinocToYew,
    MinocToVesper,
    Moonglow,
    MoonglowToNujelm,
    Nujelm,
    NujelmToMagincia,
    Ocllo,
    SerpentsHold,
    SerpentsHoldToOcllo,
    SkaraBrae,
    TheWorld,
    Trinsic,
    TrinsicToBucsDen,
    TrinsicToJhelom,
    Vesper,
    VesperToNujelm,
    Yew,
    YewToBritain
}
