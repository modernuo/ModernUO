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
    private static readonly ImageTypeInfo[] m_Table =
    {
        new(9734, typeof(Betrayer), 75, 45),
        new(9735, typeof(Bogling), 75, 45),
        new(9736, typeof(BogThing), 60, 47),
        new(9615, typeof(Gazer), 75, 45),
        new(9743, typeof(Beetle), 60, 55),
        new(9667, typeof(GiantBlackWidow), 55, 52),
        new(9657, typeof(Scorpion), 65, 47),
        new(9758, typeof(JukaMage), 75, 45),
        new(9759, typeof(JukaWarrior), 75, 45),
        new(9636, typeof(Lich), 75, 45),
        new(9756, typeof(MeerMage), 75, 45),
        new(9757, typeof(MeerWarrior), 75, 45),
        new(9638, typeof(Mongbat), 70, 50),
        new(9639, typeof(Mummy), 75, 45),
        new(9654, typeof(Pixie), 75, 45),
        new(9747, typeof(PlagueBeast), 60, 45),
        new(9750, typeof(SandVortex), 60, 43),
        new(9614, typeof(StoneGargoyle), 75, 45),
        new(9753, typeof(SwampDragon), 50, 55),
        new(8448, typeof(Wisp), 75, 45),
        new(9746, typeof(Juggernaut), 55, 38)
    };

    public ImageTypeInfo(int figurine, Type type, int x, int y)
    {
        Figurine = figurine;
        Type = type;
        X = x;
        Y = y;
    }

    public int Figurine { get; }

    public Type Type { get; }

    public int Name => Figurine < 0x4000 ? 1020000 + Figurine : 1078872 + Figurine;
    public int X { get; }
    public int Y { get; }

    public static ImageTypeInfo Get(ImageType image)
    {
        var index = (int)image;
        return m_Table[index >= 0 && index < m_Table.Length ? index : 0];
    }

    public static ImageType[] RandomList(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<ImageType>();
        }

        var length = m_Table.Length;
        Span<bool> list = stackalloc bool[length];
        list.Clear();

        var imageTypes = new ImageType[count];

        var i = 0;
        do
        {
            var rand = Utility.Random(length);
            if (!(list[rand] && (list[rand] = true)))
            {
                imageTypes[i++] = (ImageType)rand;
            }
        } while (i < count);

        return imageTypes;
    }
}
