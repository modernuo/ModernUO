using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// <b>A hurt bot a healer is walking to stops and waits for it (29.09.2026, build 321; Patrick's order of the morning: "full
/// mutual aid").</b>
///
/// <para>
/// A mending of another was the one errand whose goal walked away from it. Of the fifteen mendings on build 318, five ended
/// "could not get nearer to Nerys at (1439, 1365)", "to Keyleth", "to Norna": the patient was about its own work, walking
/// at the healer's own pace, and the walk that follows a moving body gives up after so many plans without getting nearer.
/// The healer was right to go and the patient was right to walk; nobody told the patient. Now the healer asks, once per
/// mending: a patient that is free — out of any fight, holding nothing it may not put down — is pressed to wait where it
/// stands, its work put down to be taken up again, and ends the wait when it is whole, when the healer has gone elsewhere,
/// when something comes at it, or after <see cref="MostMs"/>.
/// </para>
/// </summary>
public sealed class BotAwaitMend : BotDeed
{
    public const string Trade = "await";

    /// <summary>The longest a patient stands for its healer.</summary>
    public static int MostMs { get; set; } = 60000;

    /// <summary>Whether a healer walking to a patient asks it to wait.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>A patient nearer than this is not asked: the healer is there already.</summary>
    public static int Nearer { get; set; } = 4;

    /// <summary>Patients asked to wait; pressed; ended whole, the healer gone, a fight, the minute out.</summary>
    public static long Asked { get; private set; }

    public static long Pressed { get; private set; }

    public static long Whole { get; private set; }

    public static long Forsaken { get; private set; }

    public static long Fought { get; private set; }

    public static long Outwaited { get; private set; }

    private readonly Mobile _healer;

    private readonly Map _map;

    private readonly Point3D _where;

    private long _began;

    private bool _stopped;

    private BotAwaitMend(Mobile healer, Mobile patient)
    {
        _healer = healer;
        _map = patient.Map;
        _where = patient.Location;
        _began = Core.TickCount;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => 1.0;

    public override double Minutes => MostMs / 60000.0;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    public override int Made => 0;

    public override bool Unpaid => true;

    /// <summary>A minute at most: kept from the next review's bidding, which would end it for any errand at all.</summary>
    public override bool Committed => true;

    public override bool Still => true;

    public override string Stage => $"waiting for {_healer?.Name} to mend it";

    /// <summary>
    /// Asked by a healer walking to a patient it is not beside: the patient is pressed to wait when it is free to. Once per
    /// mending (the caller keeps that); returns whether the patient now waits.
    /// </summary>
    public static bool Ask(Mobile healer, Mobile patient)
    {
        if (!Running || healer == null || patient is not BotMobile bot || patient == healer)
        {
            return false;
        }

        // <b>Counted before it is judged, and war mode is not a fight (build 335).</b> Asked only after a filter that refused
        // any patient in war mode, this read "0 patients of 0 asked" through two sessions while thirteen menders on 333 "could
        // not get to X and is trying elsewhere" (`night-r6-rescue.md`): a hurt bot is nearly always in war mode, fighting or not.
        // Whether it is fighting is asked below, by its combatant.
        Asked++;

        // <b>A patient within Nearer that is walking is asked all the same (01.10.2026, build 388).</b> The walk to a patient gives
        // up after thirteen plans without getting nearer, and on 387 that was 58 "could not get one tile closer … (to <patient>)"
        // in 95 minutes — Liftrasa to Uldreth four times running at (1390, 1262), (1390, 1250), (1386, 1263), (1379, 1275) — the
        // healer two or three tiles off a patient walking its own town errand at the healer's own pace, and never asked to stop
        // because it stood "nearer than four". Nearer is for a patient standing still, which the healer reaches on the next step.
        var walking = bot is { Journey.Moving: true };

        if (bot is not { Deleted: false, Alive: true, Hidden: false } || bot.Map != healer.Map || (!walking && bot.InRange(healer.Location, Nearer)))
        {
            return false;
        }

        var near = walking && bot.InRange(healer.Location, Nearer);

        if (bot.Combatant != null || bot.Squad != null || BotMend.Embattled(bot))
        {
            return false;
        }

        var resolve = bot.Resolve;

        if (resolve == null || resolve.Paused != null)
        {
            return false;
        }

        if (resolve.Deed is { } deed
            && (deed is BotAwaitMend || deed is BotSalve || !deed.Resumes || deed.Committed || deed.Foe != null || deed.Braves))
        {
            return false;
        }

        if (!BotWill.Press(bot, new BotAwaitMend(healer, bot), $"{healer.Name} is coming to mend it"))
        {
            return false;
        }

        Pressed++;

        if (near)
        {
            PressedNear++;
        }

        return true;
    }

    /// <summary>Of <see cref="Pressed"/>, patients within <see cref="Nearer"/> of their healer that were walking away from it. See <see cref="Ask"/>.</summary>
    public static long PressedNear { get; private set; }

    public override void Taken(IBotWilful bot) => _began = Core.TickCount;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (BotMend.Whole(body))
        {
            Whole++;

            return BotDoing.Done($"mended by {_healer?.Name}");
        }

        if (body.Combatant != null || BotMend.Embattled(body))
        {
            Fought++;

            return BotDoing.Done("something came at it while it waited");
        }

        if (_healer is not { Deleted: false, Alive: true } || _healer.Map != body.Map
            || (_healer as IBotWilful)?.Resolve?.Deed is not BotSalve)
        {
            Forsaken++;

            return BotDoing.Done($"{_healer?.Name} went elsewhere");
        }

        if (Core.TickCount - _began >= MostMs)
        {
            Outwaited++;

            return BotDoing.Done($"waited {MostMs / 1000}s for {_healer.Name}");
        }

        // A pressed bot keeps walking unless the walk is finished (build 314): once, on the first beat.
        if (!_stopped)
        {
            _stopped = true;
            (body as BotMobile)?.Journey?.Finish();
        }

        return BotDoing.Work($"waiting for {_healer.Name}");
    }

    public static string Describe() =>
        $"{Pressed} patients of {Asked} asked stood still for their healer ({PressedNear} of them walking within {Nearer} tiles of it; {Whole} whole, {Forsaken} left when the healer went elsewhere, {Fought} broke off for a fight, {Outwaited} gave up after {MostMs / 1000}s)";
}
