using System;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// <b>A guard a town set is worked, not waited for (30.09.2026, build 339).</b>
///
/// <para>
/// The towns of build 332 set a guild a task and counted its progress, and nothing on the shard offered the work: a guard
/// task stood at 7 of 90 member-minutes for twenty minutes on 336 and a clearing at "0 minutes stood in it" while their
/// clocks ran down to failure (`night-r13-social.md`) — three failures in six hours and the guild would have been put out of
/// its own town. A member of a guild with a guard standing in a town within <see cref="Reach"/> tiles is offered a watch: it
/// walks inside the walls (<c>BotTowns.Town.Bounds</c>, where <c>BotTownTask</c> counts the minutes) and stands there for
/// <see cref="BotStandGuard.StandMs"/>. At most <see cref="MostAtOnce"/> of a guild at once. A clearing is left to the
/// companies — the town sets the worst ground near it, and a lone watchman sent there is a death — and a supply to the trades.
/// </para>
/// </summary>
public sealed class BotTownWatch : IBotProposer
{
    /// <summary>How far from the town a member is offered the watch.</summary>
    public static int Reach { get; set; } = 250;

    /// <summary>Members of one guild on watch in one town at once.</summary>
    public static int MostAtOnce { get; set; } = 3;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public static long Full { get; private set; }

    public string Name => "TownWatch";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile who || who.Guild is not Guild guild || !who.Alive || who.Tired)
        {
            return null;
        }

        var map = who.Map;

        if (map == null || map == Map.Internal || bot is IBotSquadMember { Squad: not null } || BotDungeon.Under(who.Location))
        {
            return null;
        }

        foreach (var standing in BotBurgh.All)
        {
            if (standing.Task is not { Kind: BotTownTask.Kinds.Guard } task || standing.Guild != guild.Name || task.Town == null
                || task.Done >= task.Need)
            {
                continue;
            }

            Asked++;

            if (task.Map != map || !Utility.InRange(who.Location, task.Town.Square, Reach)
                || !BotGates.Joined(map, who.Location, task.Town.Square))
            {
                continue;
            }

            if (BotStandGuard.Watching(guild.Name, task) >= MostAtOnce && !BotStandGuard.Holds(who, task))
            {
                Full++;

                continue;
            }

            Offered++;

            return new BotStandGuard(map, task, guild.Name);
        }

        return null;
    }

    public static string Describe() =>
        $"{Asked} looks at a town's guard for a member: {Offered} watches offered, {Full} passed over with {MostAtOnce} of the guild already on watch; {BotStandGuard.Stood} watches stood their time, {BotStandGuard.Ended} ended when the task did";
}

/// <summary>A member standing watch inside a town's walls for its guild's guard. See <see cref="BotTownWatch"/>.</summary>
public sealed class BotStandGuard : BotDeed
{
    public const string Trade = "watch";

    /// <summary>How long one watch lasts once inside the walls.</summary>
    public static int StandMs { get; set; } = 300000;

    /// <summary>What a watch is reckoned at: above a stroll, below real work.</summary>
    public static double Prior { get; set; } = 18.0;

    public static long Stood { get; private set; }

    public static long Ended { get; private set; }

    private static readonly System.Collections.Generic.Dictionary<Serial, (string Guild, BotTownTask Task, long Tick)> _on = [];

    private readonly Map _map;

    private readonly BotTownTask _task;

    private readonly string _guild;

    private bool _in;

    private long _since;

    private Mobile _body;

    public BotStandGuard(Map map, BotTownTask task, string guild)
    {
        _map = map;
        _task = task;
        _guild = guild;
    }

    /// <summary>How many of a guild are on watch for this task now.</summary>
    public static int Watching(string guild, BotTownTask task)
    {
        var count = 0;
        var now = Core.TickCount;

        foreach (var (_, on) in _on)
        {
            if (on.Guild == guild && ReferenceEquals(on.Task, task) && now - on.Tick < StandMs * 2)
            {
                count++;
            }
        }

        return count;
    }

    public static bool Holds(Mobile who, BotTownTask task) =>
        who != null && _on.TryGetValue(who.Serial, out var on) && ReferenceEquals(on.Task, task);

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _task.Town?.Square ?? _task.Target;

    public override double Expects => Prior;

    public override double Minutes => StandMs / 60000.0 + 2.0;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override bool Unpaid => true;

    public override bool Still => _in;

    /// <summary>
    /// <b>A watch inside the walls is a promise to the town (30.09.2026, build 340).</b> All five watches taken on 339's first
    /// twenty minutes were "outbid by hunt at 91/min after 2.0 of 7.5 minutes", by a cook, by another hunt — the protection
    /// ran out at two minutes and a watch reckoned at 18/min loses to any paid work — so none stood its five minutes and the
    /// guard tasks crept (Cove 19 of 90 member-minutes with 36 left). Once inside it is kept from the bidding; a flight, a
    /// mending, a rung above and whatever presses still take it (<c>BotWill</c>'s hold).
    /// </summary>
    public override bool Committed => _in;

    public override string Stage => _in ? $"standing guard in {_task.Town?.Name}" : $"on the way to stand guard in {_task.Town?.Name}";

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body)
        {
            _body = body;
            _on[body.Serial] = (_guild, _task, Core.TickCount);
        }
    }

    public override void Drop(IBotWilful bot) => Leave();

    private void Leave()
    {
        if (_body != null)
        {
            _on.Remove(_body.Serial);
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _task.Town == null)
        {
            Leave();

            return BotDoing.Failed("no body");
        }

        if (_task.Done >= _task.Need || !IsStanding())
        {
            Ended++;
            Leave();

            return BotDoing.Done($"the guard of {_task.Town.Name} is over");
        }

        var now = Core.TickCount;

        if (!_task.Town.Bounds.Contains(body.Location))
        {
            _in = false;

            return BotDoing.Walk(_map, _task.Town.Square, BotArrival.Within(8), $"to stand guard in {_task.Town.Name}");
        }

        if (!_in)
        {
            _in = true;
            _since = now;
            (body as BotMobile)?.Journey?.Finish();
        }

        if (now - _since >= StandMs)
        {
            Stood++;
            Leave();

            return BotDoing.Done($"stood guard in {_task.Town.Name} for {StandMs / 60000} minutes");
        }

        return BotDoing.Work($"standing guard in {_task.Town.Name}");
    }

    private bool IsStanding()
    {
        foreach (var standing in BotBurgh.All)
        {
            if (ReferenceEquals(standing.Task, _task))
            {
                return true;
            }
        }

        return false;
    }
}
