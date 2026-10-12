using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Every class, built once, looked up by name.
///
/// A registry rather than an enum, because a class is a couple of dozen numbers and a kit, and an enum
/// would only be a label pointing at a switch statement somewhere else. Everything that wants to know
/// what a healer is asks here.
///
/// <para>
/// Names are the key everywhere — configuration, logs, the summary — and they are stable strings
/// rather than positions in a list. The first version learned the general form of this lesson about
/// cities: the thing you identify something by must not be able to move.
/// </para>
/// </summary>
public static class BotClasses
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClasses));

    private static readonly BotClass[] _all =
    [
        new BotWarrior(),
        new BotCaptain(),
        new BotBaron(),
        new BotArchitect(),
        new BotSage(),
        new BotWarriorMage(),
        new BotWarriorArcher(),
        new BotArcher(),
        new BotBrawler(),
        new BotMage(),
        new BotHealer(),
        new BotCrafter(),
        new BotGatherer(),
        new BotTamer(),
        new BotTrader()
    ];

    private static readonly Dictionary<string, BotClass> _byName =
        new(_all.Length, StringComparer.OrdinalIgnoreCase);

    static BotClasses()
    {
        for (var i = 0; i < _all.Length; i++)
        {
            // Numbers are filled in here rather than in a constructor, so that the same call can put
            // them back later. See BotClass.Defaults.
            _all[i].Reset();

            _byName[_all[i].Name] = _all[i];

            if (_all[i].Casts)
            {
                Casting++;
            }
        }
    }

    /// <summary>Every class, in the order they are meant to be read.</summary>
    public static IReadOnlyList<BotClass> All => _all;

    /// <summary>
    /// How many classes cast at all, which is deliberately not the same as how many are casters. The
    /// warrior-mage and the healer both throw spells and neither fills the caster's place in a group.
    /// </summary>
    public static int Casting { get; private set; }

    /// <summary>
    /// The class of that name, or null.
    ///
    /// Null rather than a fallback on purpose. A configuration file naming a class that does not exist
    /// is a typo, and quietly substituting a warrior would produce a population that is wrong in a way
    /// nobody can see — which is how the first version ended up with smiths that had no hammer.
    /// </summary>
    public static BotClass Find(string name) =>
        name != null && _byName.TryGetValue(name, out var found) ? found : null;

    /// <summary>How many classes fill this role. For musters, and for reading the config back.</summary>
    public static int Count(BotRole role)
    {
        var count = 0;

        for (var i = 0; i < _all.Length; i++)
        {
            if (_all[i].Role == role)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Applies whatever <c>bots.json</c> had to say about the classes.
    ///
    /// Code carries the defaults and configuration moves them; nothing here is required to be present.
    /// The reason this exists at all is practical rather than architectural: this shard is designed on
    /// one machine and built on another, so a number that needs a compiler to change is a number that
    /// needs a person to change. Balance passes should not need a person.
    ///
    /// Structure is deliberately not overridable — which weapons a class may roll, what tools it gets,
    /// what is in its book. Those are what the class <em>is</em>, they cannot be expressed as a number,
    /// and a config file that could empty a smith's tool list would be a config file that can produce
    /// the first version's central defect on purpose.
    /// </summary>
    public static void Override(IReadOnlyDictionary<string, BotClassOverride> overrides)
    {
        if (overrides == null)
        {
            return;
        }

        // Back to the code's own numbers first, so that this is the same operation whether it is the
        // first time or the fifth. Applying on top of a previous pass would accumulate: a potion limit
        // lifted by a config that is later corrected would stay lifted, because nothing removes a key.
        //
        // The instances themselves are kept — bots hold references to them, so replacing them would
        // leave a living population pointing at classes nobody is configuring any more.
        for (var i = 0; i < _all.Length; i++)
        {
            _all[i].Reset();
        }

        var applied = 0;

        foreach (var (name, change) in overrides)
        {
            var found = Find(name);

            if (found == null)
            {
                logger.Warning("Configuration overrides an unknown bot class {Class}; ignored", name);
                continue;
            }

            change.ApplyTo(found);
            applied++;
        }

        if (applied > 0)
        {
            logger.Information("Configuration moved numbers on {Count} bot classes", applied);
        }
    }
}

/// <summary>
/// What configuration is allowed to say about one class. Every member is optional; absent means "leave
/// the number the code chose".
/// </summary>
public sealed class BotClassOverride
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotClassOverride));

    public int? Str { get; set; }

    public int? Dex { get; set; }

    public int? Int { get; set; }

    /// <summary>
    /// Skill targets, by skill name. Replaces the class's list outright rather than merging into it: a
    /// half-stated build is harder to reason about than a fully stated one, and the whole list is four
    /// or five lines.
    /// </summary>
    public Dictionary<string, double> SkillTargets { get; set; }

    /// <summary>Potion families this class may carry more than one of. Merged, not replaced.</summary>
    public Dictionary<string, int> PotionLimits { get; set; }

    public bool? NeedsMeditation { get; set; }

    public bool? HandsAlwaysFree { get; set; }

    public int? IntrinsicManaTrickle { get; set; }

    public int? StaffManaTrickle { get; set; }

    public double? CritChancePerSkill { get; set; }

    public int? CritMultiplier { get; set; }

    public int? FreeCraftIntervalMs { get; set; }

    public int? ForageIntervalMs { get; set; }

    public int? ForageYieldMin { get; set; }

    public int? ForageYieldMax { get; set; }

    public int? BrewIntervalMs { get; set; }

    /// <summary>Gold the crown keeps this class at. Nought for everybody who lives on what it earns.</summary>
    public int? Stipend { get; set; }

    internal void ApplyTo(BotClass target)
    {
        target.Str = Str ?? target.Str;
        target.Dex = Dex ?? target.Dex;
        target.Int = Int ?? target.Int;
        target.NeedsMeditation = NeedsMeditation ?? target.NeedsMeditation;
        target.HandsAlwaysFree = HandsAlwaysFree ?? target.HandsAlwaysFree;
        target.IntrinsicManaTrickle = IntrinsicManaTrickle ?? target.IntrinsicManaTrickle;
        target.StaffManaTrickle = StaffManaTrickle ?? target.StaffManaTrickle;
        target.CritChancePerSkill = CritChancePerSkill ?? target.CritChancePerSkill;
        target.CritMultiplier = CritMultiplier ?? target.CritMultiplier;
        target.FreeCraftIntervalMs = FreeCraftIntervalMs ?? target.FreeCraftIntervalMs;
        target.ForageIntervalMs = ForageIntervalMs ?? target.ForageIntervalMs;
        target.ForageYieldMin = ForageYieldMin ?? target.ForageYieldMin;
        target.ForageYieldMax = ForageYieldMax ?? target.ForageYieldMax;
        target.BrewIntervalMs = BrewIntervalMs ?? target.BrewIntervalMs;
        target.Stipend = Stipend ?? target.Stipend;

        if (SkillTargets is { Count: > 0 })
        {
            List<(SkillName Skill, double Target)> resolved = new(SkillTargets.Count);

            foreach (var (name, value) in SkillTargets)
            {
                if (Enum.TryParse<SkillName>(name, true, out var skill))
                {
                    resolved.Add((skill, value));
                    continue;
                }

                logger.Warning(
                    "Configuration names an unknown skill {Skill} for class {Class}; ignored",
                    name,
                    target.Name
                );
            }

            if (resolved.Count > 0)
            {
                target.Skills = resolved;
            }
        }

        if (PotionLimits is not { Count: > 0 })
        {
            return;
        }

        foreach (var (name, limit) in PotionLimits)
        {
            if (Enum.TryParse<BotPotionKind>(name, true, out var kind))
            {
                target.PotionLimits[kind] = limit;
                continue;
            }

            logger.Warning(
                "Configuration names an unknown potion kind {Kind} for class {Class}; ignored",
                name,
                target.Name
            );
        }
    }
}
