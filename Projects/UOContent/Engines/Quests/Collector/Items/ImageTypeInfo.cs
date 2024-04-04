using System;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector;

public enum ImageType
{
    Betrayer,
    Bogling,
    BogThing,
    Gazer,
    Beetle,
    GiantBlackWidow,
    Scorpion,
    JukaMage,
    JukaWarrior,
    Lich,
    MeerMage,
    MeerWarrior,
    Mongbat,
    Mummy,
    Pixie,
    PlagueBeast,
    SandVortex,
    StoneGargoyle,
    SwampDragon,
    Wisp,
    Juggernaut
}

public class ImageTypeInfo
{
    private static readonly ImageTypeInfo[] _table =
    [
        new ImageTypeInfo(9734, typeof(Betrayer), ImageType.Betrayer, 75, 45),
        new ImageTypeInfo(9735, typeof(Bogling), ImageType.Bogling, 75, 45),
        new ImageTypeInfo(9736, typeof(BogThing), ImageType.BogThing, 60, 47),
        new ImageTypeInfo(9615, typeof(Gazer), ImageType.Gazer, 75, 45),
        new ImageTypeInfo(9743, typeof(Beetle), ImageType.Beetle, 60, 55),
        new ImageTypeInfo(9667, typeof(GiantBlackWidow), ImageType.GiantBlackWidow, 55, 52),
        new ImageTypeInfo(9657, typeof(Scorpion), ImageType.Scorpion, 65, 47),
        new ImageTypeInfo(9758, typeof(JukaMage), ImageType.JukaMage, 75, 45),
        new ImageTypeInfo(9759, typeof(JukaWarrior), ImageType.JukaWarrior, 75, 45),
        new ImageTypeInfo(9636, typeof(Lich), ImageType.Lich, 75, 45),
        new ImageTypeInfo(9756, typeof(MeerMage), ImageType.MeerMage, 75, 45),
        new ImageTypeInfo(9757, typeof(MeerWarrior), ImageType.MeerWarrior, 75, 45),
        new ImageTypeInfo(9638, typeof(Mongbat), ImageType.Mongbat, 70, 50),
        new ImageTypeInfo(9639, typeof(Mummy), ImageType.Mummy, 75, 45),
        new ImageTypeInfo(9654, typeof(Pixie), ImageType.Pixie, 75, 45),
        new ImageTypeInfo(9747, typeof(PlagueBeast), ImageType.PlagueBeast, 60, 45),
        new ImageTypeInfo(9750, typeof(SandVortex), ImageType.SandVortex, 60, 43),
        new ImageTypeInfo(9614, typeof(StoneGargoyle), ImageType.StoneGargoyle, 75, 45),
        new ImageTypeInfo(9753, typeof(SwampDragon), ImageType.SwampDragon, 50, 55),
        new ImageTypeInfo(8448, typeof(Wisp), ImageType.Wisp, 75, 45),
        new ImageTypeInfo(9746, typeof(Juggernaut), ImageType.Juggernaut, 55, 38)
    ];

    // Used for sampling
    private static readonly ImageTypeInfo[] _shuffleTable = (ImageTypeInfo[])_table.Clone();

    public ImageTypeInfo(int figurine, Type type, ImageType image, int x, int y)
    {
        Figurine = figurine;
        Image = image;
        Type = type;
        X = x;
        Y = y;
    }

    public int Figurine { get; }

    public ImageType Image { get; }

    public Type Type { get; }

    public int Name => Figurine < 0x4000 ? 1020000 + Figurine : 1078872 + Figurine;
    public int X { get; }
    public int Y { get; }

    public static ImageTypeInfo Get(ImageType image)
    {
        var index = (int)image;
        return _table[index >= 0 && index < _table.Length ? index : 0];
    }

    public static ImageType[] RandomList(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<ImageType>();
        }

        _shuffleTable.Shuffle();
        var imageTypes = new ImageType[count];

        var minCount = Math.Min(count, _shuffleTable.Length);
        for (var i = 0; i < minCount; i++)
        {
            imageTypes[i] = _shuffleTable[i].Image;
        }

        return imageTypes;
    }
}
