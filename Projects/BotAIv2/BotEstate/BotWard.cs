using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// A warden's walk to a stranger hunting the guild's land, and the sentence that makes the toll owed. See
/// <see cref="BotTollman"/> and <see cref="BotToll"/>.
///
/// <para>
/// Shaped on the bailiff's eviction (<see cref="BotEvict"/>), and for the same reasons: followed rather than aimed at,
/// because a hunter moves about its hunt; finished, not failed, when the stranger left the land before anybody arrived —
/// the land is clear, which is what the walk was for; and once the word is said nothing is left to decide, so a follow that
/// loses the stranger afterwards cannot turn a told stranger into a failed errand.
/// </para>
///
/// <para>
/// The stranger answers, and what it answers is its guild's opinion of the warden's: a toll is the start of a quarrel
/// between guilds that already dislike each other, and a formality between ones that do not.
/// </para>
/// </summary>
public sealed class BotWard : BotDeed
{
    public const string Trade = "ward";

    /// <summary>What the walk is reckoned at per minute. Unpaid and pressed on nobody: it is offered, and this orders it.</summary>
    public static double Prior { get; set; } = 300.0;

    public static double WorkMinutes { get; set; } = 1.5;

    /// <summary>How close the warden comes to speak.</summary>
    public static int Reach { get; set; } = 3;

    /// <summary>How long it follows before it gives up.</summary>
    public static int ChaseMs { get; set; } = 120000;

    /// <summary>The opinion under which a stranger answers the warden in anger.</summary>
    public static double Surly { get; set; } = -20.0;

    public static long Spoken { get; private set; }

    public static long Gone { get; private set; }

    public static long Missed { get; private set; }

    private readonly BotMobile _them;

    private readonly string _ours;

    private long _began;

    private bool _begun;

    private bool _said;

    public BotWard(BotMobile them, string ours)
    {
        _them = them;
        _ours = ours;
    }

    public override string Kind => Trade;

    public override bool Summons => true;

    public override Map Map => _them?.Map;

    public override Point3D Where => _them?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override bool Unpaid => true;

    public override string Stage => _said ? $"told {_them?.Name} of the toll" : $"to {_them?.Name}, who is hunting our land";

    public override bool Bend(IBotWilful bot)
    {
        if (_said || _them is not { Deleted: false } || BotLand.Holder(_them.Map, _them.Location) != _ours)
        {
            return true;
        }

        BotTollman.Unreached(bot?.Self, _them);

        return false;
    }

    public override void Drop(IBotWilful bot) => BotTollman.Release(_them);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _them is not { Deleted: false, Alive: true } || _them.Map == null || _them.Map == Map.Internal)
        {
            Missed++;

            return BotDoing.Failed("the hunter is gone");
        }

        BotTollman.Hold(_them);

        if (!_begun)
        {
            _begun = true;
            _began = Core.TickCount;
        }

        if (BotToll.Warned(_them, _ours))
        {
            return BotDoing.Done($"{_them.Name} had been told already");
        }

        if (BotLand.Holder(_them.Map, _them.Location) != _ours)
        {
            Gone++;

            return BotDoing.Done($"{_them.Name} had left the land of {_ours} before it was told");
        }

        if (Core.TickCount - (_began + ChaseMs) >= 0)
        {
            Missed++;

            return BotDoing.Failed($"never got within speaking distance of {_them.Name}");
        }

        if (!body.InRange(_them.Location, Reach))
        {
            return BotDoing.Walk(_them.Map, _them, BotArrival.Within(Reach), $"over to {_them.Name}, who hunts our land");
        }

        BotVoice.Aloud(body, $"You hunt the land of {_ours}. {BotToll.PatrolRate:P0} of what you take here is ours.");

        var theirs = (_them.Guild as Guilds.Guild)?.Name;

        BotVoice.Aloud(_them, theirs != null && BotRegard.Of(theirs, _ours) <= Surly ? "We will see about that." : "So be it.");

        BotToll.Warn(_them, _ours);
        _said = true;
        Spoken++;

        return BotDoing.Done($"told {_them.Name} that hunting the land of {_ours} is tolled");
    }

    public static string Describe() =>
        Spoken + Gone + Missed == 0
            ? "no stranger has been told of a toll"
            : $"{Spoken} strangers told of the toll, {Gone} gone before anybody arrived, {Missed} never reached";

    public static void Forget()
    {
        Spoken = 0;
        Gone = 0;
        Missed = 0;
    }
}
