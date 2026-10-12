using System;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Walking over to somebody working your guild's land and telling them to move along.
///
/// <para>
/// <b>What makes the land real, and it is deliberately almost nothing.</b> The errand is a walk, a sentence
/// and a look at whether they went. No blow is struck here at any standing — striking is what a war is for,
/// and a war is declared by <see cref="BotRegard"/> out of what these errands find, never inside one of
/// them. Patrick's plan says eviction is rare and the measure that says it has gone wrong is the completion
/// band falling while evictions rise; an errand that could kill would make that measure impossible to read.
/// </para>
///
/// <para>
/// <b>The refusal is the point rather than the going.</b> A bot told to move along does not obey — nothing
/// on this shard obeys anybody — so what actually happens is that the guild learns something: they were
/// asked, and they are still there. That is what moves the opinion, and it is why the errand waits a few
/// seconds after speaking before looking again. Somebody who has wandered off in that time is not defying
/// anybody, and counting them as though they had would be a war built out of ordinary walking.
/// </para>
/// </summary>
public sealed class BotEvict : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "evict";

    /// <summary>
    /// What moving somebody along is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// <b>Three hundred and eighty, and the first hundred and twenty were a veto in a preference's
    /// clothes.</b> Measured over the first half-hour it ran: the bailiff offered eight evictions and not one
    /// was taken. At a hundred and twenty a minute this errand loses to hunting at five hundred, to crafting
    /// at three, and to nearly everything else on the shard — so the whole eviction leg of stage three never
    /// ran, and with it the <c>Defiance</c> mover that stage four was meant to be built out of. A quarrel
    /// between guilds then rests entirely on the border drift, which is a quarrel about nothing.
    /// </para>
    ///
    /// <para>
    /// Rare has to mean "the conditions rarely hold", not "it always loses" — and the conditions are already
    /// narrow: on its own land, somebody of another guild on it too, and the guild already minding them. That
    /// came to eight offers in thirty minutes across forty-nine bots. High enough to win most of those and
    /// still below the best work on the shard, so a bot with something worth doing goes on doing it. Exactly
    /// the correction <c>BotHall.Prior</c> carries a note about, for exactly the same reason.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 380.0;

    /// <summary>How long the errand takes once the bot is standing there.</summary>
    public static double WorkMinutes { get; set; } = 1.0;

    /// <summary>How near the trespasser the bot must get to say anything. Speaking distance.</summary>
    public static int Reach { get; set; } = 3;

    /// <summary>How long they are given to move off after being told, in milliseconds.</summary>
    public static int GraceMs { get; set; } = 8000;

    /// <summary>
    /// How long the whole errand may run before the word is given up on.
    ///
    /// <para>
    /// A deadline rather than a distance, and that is the right shape for this one alone. Every other errand
    /// on this shard walks at a place, so "the walk stopped closing" is a fair test; this one walks at a bot
    /// that is going about its own business, and a chase that never closes is not evidence of anything wrong
    /// — it is evidence that the other bot is faster. Ninety seconds is far longer than crossing a guild's
    /// yard and short enough that a member is not spending its afternoon following somebody about.
    /// </para>
    /// </summary>
    public static int ChaseMs { get; set; } = 90000;

    /// <summary>Trespassers who left after being told.</summary>
    public static long Moved { get; private set; }

    /// <summary>Trespassers who were still there. See <c>BotRegard.Defied</c>.</summary>
    public static long Stayed { get; private set; }

    /// <summary>Errands that never got to say anything at all.</summary>
    public static long Missed { get; private set; }

    private readonly BotMobile _them;

    private readonly string _ours;

    private long _spoke;

    private bool _said;

    private long _began;

    private bool _begun;

    public BotEvict(BotMobile them, string ours)
    {
        _them = them;
        _ours = ours;
    }

    public override string Kind => Trade;

    /// <summary>Somebody working the guild's own land who should not be. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    public override Map Map => _them?.Map;

    public override Point3D Where => _them?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    /// <summary>Not about money. A guild keeping its yard earns nothing by it and should not be judged on it.</summary>
    public override bool Unpaid => true;

    public override string Stage =>
        _said ? $"waiting to see whether {_them?.Name} moves" : $"to {_them?.Name}, who is working our land";

    /// <summary>
    /// Nothing to bend to: it is one named bot, and if the way to it is closed the errand is over. But the member is not
    /// sent after the same one again for a while, because an unpaid call learns nothing from failing. See
    /// <see cref="BotBailiff.ShunMs"/>.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        // <b>Once the word is said, or the trespasser has gone, the walk has nothing left to decide.</b> The follow begun
        // on the way over keeps running after the member has spoken, and when the trespasser walks off — which is what it
        // was told to do — the follow gives up and ended the errand "could not get nearer": 8 of the 9 evictions that
        // failed between 02:55 and 03:23 on 24.09.2026 were "waiting to see whether X moves — could not get nearer to X".
        // The errand's own clock ends it at the next beat, moved or stayed.
        if (_said || _them is not { Deleted: false } || !BotLand.Trespassing(_them, out var whose) || whose != _ours)
        {
            return true;
        }

        BotBailiff.Unreached(bot?.Self, _them);

        return false;
    }

    public override void Drop(IBotWilful bot) => BotBailiff.Release(_them);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _them is not { Deleted: false } || _them.Map == null || _them.Map == Map.Internal)
        {
            Missed++;

            return BotDoing.Failed("whoever it was is gone");
        }

        BotBailiff.Hold(_them);

        if (!_begun)
        {
            _begun = true;
            _began = Core.TickCount;
        }

        // They left of their own accord before anybody got there, which is the commonest and best ending.
        if (!BotLand.Trespassing(_them, out var whose) || whose != _ours)
        {
            if (!_said)
            {
                // The land is clear, which is what the errand was for; counted apart as before, and not as a failure.
                Missed++;

                return BotDoing.Done($"{_them.Name} had already left our land");
            }

            Moved++;

            return BotDoing.Done($"{_them.Name} moved off the land of {_ours}");
        }

        if (!_said)
        {
            if (Core.TickCount - (_began + ChaseMs) >= 0)
            {
                Missed++;

                return BotDoing.Failed($"never got within speaking distance of {_them.Name}");
            }

            if (!body.InRange(_them.Location, Reach))
            {
                // Followed rather than aimed at: a bot at work moves about its work, and a walk order to the
                // tile it stood on a moment ago is a walk order that is replaced every beat. See BotPeddle,
                // where that cost an afternoon of silent errands.
                return BotDoing.Walk(_them.Map, _them, BotArrival.Within(Reach), $"over to {_them.Name}");
            }

            BotVoice.Aloud(body, $"This is the land of {_ours}. Move along.");
            _said = true;
            _spoke = Core.TickCount;

            return BotDoing.Work($"told {_them.Name} to move along");
        }

        if (Core.TickCount - (_spoke + GraceMs) < 0)
        {
            return BotDoing.Work("waiting to see whether they go");
        }

        // Still here, and they were asked. That is the fact the guild's opinion is made of — once. See BotBailiff.ToldMs.
        Stayed++;
        BotRegard.Defied(_them.Guild?.Name, _ours);
        BotBailiff.Told(_them);

        return BotDoing.Done($"{_them.Name} was told to leave the land of {_ours} and stayed");
    }

    public static string Describe() =>
        Moved + Stayed + Missed == 0
            ? "nobody has been moved along"
            : $"{Moved} moved along when told, {Stayed} stayed and were minded, {Missed} were gone before anybody arrived";

    public static void Forget()
    {
        Moved = 0;
        Stayed = 0;
        Missed = 0;
    }
}
