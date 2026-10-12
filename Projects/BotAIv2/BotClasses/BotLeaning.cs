using System;

namespace Server.BotAI.V2;

/// <summary>
/// Which ladder of materials a leaning's band is read off, when the engine has one for its trade.
///
/// <para>
/// <b>Read rather than copied (01.10.2026, Patrick's point 7).</b> A smith born to work coloured metal has to be born at the
/// first coloured metal's floor, and that floor is the engine's — <c>DefBlacksmithy</c>'s sub-resources, sixty-five for dull
/// copper in this era. Writing sixty-five here would be the second copy of a number this assembly has paid for every time it
/// kept one. So a leaning names the ladder, and <see cref="BotBirth"/> reads where its first coloured rung stands.
/// </para>
/// </summary>
public enum BotTier
{
    /// <summary>No ladder: the band is the leaning's own two numbers.</summary>
    None,

    /// <summary>The smith's metals: <c>DefBlacksmithy.CraftSubRes</c>, dull copper upwards.</summary>
    Metal,

    /// <summary>The tailor's hides: <c>DefTailoring.CraftSubRes</c>, spined upwards.</summary>
    Leather,

    /// <summary>The miner's veins: <c>Mining.OreAndStone</c>, the first vein that asks the miner anything.</summary>
    Ore
}

/// <summary>
/// One way a bot of a class may be born: the skill it leans towards, how often a newborn of the class leans that way, and how
/// far.
///
/// <para>
/// <b>Patrick's point 7 of 01.10.2026, in his words translated:</b> "all the bots start at the same level now — add some
/// randomness: some may start with more Swordsmanship if they are swordsmen but put fewer points into other talents; gatherers
/// who love digging put more into Mining, to dig more and better ore faster; crafters put the most into Blacksmithy, Fletching
/// and the other producing trades, to give the market what it asks for sooner." Until then every bot of a class was born the
/// same fifty, thirty and twenty (<c>BotMobile.Learn</c>) and the weapon at fifty, so the forty-nine bots of one class were one
/// bot forty-nine times.
/// </para>
///
/// <para>
/// <b>A leaning spends no point the class did not already give.</b> The birth's own points are redistributed: the leaning's
/// skill is raised to a value drawn inside <see cref="Low"/>..<see cref="High"/>, and what that costs is taken from the other
/// skills the bot was born with, in proportion to what each can spare — down to half its birth value
/// (<see cref="BotBirth.KeepShare"/>), to the floor this leaning names for it (<see cref="Keeps"/>), and never below its birth
/// value for the skill the class is judged by (<see cref="BotClass.MainSkill"/>: a healer's Healing, an archer's Archery, a
/// mage's Magery). A bot whose others cannot pay for the whole draw leans less, and is counted.
/// </para>
/// </summary>
/// <param name="Name">The word for the bot that leans this way, for the birth line and the census ("smith", "digger").</param>
/// <param name="Skill">The skill leaned into. Null with <paramref name="Weapon"/> unset is an even birth: the class's own deal.</param>
/// <param name="Weight">How many newborns in the class's total weight lean this way.</param>
/// <param name="Low">The least the leaned skill is born at.</param>
/// <param name="High">The most. Never above the class's own target for the skill, nor a hundred.</param>
/// <param name="Weapon">Leans into whatever the birth roll handed it (<c>BotBond.Weapon</c>) rather than a named skill.</param>
/// <param name="Tier">
/// A ladder of materials to read the band off: <paramref name="Low"/> becomes its first coloured rung and the span
/// (<paramref name="High"/> − <paramref name="Low"/>) is kept. The two numbers stand when the engine cannot be read.
/// </param>
/// <param name="Keeps">Floors this leaning holds other skills at, over the share rule and the main-skill rule both.</param>
public readonly record struct BotLeaning(
    string Name,
    SkillName? Skill,
    int Weight,
    double Low = 0.0,
    double High = 0.0,
    bool Weapon = false,
    BotTier Tier = BotTier.None,
    (SkillName Skill, double Floor)[] Keeps = null
)
{
    /// <summary>An even birth: the class's own fifty, thirty, twenty and the weapon at fifty, untouched.</summary>
    public static BotLeaning Even(int weight) => new("even", null, weight);

    /// <summary>Whether this leaning moves no point at all.</summary>
    public bool IsEven => Skill == null && !Weapon;

    /// <summary>The floor this leaning names for a skill, or null when it names none.</summary>
    public double? Floor(SkillName skill)
    {
        var keeps = Keeps;

        if (keeps == null)
        {
            return null;
        }

        for (var i = 0; i < keeps.Length; i++)
        {
            if (keeps[i].Skill == skill)
            {
                return Math.Max(0.0, keeps[i].Floor);
            }
        }

        return null;
    }
}
