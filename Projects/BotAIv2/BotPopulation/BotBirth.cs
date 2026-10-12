using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Engines.Harvest;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// A newborn's temperament: the same birth points as its class deals, leaned towards one skill of the class's own.
///
/// <para>
/// <b>Patrick's point 7 of 01.10.2026:</b> "all the bots start at the same level now — add some randomness". Swordsmen with
/// more Swordsmanship and fewer points in the rest, gatherers
/// who love digging with more Mining, crafters with the most in Blacksmithy, Fletching and the other producing trades. Until
/// this, <c>BotMobile.Learn</c> gave every bot of a class the same fifty, thirty and twenty and <c>LearnWeapon</c> the weapon at
/// fifty, so a class was one bot many times over — and its crafters were born at Blacksmithy 30, thirty-five points under the
/// first coloured metal the engine lets a smith work (dull copper, 65 on the base: <c>CraftItem</c> line 647). On build 384 the
/// island dug 622 coloured ingots that no crafter could touch, and the live crafters stood at Blacksmithy 36 to 49 after a day.
/// </para>
///
/// <para>
/// <b>Nothing is added; the birth's own points move.</b> A leaning (<see cref="BotLeaning"/>, declared by the class) raises one
/// skill to a value drawn inside its band; the points come out of the other skills the bot was born with, in proportion to what
/// each can spare. A skill spares down to <see cref="KeepShare"/> of its birth value, or to the floor the leaning names for it,
/// and the skill the class is judged by (<see cref="BotClass.MainSkill"/>) spares nothing unless the leaning names a floor for it
/// — a healer's Healing, an archer's Archery and a mage's Magery are never lowered. Within a hundred per skill and the class's own
/// target; the total is the class's own, far under the era's 700.
/// </para>
///
/// <para>
/// <b>Seeded by the bot's name and class, so a birth is reproducible.</b> FNV-1a over the two names into a SplitMix64 stream: the
/// same name raised into the same class leans the same way on every shard and every run (the engine's <c>Utility.Random</c>
/// would not, and <c>string.GetHashCode</c> is randomised per process). The seasoned classes are left as they are: they are born
/// at their share of their targets for their offices' sake (<see cref="BotClass.Seasoned"/>).
/// </para>
/// </summary>
public static class BotBirth
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotBirth));

    /// <summary>Whether newborns lean at all. Off, every bot is born as its class deals it, exactly as before 01.10.2026.</summary>
    public static bool Leans { get; set; } = true;

    /// <summary>
    /// The share of its birth value a skill keeps when it pays for a leaning, unless the leaning names its own floor. A half: a
    /// smith's blade goes from fifty to twenty-five at the most, which is what the crafter class says its blade is for ("it carries a
    /// blade because everything on this island does"). Raised to 0.6 a smith still reaches 68; from 0.65 the smith's band can no
    /// longer be paid for in full and births start counting as short of the first coloured metal (<see cref="Short"/>).
    /// </summary>
    public static double KeepShare { get; set; } = 0.5;

    /// <summary>Bots raised this session through a class's deal (the seasoned classes are not counted).</summary>
    public static long Born { get; private set; }

    /// <summary>Of those, the ones that leaned.</summary>
    public static long Leaned { get; private set; }

    /// <summary>Of those, the ones born even: the class's own deal.</summary>
    public static long Even { get; private set; }

    /// <summary>Leanings whose other skills could not pay for the whole draw, which leaned less than they drew.</summary>
    public static long Trimmed { get; private set; }

    /// <summary>Smiths born at or above the first coloured metal's floor: the first stint holding a coloured bar makes coloured armour.</summary>
    public static long Coloured { get; private set; }

    /// <summary>Smiths born below it. Nought unless the class's skills or <see cref="KeepShare"/> were changed under the leaning.</summary>
    public static long Short { get; private set; }

    /// <summary>Points moved by leanings this session, in tenths, for the census.</summary>
    private static long _moved;

    /// <summary>Leanings born this session, by class and by the word for the leaning.</summary>
    private static readonly Dictionary<string, Dictionary<string, int>> _census = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The skills a birth dealt: the class's own and the weapon the roll handed over, once each. Reused; births are rare.</summary>
    private static readonly List<Dealt> _dealt = [];

    private sealed class Dealt
    {
        public SkillName Skill;

        public int Plain;

        public int Target;

        public int Spare;

        public int Gives;
    }

    /// <summary>
    /// Leans a bot that has just been dealt its class's skills and its weapon. Called by <c>BotMobile.Become</c> after
    /// <c>LearnWeapon</c> and before <c>BotProgress.Restore</c>, which only ever raises what this leaves.
    /// </summary>
    public static void Lean(BotMobile bot, BotClass klass)
    {
        if (bot == null || klass == null || klass.Seasoned)
        {
            return;
        }

        Born++;

        var leanings = klass.Leanings;

        if (!Leans || leanings == null || leanings.Count == 0)
        {
            Tally(klass.Name, "as dealt");
            Even++;

            return;
        }

        var draw = new Draw(Seed(bot.Name, klass.Name));
        var leaning = Pick(leanings, ref draw);
        var weapon = bot.Bond?.Weapon;
        SkillName? leant = leaning.Weapon ? weapon?.Skill : leaning.Skill;

        if (leaning.IsEven || leant is not { } signature)
        {
            Tally(klass.Name, "even");
            Even++;

            logger.Information("{Name} the {Class} was born even, as its class deals it", bot.Name, klass.Name);

            return;
        }

        Deal(bot, klass, weapon);

        var sig = Find(signature);

        if (sig == null)
        {
            // A leaning towards a skill the class never dealt: born at nought in it, aiming at a hundred.
            sig = new Dealt { Skill = signature, Plain = bot.Skills[signature].BaseFixedPoint, Target = 1000 };
            _dealt.Add(sig);
        }

        var cap = Math.Min(Math.Min(1000, sig.Target), bot.Skills[signature].CapFixedPoint);
        var (low, high) = Band(leaning);
        var lowT = Math.Min((int)Math.Round(low * 10.0), cap);
        var highT = Math.Max(lowT, Math.Min((int)Math.Round(high * 10.0), cap));

        // Whole points: a tenth of a point is not a temperament.
        var drawn = Math.Min(cap, lowT + draw.Below((highT - lowT) / 10 + 1) * 10);
        var want = drawn - sig.Plain;
        var spare = 0;

        for (var i = 0; i < _dealt.Count; i++)
        {
            var dealt = _dealt[i];

            dealt.Gives = 0;
            dealt.Spare = 0;

            if (ReferenceEquals(dealt, sig) || dealt.Plain <= 0)
            {
                continue;
            }

            var floor = leaning.Floor(dealt.Skill) is { } named
                ? (int)Math.Round(named * 10.0)
                : dealt.Skill == klass.MainSkill
                    ? dealt.Plain
                    : (int)Math.Ceiling(dealt.Plain * Math.Clamp(KeepShare, 0.0, 1.0));

            dealt.Spare = Math.Max(0, dealt.Plain - Math.Clamp(floor, 0, dealt.Plain));
            spare += dealt.Spare;
        }

        var take = Math.Clamp(want, 0, spare);

        if (take < want)
        {
            Trimmed++;
        }

        // In proportion to what each can spare, the remainder a tenth at a time to whoever still can.
        var left = take;

        for (var i = 0; i < _dealt.Count && spare > 0; i++)
        {
            var dealt = _dealt[i];
            var gives = (int)(take * (long)dealt.Spare / spare);

            dealt.Gives = gives;
            left -= gives;
        }

        for (var pass = 0; left > 0 && pass < 1000; pass++)
        {
            for (var i = 0; i < _dealt.Count && left > 0; i++)
            {
                var dealt = _dealt[i];

                if (dealt.Gives < dealt.Spare)
                {
                    dealt.Gives++;
                    left--;
                }
            }
        }

        take -= left;

        for (var i = 0; i < _dealt.Count; i++)
        {
            var dealt = _dealt[i];

            if (dealt.Gives > 0)
            {
                bot.Skills[dealt.Skill].BaseFixedPoint = dealt.Plain - dealt.Gives;
            }
        }

        if (take > 0)
        {
            bot.Skills[signature].BaseFixedPoint = sig.Plain + take;
        }

        var word = Word(leaning, signature);

        Leaned++;
        _moved += take;
        Tally(klass.Name, word);

        if (leaning.Tier == BotTier.Metal)
        {
            if (bot.Skills[signature].Base >= FirstRung(BotTier.Metal, leaning.Low))
            {
                Coloured++;
            }
            else
            {
                Short++;
            }
        }

        Say(bot, klass, word, sig, take < want);
    }

    /// <summary>The birth line: what moved, from what to what.</summary>
    private static void Say(BotMobile bot, BotClass klass, string word, Dealt sig, bool trimmed)
    {
        using var said = ValueStringBuilder.Create(192);

        said.Append(bot.Skills[sig.Skill].Name);
        said.Append(' ');
        said.Append(sig.Plain / 10.0, "F0");
        said.Append(" to ");
        said.Append(bot.Skills[sig.Skill].Base, "F0");

        for (var i = 0; i < _dealt.Count; i++)
        {
            var dealt = _dealt[i];

            if (dealt.Gives <= 0)
            {
                continue;
            }

            said.Append(", ");
            said.Append(bot.Skills[dealt.Skill].Name);
            said.Append(' ');
            said.Append(dealt.Plain / 10.0, "F1");
            said.Append(" to ");
            said.Append(bot.Skills[dealt.Skill].Base, "F1");
        }

        if (trimmed)
        {
            said.Append(" (less than it drew: the rest could spare no more)");
        }

        logger.Information(
            "{Name} the {Class} was born leaning {Leaning}: {Skills}",
            bot.Name,
            klass.Name,
            word,
            said.ToString()
        );
    }

    /// <summary>The skills this birth dealt, read back off the bot: the class's list and the rolled weapon, once each.</summary>
    private static void Deal(BotMobile bot, BotClass klass, BotWeaponOption? weapon)
    {
        _dealt.Clear();

        var wanted = klass.Skills;

        for (var i = 0; i < wanted.Count; i++)
        {
            var (skill, target) = wanted[i];

            if (Find(skill) == null && bot.Skills[skill] != null)
            {
                _dealt.Add(new Dealt { Skill = skill, Plain = bot.Skills[skill].BaseFixedPoint, Target = (int)Math.Round(target * 10.0) });
            }
        }

        if (weapon is { } option && bot.Skills[option.Skill] != null)
        {
            var known = Find(option.Skill);
            var target = (int)Math.Round(option.Target * 10.0);

            if (known == null)
            {
                _dealt.Add(new Dealt { Skill = option.Skill, Plain = bot.Skills[option.Skill].BaseFixedPoint, Target = target });
            }
            else
            {
                known.Plain = bot.Skills[option.Skill].BaseFixedPoint;
                known.Target = Math.Max(known.Target, target);
            }
        }
    }

    private static Dealt Find(SkillName skill)
    {
        for (var i = 0; i < _dealt.Count; i++)
        {
            if (_dealt[i].Skill == skill)
            {
                return _dealt[i];
            }
        }

        return null;
    }

    /// <summary>One of the class's leanings, by weight.</summary>
    private static BotLeaning Pick(IReadOnlyList<BotLeaning> leanings, ref Draw draw)
    {
        var total = 0;

        for (var i = 0; i < leanings.Count; i++)
        {
            total += Math.Max(0, leanings[i].Weight);
        }

        if (total <= 0)
        {
            return BotLeaning.Even(1);
        }

        var roll = draw.Below(total);

        for (var i = 0; i < leanings.Count; i++)
        {
            var weight = Math.Max(0, leanings[i].Weight);

            if (roll < weight)
            {
                return leanings[i];
            }

            roll -= weight;
        }

        return leanings[^1];
    }

    /// <summary>The band a leaning draws in: its own two numbers, or its ladder's first coloured rung with the same span.</summary>
    public static (double Low, double High) Band(BotLeaning leaning)
    {
        if (leaning.Tier == BotTier.None)
        {
            return (leaning.Low, Math.Max(leaning.Low, leaning.High));
        }

        var rung = FirstRung(leaning.Tier, leaning.Low);

        return (rung, rung + Math.Max(0.0, leaning.High - leaning.Low));
    }

    /// <summary>
    /// The skill the first material above the plain one asks for, on the engine's own ladder; the fallback when the engine has not
    /// built the system yet. Dull copper, spined leather and the dull copper vein all stand at sixty-five in this era.
    /// </summary>
    public static double FirstRung(BotTier tier, double fallback)
    {
        var least = double.MaxValue;

        switch (tier)
        {
            case BotTier.Metal:
                {
                    Rungs(DefBlacksmithy.CraftSystem, ref least);

                    break;
                }
            case BotTier.Leather:
                {
                    Rungs(DefTailoring.CraftSystem, ref least);

                    break;
                }
            case BotTier.Ore:
                {
                    var veins = Mining.System?.OreAndStone?.Resources;

                    if (veins != null)
                    {
                        for (var i = 0; i < veins.Length; i++)
                        {
                            var asks = veins[i]?.ReqSkill ?? 0.0;

                            if (asks > 0.0 && asks < least)
                            {
                                least = asks;
                            }
                        }
                    }

                    break;
                }
        }

        return least == double.MaxValue ? fallback : least;
    }

    private static void Rungs(CraftSystem system, ref double least)
    {
        var rungs = system?.CraftSubRes;

        if (rungs == null)
        {
            return;
        }

        for (var i = 0; i < rungs.Count; i++)
        {
            var asks = rungs.GetAt(i)?.RequiredSkill ?? 0.0;

            if (asks > 0.0 && asks < least)
            {
                least = asks;
            }
        }
    }

    /// <summary>The word for a leaning: the weapon's own for a blade ("swordsman", "maceman", "fencer"), else the leaning's name.</summary>
    private static string Word(BotLeaning leaning, SkillName signature) =>
        !leaning.Weapon
            ? leaning.Name
            : signature switch
            {
                SkillName.Swords  => "swordsman",
                SkillName.Macing  => "maceman",
                SkillName.Fencing => "fencer",
                _                 => leaning.Name
            };

    private static void Tally(string calling, string word)
    {
        calling ??= "?";

        if (!_census.TryGetValue(calling, out var words))
        {
            words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _census[calling] = words;
        }

        words[word] = words.GetValueOrDefault(word) + 1;
    }

    /// <summary>
    /// The seed of a birth: FNV-1a over the name and the class. Stable across processes and runs, which <c>string.GetHashCode</c>
    /// is not.
    /// </summary>
    public static ulong Seed(string name, string calling)
    {
        var hash = 14695981039346656037UL;

        hash = Fold(hash, name);
        hash = (hash ^ 0x1F) * 1099511628211UL;

        return Fold(hash, calling);
    }

    private static ulong Fold(ulong hash, string text)
    {
        if (text == null)
        {
            return hash;
        }

        for (var i = 0; i < text.Length; i++)
        {
            hash = (hash ^ text[i]) * 1099511628211UL;
        }

        return hash;
    }

    /// <summary>SplitMix64: a small, fixed generator, so the same seed draws the same numbers on every build of .NET.</summary>
    private struct Draw
    {
        private ulong _state;

        public Draw(ulong seed) => _state = seed;

        private ulong Next()
        {
            _state += 0x9E3779B97F4A7C15UL;

            var z = _state;

            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;

            return z ^ (z >> 31);
        }

        /// <summary>A whole number from nought to one under <paramref name="n"/>.</summary>
        public int Below(int n) => n <= 1 ? 0 : (int)(Next() % (ulong)n);
    }

    /// <summary>
    /// The census for the <c>Rest:</c> line: births this session, how many leaned, how many smiths were born able to work a
    /// coloured bar, and every class's leanings by count.
    /// </summary>
    public static string Describe()
    {
        if (Born == 0)
        {
            return "no bot born from a class's deal this session (a restart keeps the bots it had, and how they were born)";
        }

        using var line = ValueStringBuilder.Create(512);

        line.Append(
            $"{Born} born this session from a class's deal: {Leaned} leaned ({_moved / 10.0:F0} points moved), {Even} even, {Trimmed} leaned less than they drew; "
            + $"{Coloured} smiths born at or above the first coloured metal ({FirstRung(BotTier.Metal, 65.0):F0}), {Short} below it; by class"
        );

        List<KeyValuePair<string, Dictionary<string, int>>> classes = [.. _census];

        classes.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));

        for (var i = 0; i < classes.Count; i++)
        {
            var (calling, words) = classes[i];
            List<KeyValuePair<string, int>> ordered = [.. words];

            ordered.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));

            line.Append(i == 0 ? " " : "; ");
            line.Append(calling);
            line.Append(':');

            for (var j = 0; j < ordered.Count; j++)
            {
                line.Append(j == 0 ? " " : ", ");
                line.Append(ordered[j].Key);
                line.Append(' ');
                line.Append(ordered[j].Value);
            }
        }

        return line.ToString();
    }
}
