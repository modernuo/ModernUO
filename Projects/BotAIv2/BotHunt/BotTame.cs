using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.BotAI.V2;

/// <summary>
/// The beasts a bot has tamed: kept beside it, set on whatever it fights, and fed.
///
/// <para>
/// <b>Patrick's third addition of the night of 29.09.2026: "the bots have knowledge of taming domestic and wild animals,
/// and the animals help them in a fight."</b> The engine already knows everything about a tamed creature — it follows
/// its master, it attacks what it is told to, it forgets a master that never feeds it — and a player drives that with
/// four words: "all follow me", "all kill". This is those words, said by the bot's beat.
/// </para>
///
/// <para>
/// <b>The list is the bot's, and it is found again rather than saved.</b> A tamed creature is saved by the engine with
/// its master; at the first beat after a boot the bot looks round itself for creatures that answer to it
/// (<see cref="Adopt"/>), and from then on the list is kept as beasts are tamed and die. A horse is not a beast here —
/// the stable's business — nor anything summoned.
/// </para>
///
/// <para>
/// <b>Fed by a word, not by meat.</b> The engine's loyalty falls on its own clock and a beast at nought goes wild. A
/// tamer with meat in its pack would feed it; a tamer without would lose it in an hour, which the shard's population
/// cannot afford to watch. So every <see cref="FeedMs"/> a bot's beasts are made loyal again — the ration is counted
/// (<see cref="Fed"/>), and what it costs is nothing, which is the one lie in this file.
/// </para>
/// </summary>
public static class BotPets
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPets));

    /// <summary>Whether beasts are driven at all. Off, a tamed beast follows and does nothing else.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The most beasts one bot keeps.</summary>
    public static int Most { get; set; } = 2;

    /// <summary>How far round itself a bot looks for its beasts after a boot.</summary>
    public static int AdoptReach { get; set; } = 24;

    /// <summary>How often a bot's beasts are fed.</summary>
    public static int FeedMs { get; set; } = 600000;

    /// <summary>How far a beast may be from its master before it is called back rather than set on a foe.</summary>
    public static int Leash { get; set; } = 18;

    public static long Tamed { get; private set; }

    public static long Adopted { get; private set; }

    public static long Lost { get; private set; }

    public static long SetOn { get; private set; }

    public static long Fed { get; private set; }

    /// <summary>The beasts of every bot, by its serial; a list is made when the first beast is tamed or adopted.</summary>
    private static readonly Dictionary<Serial, List<BaseCreature>> _beasts = [];

    private static readonly Dictionary<Serial, long> _adoptedTick = [];

    private static readonly Dictionary<Serial, long> _fedTick = [];

    public static IReadOnlyList<BaseCreature> Of(Mobile bot) =>
        bot != null && _beasts.TryGetValue(bot.Serial, out var list) ? list : [];

    public static int Count(Mobile bot) => Of(bot).Count;

    /// <summary>Whether this creature is the sort a bot tames as a beast: not a mount, not summoned, tamable.</summary>
    public static bool Beastly(BaseCreature creature) =>
        creature is { Deleted: false, Tamable: true, Summoned: false } && creature is not BaseMount;

    public static void Add(Mobile bot, BaseCreature beast)
    {
        if (bot == null || beast == null)
        {
            return;
        }

        if (!_beasts.TryGetValue(bot.Serial, out var list))
        {
            list = [];
            _beasts[bot.Serial] = list;
        }

        if (!list.Contains(beast))
        {
            list.Add(beast);
        }
    }

    /// <summary>The bot's beat: its beasts found after a boot, set on what it fights, called back, fed. A dictionary lookup for a bot with none.</summary>
    public static void Keep(BotMobile bot)
    {
        if (!Running || bot is not { Deleted: false, Alive: true } || bot.Map == null || bot.Map == Map.Internal)
        {
            return;
        }

        var now = Core.TickCount;

        if (!_adoptedTick.ContainsKey(bot.Serial))
        {
            _adoptedTick[bot.Serial] = now;
            Adopt(bot);
        }

        if (!_beasts.TryGetValue(bot.Serial, out var list) || list.Count == 0)
        {
            return;
        }

        var feed = !_fedTick.TryGetValue(bot.Serial, out var fed) || now - fed >= FeedMs;

        if (feed)
        {
            _fedTick[bot.Serial] = now;
        }

        var foe = bot.Combatant is { Deleted: false, Alive: true } target && target.Map == bot.Map ? target : null;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            var beast = list[i];

            if (beast is not { Deleted: false, Alive: true } || beast.ControlMaster != bot || beast.Map != bot.Map)
            {
                list.RemoveAt(i);
                Lost++;

                continue;
            }

            if (feed)
            {
                beast.Loyalty = BaseCreature.MaxLoyalty;
                Fed++;
            }

            var apart = Math.Max(Math.Abs(beast.X - bot.X), Math.Abs(beast.Y - bot.Y));

            if (foe != null && apart <= Leash)
            {
                if (beast.ControlOrder != OrderType.Attack || beast.ControlTarget != foe)
                {
                    beast.ControlTarget = foe;
                    beast.ControlOrder = OrderType.Attack;
                    SetOn++;
                }
            }
            else if (beast.ControlOrder != OrderType.Follow || beast.ControlTarget != bot)
            {
                beast.ControlTarget = bot;
                beast.ControlOrder = OrderType.Follow;
            }
        }
    }

    /// <summary>The creatures round a bot that already answer to it, after a boot.</summary>
    private static void Adopt(BotMobile bot)
    {
        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, AdoptReach))
        {
            if (creature is { Deleted: false, Alive: true, Controlled: true } && creature.ControlMaster == bot && Beastly(creature))
            {
                Add(bot, creature);
                Adopted++;
            }
        }
    }

    /// <summary>A beast tamed just now. See <see cref="BotTame"/>.</summary>
    public static void Took(BotMobile bot, BaseCreature beast)
    {
        Add(bot, beast);
        Tamed++;
    }

    public static string Describe() =>
        $"{Tamed} beasts tamed and {Adopted} found again after a boot, {Lost} lost; {SetOn} times set on a foe, {Fed} rations; {BotTamerProposer.Describe()}";

    public static void Forget()
    {
        _beasts.Clear();
        _adoptedTick.Clear();
        _fedTick.Clear();
        Tamed = 0;
        Adopted = 0;
        Lost = 0;
        SetOn = 0;
        Fed = 0;
    }
}

/// <summary>
/// Bringing one beast to heel: walk up, try, try again, and the engine's own arithmetic decides.
///
/// <para>
/// The roll is the engine's — <c>CheckTargetSkill</c> on Animal Taming against the creature's own difficulty, the same
/// window a player gets (see <c>SkillHandlers.AnimalTaming</c>) — so the skill trains as a player's would, and what a
/// bot can tame is exactly what its skill says. A beast that has had owners before is harder, as it is for a player.
/// </para>
/// </summary>
public sealed class BotTame : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTame));

    public const string Trade = "tame";

    /// <summary>What taming is worth a minute. Sixty: a lesson's worth, since the beast pays in every fight after.</summary>
    public static double Prior { get; set; } = 60.0;

    /// <summary>Seconds between tries, as the engine paces a player.</summary>
    public static int TryMs { get; set; } = 4000;

    public static int MostTries { get; set; } = 6;

    public static long Tried { get; private set; }

    public static long Won { get; private set; }

    public static long Refused { get; private set; }

    private readonly Map _map;

    private readonly BaseCreature _beast;

    private long _triedTick;

    private int _tries;

    public BotTame(Map map, BaseCreature beast)
    {
        _map = map;
        _beast = beast;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _beast?.Location ?? Point3D.Zero;

    public override Mobile Foe => null;

    public override double Expects => Prior;

    public override double Minutes => 1.5;

    public override SkillName? Trains => SkillName.AnimalTaming;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    public override string Stage => _tries == 0 ? $"walking up to {_beast?.Name}" : $"taming {_beast?.Name} ({_tries} tries)";

    public override void Taken(IBotWilful bot) => Tried++;

    /// <summary>
    /// The walk to the beast found no way: the beast is left alone by every tamer for a while, and this tamer rests its taming
    /// as after a failed one.
    ///
    /// <para>
    /// <b>The same alligator, twelve times (01.10.2026, build 386).</b> "No way through to an alligator at (6041, 1491) in Misc
    /// Dungeons" ended twelve of the twenty tamings of build 386 — Katra, six within two seconds at 13:56:46 and six more at
    /// 14:01:48 — because the ending marked nothing and the next look found the same beast within reach again. The nearest
    /// beast through a wall is the nearest beast on the next beat too. See <see cref="BotTamerProposer.Unreachable"/>.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        BotTamerProposer.Unreachable(_beast);
        BotTamerProposer.Failed(bot?.Self);
        Unreached++;

        return false;
    }

    /// <summary>Beasts the walk found no way to, shunned for <see cref="BotTamerProposer.UnreachableMs"/>. See <see cref="Bend"/>.</summary>
    public static long Unreached { get; private set; }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body is not BotMobile tamer || _map == null || _beast is not { Deleted: false, Alive: true } || _beast.Map != _map)
        {
            return BotDoing.Failed("the beast is gone");
        }

        if (_beast.Controlled)
        {
            return BotDoing.Failed($"{_beast.Name} answers to somebody already");
        }

        if (!body.InRange(_beast.Location, 2))
        {
            return BotDoing.Walk(_map, _beast, BotArrival.Within(2), $"up to {_beast.Name}");
        }

        var now = Core.TickCount;

        if (now - _triedTick < TryMs)
        {
            return BotDoing.Work($"taming {_beast.Name}");
        }

        _triedTick = now;
        _tries++;

        // The engine's own window: MinTameSkill, six more for every owner before, 50 % at the middle of it.
        var minSkill = _beast.MinTameSkill + _beast.Owners.Count * 6.0 + 24.9;

        if (AnimalTaming.CheckMastery(body, _beast) || body.CheckTargetSkill(SkillName.AnimalTaming, _beast, minSkill - 25.0, minSkill + 25.0))
        {
            if (_beast.Owners.Count == 0)
            {
                AnimalTaming.ScaleSkills(_beast, 0.90);

                if (_beast.StatLossAfterTame)
                {
                    AnimalTaming.ScaleStats(_beast, 0.50);
                }
            }

            _beast.Owners.Add(body);
            _beast.SetControlMaster(body);
            _beast.IsBonded = false;
            _beast.ControlTarget = body;
            _beast.ControlOrder = OrderType.Follow;

            BotPets.Took(tamer, _beast);
            Won++;

            logger.Information(
                "{Name} the {Class} has tamed {Beast} (Animal Taming {Skill:F1} against {Min:F1}, {Tries} tries) and keeps {Count} beasts now",
                body.Name,
                tamer.Class?.Name,
                _beast.Name,
                body.Skills.AnimalTaming.Base,
                _beast.MinTameSkill,
                _tries,
                BotPets.Count(body)
            );

            return BotDoing.Done($"{_beast.Name} answers to it now, after {_tries} tries");
        }

        body.CheckTargetSkill(SkillName.AnimalLore, _beast, 0.0, 120.0);

        if (_tries >= MostTries)
        {
            Refused++;
            BotTamerProposer.Failed(body);

            return BotDoing.Failed($"{_beast.Name} would not be tamed in {MostTries} tries (Animal Taming {body.Skills.AnimalTaming.Base:F1} against {_beast.MinTameSkill:F1})");
        }

        return BotDoing.Work($"taming {_beast.Name}");
    }

    public static string Describe() => $"{Tried} tamings tried, {Won} won, {Refused} given up, {Unreached} beasts found no way to and shunned";

    public static void Forget()
    {
        Tried = 0;
        Won = 0;
        Refused = 0;
        Unreached = 0;
    }
}

/// <summary>Offers a bot whose class knows taming the nearest beast its skill allows, while it keeps fewer than <see cref="BotPets.Most"/>.</summary>
public sealed class BotTamerProposer : IBotProposer
{
    /// <summary>How far round itself a bot looks for a beast to tame.</summary>
    public static int Reach { get; set; } = 14;

    /// <summary>The least Animal Taming a bot needs before a beast is offered at all.</summary>
    public static double LeastSkill { get; set; } = 20.0;

    /// <summary>The least a beast must be worth in a fight to be tamed: a hundred keeps the rats, the dogs and the hawks of the town out of it (Imogen's first two were a dog and a rat, 02:38 on 29.09.2026).</summary>
    public static double LeastPower { get; set; } = 100.0;

    /// <summary>How long one bot waits after a taming that failed before another beast is offered.</summary>
    public static int RetryMs { get; set; } = 300000;

    /// <summary>How long a beast the walk found no way to is offered to nobody. See <see cref="Unreachable"/> (build 386).</summary>
    public static int UnreachableMs { get; set; } = 1800000;

    private static readonly Dictionary<Serial, long> _unreachable = [];

    /// <summary>A beast no tamer could walk up to: left alone by every tamer for <see cref="UnreachableMs"/>. See <see cref="BotTame.Bend"/>.</summary>
    public static void Unreachable(Mobile beast)
    {
        if (beast == null || UnreachableMs <= 0)
        {
            return;
        }

        if (_unreachable.Count > 1024)
        {
            _unreachable.Clear();
        }

        _unreachable[beast.Serial] = Core.TickCount + UnreachableMs;
    }

    private static bool Shunned(Mobile beast) =>
        _unreachable.TryGetValue(beast.Serial, out var until) && Core.TickCount - until < 0;

    public static long Asked { get; private set; }

    public static long Kept { get; private set; }

    public static long NoBeast { get; private set; }

    public static long Offered { get; private set; }

    private static readonly Dictionary<Serial, long> _failed = [];

    private static bool _said;

    public string Name => "Tamer";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (!BotPets.Running || map == null || map == Map.Internal || !body.Alive || body is not BotMobile who)
        {
            return null;
        }

        if (who.Class?.Skills == null || !Wants(who.Class) || body.Skills.AnimalTaming.Base < LeastSkill)
        {
            return null;
        }

        Asked++;

        if (BotPets.Count(body) >= BotPets.Most)
        {
            Kept++;

            return null;
        }

        if (_failed.TryGetValue(who.Serial, out var when) && Core.TickCount - (when + RetryMs) < 0)
        {
            return null;
        }

        var skill = body.Skills.AnimalTaming.Base;
        BaseCreature best = null;
        var bestWorth = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(body.Location, Reach))
        {
            if (!BotPets.Beastly(creature) || creature.Controlled || !creature.Alive || creature.MinTameSkill > skill + 5.0)
            {
                continue;
            }

            // A beast behind a wall, lately found so. See Unreachable.
            if (Shunned(creature))
            {
                continue;
            }

            if (BotThreat.Power(creature) < LeastPower)
            {
                continue;
            }

            if (body.Followers + creature.ControlSlots > body.FollowersMax)
            {
                continue;
            }

            // The strongest it can hope to tame: a wolf before a rabbit.
            var worth = BotThreat.Power(creature) / (1.0 + Math.Max(0.0, creature.MinTameSkill - skill));

            if (worth > bestWorth)
            {
                bestWorth = worth;
                best = creature;
            }
        }

        if (best == null)
        {
            NoBeast++;

            return null;
        }

        Offered++;

        if (!_said)
        {
            _said = true;

            LogFactory.GetLogger(typeof(BotTamerProposer)).Information(
                "{Name} the {Class} is the first offered a beast to tame: {Beast} (wants {Min:F1}, has {Skill:F1})",
                body.Name,
                who.Class?.Name,
                best.Name,
                best.MinTameSkill,
                skill
            );
        }

        return new BotTame(map, best);
    }

    /// <summary>A taming failed: this bot is left alone a while. Called by the will's ending of the deed through BotTame? No: by the deed's own failure path via the retry map here.</summary>
    public static void Failed(Mobile bot)
    {
        if (bot != null)
        {
            _failed[bot.Serial] = Core.TickCount;
        }
    }

    private static bool Wants(BotClass klass)
    {
        var skills = klass.Skills;

        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i].Skill == SkillName.AnimalTaming)
            {
                return true;
            }
        }

        return false;
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody who knows taming has been asked"
            : $"{Asked} asked to tame: {Offered} offered a beast, {Kept} keeping their {BotPets.Most} already, {NoBeast} with no beast within {Reach} their skill allows; {BotTame.Describe()}";

    public static void Forget()
    {
        _failed.Clear();
        _unreachable.Clear();
        _said = false;
        Asked = 0;
        Kept = 0;
        NoBeast = 0;
        Offered = 0;
        BotTame.Forget();
    }
}
