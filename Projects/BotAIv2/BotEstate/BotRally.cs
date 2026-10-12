using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Falling in with the guild's war company and fighting with it, as a company, until the company is done.
///
/// <para>
/// <b>Patrick's order of 13.09.2026, the fifth of the evening: when one guild attacks another, a company
/// forms and there is a real battle.</b> Until this a war was a hundred duels: every member offered a
/// quarrel of its own, walked at its own enemy and fought alone, and the squad — the formation, the medics
/// behind it, the casters, the share-out — existed only against monsters. The squad's fighting code never
/// cared what it fought: it sets a combatant and asks the engine whether the blow is lawful, and in a war
/// it is. What it lacked was a way to be formed for a war.
/// </para>
///
/// <para>
/// <b>This errand is the way.</b> The first member to answer a call or a threat forms the guild's war
/// company and points it at the enemy (<c>BotFeud.Rally</c>); every member after it is handed this, walks
/// to the company and joins it; and from then on the company's own beat does the fighting — station,
/// formation, blow, bandage, spell — while this errand does nothing but stand in the ranks. It runs
/// <c>Alongside</c> the company like a hunt's enlistment does, and it ends when the company does.
/// </para>
///
/// <para>
/// <b>Not the enlistment errand, though it is nearly one.</b> <c>BotEnlist</c> refuses a company of one,
/// which is right for a hunt and wrong for the bot that just formed the company: the founder would be
/// refused its own company in the same second. This one holds as long as the company holds.
/// </para>
/// </summary>
public sealed class BotRally : BotDeed
{
    /// <summary>The ledger key.</summary>
    public const string Trade = "rally";

    /// <summary>What answering the guild's call is reckoned at per minute. A quarrel's.</summary>
    public static double Prior { get; set; } = 400.0;

    /// <summary>And what defending the guild's own ground is. A quarrel's, in defence.</summary>
    public static double DefendPrior { get; set; } = 700.0;

    /// <summary>How long one is expected to run. The fight's, not the walk's.</summary>
    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>Rallies stood through to the company's end.</summary>
    public static long Stood { get; private set; }

    /// <summary>Rallies that found no company to join by the time they arrived.</summary>
    public static long Late { get; private set; }

    /// <summary>Marches begun: a leader setting its company walking at an enemy out of reach.</summary>
    public static long Marched { get; private set; }

    /// <summary>Rallies that found their company already on a living enemy and left it on that one. See <see cref="Advance"/>.</summary>
    public static long Kept { get; private set; }

    /// <summary>How far the enemy may drift from where the leader last set out for before the march is re-aimed.</summary>
    public static int Drift { get; set; } = 8;

    private readonly Guild _guild;

    /// <summary>The guild this rally is for. See <c>BotWar.Involved</c>.</summary>
    public Guild Guild => _guild;

    private readonly BotMobile _enemy;

    private readonly Map _map;

    private readonly bool _defending;

    /// <summary>The company, from the first beat this errand is actually held. See <c>BotFeud.Form</c>.</summary>
    private BotSquad _company;

    private bool _joined;

    /// <summary>Where the leader last set out for on a march, so a moving enemy does not reset the walk every beat.</summary>
    private Point3D _march;

    /// <summary>Whether this straggler has been let go from the muster with a wave. See <c>BotFeud.WaveGoes</c>.</summary>
    private bool _waved;

    public BotRally(Guild guild, BotMobile enemy, Map map, bool defending)
    {
        _guild = guild;
        _enemy = enemy;
        _map = map;
        _defending = defending;
    }

    public override string Kind => Trade;

    /// <summary>The guild's war company calling its members in. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    /// <summary>
    /// Work put down for a war's muster is kept for as long as the war company holds the bot, as it is for any other company's
    /// call (12.10.2026, w2): 45 pieces in 10.2 h on 11.10 and 74 in 38.7 h on 09–10.10 were dropped "not taken up again after
    /// rally: it was put down for N minutes". See <see cref="BotDeed.Keeps"/>.
    /// </summary>
    public override bool Keeps => true;

    public override Map Map => _map;

    /// <summary>Where the company stands, or where the enemy was seen until there is a company.</summary>
    public override Point3D Where => _company?.Anchor ?? _enemy?.Location ?? Point3D.Zero;

    public override double Expects => _defending ? DefendPrior : Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 0.0;

    /// <summary>A guild's war earns the bot nothing. See <c>BotQuarrel.Unpaid</c>.</summary>
    public override bool Unpaid => true;

    /// <summary>The company is the work; this runs beside it. See <c>BotEnlist.Alongside</c>.</summary>
    public override bool Alongside => true;

    /// <summary>
    /// A member stands where the company stands, once it is in it — the enlistment's rule (<c>BotEnlist.Still</c>), which
    /// this errand, its near twin, never had.
    ///
    /// <para>
    /// <b>Fifteen minutes of defending the yard were judged "nothing has come of this".</b> The Ash declared war on The
    /// Blade at 08:44 on 24.09.2026; The Blade's company stood on its own ground, and at 09:00–09:02 seven of its members —
    /// Aric, Delwyn, Cassia, Vesna, Emrys, Calla Ashdown, Corwin — were failed by BotWill's quarter-hour watch, because in
    /// the ranks this errand says "in the war company, N of us" on every beat; two more were cancelled by the stall watch,
    /// and the stall watch printed the rest as errors. The company's own clock ends it: a war company is charged until
    /// BotFeud.Disband (the war over, or nothing left to fight), then the quiet clock lets it go, and this errand ends
    /// "the war company is done". On the walk to the company the watches still watch.
    /// </para>
    /// </summary>
    public override bool Still => _joined;

    /// <summary>Defending the yard jumps the work in hand. See <c>BotQuarrel.Pressing</c>.</summary>
    public override bool Pressing(IBotWilful bot) => _defending;

    public override string Stage =>
        (_defending ? "defending our ground: " : "")
        + (_joined ? $"in the war company of {_guild?.Name}, {_company?.Count} of us" : $"rallying to the war company of {_guild?.Name}");

    public override bool Bend(IBotWilful bot) => false;

    public override void Drop(IBotWilful bot)
    {
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        if (bot is not IBotSquadMember member)
        {
            return BotDoing.Failed("not the sort of thing that joins companies");
        }

        // The first beat of a rally that won its auction. The company is formed now, by this bot, or it is
        // the one somebody else formed since the offer was made. See BotFeud.Form for why not sooner.
        if (_company == null)
        {
            _company = BotFeud.Company(_guild);

            if (_company == null)
            {
                if (_enemy is not { Deleted: false, Alive: true })
                {
                    return BotDoing.Done("the enemy was down before a company could be formed");
                }

                _company = BotFeud.Form(member, _guild, _enemy, _defending);

                if (_company == null)
                {
                    return member.Squad != null
                        ? BotDoing.Done("fell in with another company on the way")
                        : BotDoing.Failed("could not form the war company");
                }
            }

            // <b>Pointed at this rally's enemy only when it is on nobody, which a company already fighting is not.</b>
            // Every rally engaged the company on its own first beat, so a guild's company was aimed at whichever enemy the
            // latest member to answer had been sent after, wherever that member stood. On build 48 The Hammer's company,
            // Ronan alone, went from Alden Ashdown to Ulric, Lorcan and Jorunn between 04:46:08 and 04:46:55 on 15.09.2026,
            // each change in the second somebody took a rally, and broke off "we never got near it, nearest 669 tiles off";
            // six strong at 04:53 it broke off Quenna 505 tiles off. The same rule the guild's call keeps (see
            // BotFeud.Call): a member arriving does not swing the whole guild onto whoever it saw first. A company on a
            // living enemy keeps it, and BotFeud.Watch points it at the next one when that one is down.
            if (BotFeud.Mustering(_company))
            {
                // Not aimed yet: the muster aims it when it sets out (BotFeud.Watch), at whoever the guild is on then.
            }
            else if (_company.Focus is { Deleted: false, Alive: true } held && !ReferenceEquals(held, _enemy))
            {
                Kept++;
            }
            else
            {
                _company.Engage(_enemy, ReferenceEquals(member.Squad, _company) ? member : null);
            }
        }

        if (!BotFeud.Standing(_company))
        {
            if (_joined)
            {
                Stood++;

                return BotDoing.Done("the war company is done");
            }

            Late++;

            return BotDoing.Failed("the war company was gone before it got there");
        }

        if (member.Squad != null)
        {
            if (!ReferenceEquals(member.Squad, _company))
            {
                return BotDoing.Done("fell in with another company on the way");
            }

            _joined = true;

            // <b>While the company musters, its leader stands at the muster and the company forms on him.</b> See
            // BotFeud.MusterMs. A fight that finds the muster (a member set upon) is fought where it is; after it, back.
            if (ReferenceEquals(_company.Leader, member) && BotFeud.Mustering(_company) && _company.Stance != BotSquadStance.Fighting)
            {
                var at = BotFeud.MusterAt(_company);

                if (at != Point3D.Zero && !body.InRange(at, 2))
                {
                    return BotDoing.Walk(_map, at, BotArrival.Within(2), $"to the muster at ({at.X}, {at.Y})");
                }

                return BotDoing.Work($"mustering the war company at ({at.X}, {at.Y}): {BotFeud.Gathered(_company)} of {BotFeud.Needed(_guild, _company)}");
            }

            // <b>The leader walks at the enemy; the company is stationed on the leader and follows.</b> Until
            // the sortie (14.09.2026) every war company was formed beside its enemy and never had to move;
            // one pointed at an enemy four hundred tiles off stood where it was formed, because the only
            // walking a member does is to its station and the leader is stationed on nobody. Aimed at where
            // the enemy stands and re-aimed only when it has drifted, or a moving target would reset the walk
            // every beat (see BotWalk.Matches).
            if (ReferenceEquals(_company.Leader, member)
                && _company.Focus is { Deleted: false, Alive: true } focus
                && focus.Map == _map
                && !body.InRange(focus.Location, BotSquad.PressReach))
            {
                if (_march == Point3D.Zero || !Utility.InRange(_march, focus.Location, Drift))
                {
                    if (_march == Point3D.Zero)
                    {
                        Marched++;
                    }

                    _march = focus.Location;
                }

                return BotDoing.Walk(_map, _march, BotArrival.Within(BotSquad.PressReach - 1), $"at {focus.Name}");
            }

            _march = Point3D.Zero;

            return BotDoing.Work($"in the war company, {_company.Count} of us");
        }

        var anchor = _company.Anchor;

        if (!body.InRange(anchor, BotSquad.PressReach))
        {
            // <b>A straggler does not walk to a company already out on its own.</b> It waits at the muster until a wave of
            // BotFeud.Wave has gathered there, or the first of them has waited BotFeud.WaveMs, and they go together. See
            // BotFeud.MusterMs for what walking alone into the enemy cost.
            if (!_waved && BotFeud.Out(_company) && !body.InRange(anchor, BotFeud.Straggle))
            {
                var at = BotFeud.MusterAt(_company);

                if (at != Point3D.Zero)
                {
                    if (!body.InRange(at, BotFeud.Assembly))
                    {
                        return BotDoing.Walk(_map, at, BotArrival.Within(BotFeud.Assembly - 2), $"to the muster at ({at.X}, {at.Y}) for the next wave");
                    }

                    if (!BotFeud.WaveGoes(_company, body))
                    {
                        return BotDoing.Work($"waiting at the muster for the next wave: {BotFeud.Waiting(_company)} of {BotFeud.Wave}");
                    }

                    _waved = true;
                }
            }

            return BotDoing.Walk(_map, anchor, BotArrival.Within(BotSquad.PressReach - 1), "to the war company");
        }

        if (!BotSquads.Join(_company, member))
        {
            Late++;

            return BotDoing.Failed("the war company had no room by the time it arrived");
        }

        _joined = true;

        return BotDoing.Work($"fell in with the war company, {_company.Count} of us");
    }

    public static string Describe() =>
        Stood + Late + Marched + Kept == 0
            ? "nobody has rallied to a war company"
            : $"{Stood} rallies stood through to the company's end, {Late} arrived to find no company, {Marched} marches led at an enemy out of reach, {Kept} rallies left their company on the enemy it already had";

    public static void Forget()
    {
        Stood = 0;
        Late = 0;
        Marched = 0;
        Kept = 0;
    }
}
