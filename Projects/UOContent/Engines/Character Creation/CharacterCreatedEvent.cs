using Server.Accounting;
using Server.Network;

namespace Server.Engines.CharacterCreation;

public class CharacterCreatedEventArgs(
    NetState state, IAccount a, string name, bool female,
    int hue, byte[] stats, CityInfo city, (SkillName, byte)[] skills,
    int shirtHue, int pantsHue, int hairId, int hairHue,
    int beardId, int beardHue, int profession, Race race
)
{
    public NetState State { get; } = state;

    public IAccount Account { get; } = a;

    public Mobile Mobile { get; set; }

    public string Name { get; } = name;

    public bool Female { get; } = female;

    public int Hue { get; } = hue;

    public byte[] Stats { get; } = stats;

    public CityInfo City { get; } = city;

    public (SkillName, byte)[] Skills { get; } = skills;

    public int ShirtHue { get; } = shirtHue;

    public int PantsHue { get; } = pantsHue;

    public int HairID { get; } = hairId;

    public int HairHue { get; } = hairHue;

    public int BeardID { get; } = beardId;

    public int BeardHue { get; } = beardHue;

    public int Profession { get; set; } = profession;

    public Race Race { get; } = race;
}
