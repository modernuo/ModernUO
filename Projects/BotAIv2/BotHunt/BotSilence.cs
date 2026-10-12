using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The fence's chase after a witness it could not buy: kill it before it gets away, or it tells.
///
/// <para>
/// <b>Patrick's word of 17.09.2026, night.</b> "They may buy the witness off, or kill it. A killed witness tells
/// nobody; a witness that gets away tells." So this is a race and not a fight: the fence has <see cref="GivesUpMs"/>
/// and <see cref="LosesAt"/> tiles, and the moment either runs out the witness is over the hill and the Baron knows.
/// </para>
///
/// <para>
/// The killing is a murder like any other and is entered on the fence's own rap sheet by the ordinary machinery: a
/// keeper of stolen goods who cuts a witness down has stopped being the quiet one, and the island will hunt it as it
/// hunts the rest of the band.
/// </para>
/// </summary>
public sealed class BotSilence : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSilence));

    public const string Trade = "silence";

    /// <summary>How long the fence chases before the witness is counted away.</summary>
    public static int GivesUpMs { get; set; } = 30000;

    /// <summary>How far the witness must get to be away.</summary>
    public static int LosesAt { get; set; } = 20;

    /// <summary>What the chase is held at against other work, a minute: above everything, because the alternative is a price on its head.</summary>
    public static double Prior { get; set; } = 600.0;

    /// <summary>Chases begun.</summary>
    public static long Chases { get; private set; }

    /// <summary>Witnesses cut down before they could tell.</summary>
    public static long Silenced { get; private set; }

    /// <summary>Witnesses that got away and told.</summary>
    public static long Away { get; private set; }

    private readonly Mobile _witness;

    private readonly BotBrawl _fight;

    private readonly long _began;

    private bool _counted;

    private bool _told;

    public BotSilence(Mobile witness)
    {
        _witness = witness;
        _began = Core.TickCount;
        _fight = new BotBrawl(witness, BotBrawl.Robbery, SkillName.Tactics, Ends);
    }

    /// <summary>The chase's rule: over when the witness is down or away. Losing nerve is not a way out of this one.</summary>
    private static string Ends(IBotWilful bot, BotBrawl brawl)
    {
        var witness = brawl?.Foe;

        if (witness is not { Deleted: false, Alive: true })
        {
            return "the witness is down";
        }

        if (bot?.Self is { } body && (body.Map != witness.Map || !body.InRange(witness.Location, LosesAt)))
        {
            return "the witness is away";
        }

        return null;
    }

    public override string Kind => Trade;

    public override bool Committed => true;

    public override bool Unpaid => true;

    public override bool Braves => true;

    public override bool Repeats(BotDeed other) => other is BotSilence;

    public override Mobile Foe => _witness;

    public override Map Map => _fight.Map;

    public override Point3D Where => _witness?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => GivesUpMs / 60000.0 + 0.5;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    public override int Made => 0;

    public override SkillName? Trains => SkillName.Tactics;

    public override string Stage => "after a witness";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        if (!_counted)
        {
            _counted = true;
            Chases++;
        }

        if (_witness is not { Deleted: false, Alive: true })
        {
            Silenced++;

            logger.Information("{Name} the fence cut down {Witness}, who told nobody", body.Name, _witness?.Name);

            return BotDoing.Done("the witness told nobody");
        }

        if (Core.TickCount - _began >= GivesUpMs || body.Map != _witness.Map
            || !body.InRange(_witness.Location, LosesAt))
        {
            Tell(body as BotMobile);

            return BotDoing.Done($"{_witness.Name} got away");
        }

        return _fight.Advance(bot);
    }

    public override bool Bend(IBotWilful bot) => _fight.Bend(bot);

    public override void Drop(IBotWilful bot)
    {
        _fight.Drop(bot);

        // Dropped rather than seen through: the witness is as good as away, and the Baron hears of it.
        if (_witness is { Deleted: false, Alive: true })
        {
            Tell(bot?.Self as BotMobile);
        }
    }

    /// <summary>
    /// The witness told, once and once only.
    ///
    /// <para>
    /// <b>Both doors out of this deed lead here</b> — the clock running out inside <c>Advance</c>, and the drop that
    /// settles it afterwards — and on the night of 17.09.2026 Hale's chase after Alaric went through both: "Alaric told
    /// the Baron of Hale the fence, and the city puts 5000gp on its head", printed twice at 00:08:53, counted twice,
    /// and a second price put on the same head.
    /// </para>
    /// </summary>
    private void Tell(BotMobile fence)
    {
        if (_told)
        {
            return;
        }

        _told = true;
        Away++;
        BotFence.Tell(fence, _witness);
    }

    public static string Describe() =>
        $"{Chases} chases after a witness: {Silenced} cut down before they could tell, {Away} got away and told";

    public static void Forget()
    {
        Chases = 0;
        Silenced = 0;
        Away = 0;
    }
}
