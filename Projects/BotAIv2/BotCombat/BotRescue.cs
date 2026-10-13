using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Going to somebody's aid, or hitting back at whatever is hitting you.
///
/// <para>
/// <b>It is a hunt with a different name and a different price, and both differences are the point.</b> The
/// fighting itself is <see cref="BotSlay"/>'s — closing at the right distance for the weapon, giving ground
/// when something gets too near, the flight rule, the caps on a fight that is going nowhere, the corpse
/// afterwards. Writing a second one of those would be writing a second set of the same bugs. What this adds
/// is that the work is worth dropping other work for, and that it is filed under its own name so the ledger
/// never averages "went to help Orin" together with "went to kill a rat".
/// </para>
///
/// <para>
/// <b>Pressing, which almost nothing is.</b> The dwell exists so bots finish what they start, and it is right
/// nearly always — a vein is still there in half a minute. A bot being eaten is not: half a minute is the
/// whole of the event. This is the case the pressing flag was written for.
/// </para>
/// </summary>
public sealed class BotRescue : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "rescue";

    /// <summary>
    /// What going to somebody's aid is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// Above a lone hunt, and not because the corpse pays better — it is the same corpse. What is worth more
    /// is what does not happen: a bot that dies drops everything it carried, spends three minutes dead, and
    /// walks back for its own corpse afterwards. The ledger will pull this number towards what actually
    /// arrives in the pack, which will be less; that is correct and it should still be chosen, because the
    /// thing it is really buying is not in the pack.
    /// </para>
    /// </summary>
    /// <para>
    /// <b>Raised from 140 to 400 by Patrick's order of 08.09.2026: help must always come.</b> He watched a
    /// bot being chewed on by a rat with ten others standing about, and the shard's own count agreed — 30
    /// cries for help, 19 answered. The cry was never the problem: BotRescuer raises one on every branch,
    /// including the branch where the bot could win alone. What failed was the price. Carrying goods to a
    /// shopkeeper is worth 183 to 455 a minute here, so a bot doing anything profitable weighed a
    /// neighbour's life against its errand and kept walking.
    /// </para>
    ///
    /// <para>
    /// <b>Safe to raise because the crowd is refused elsewhere.</b> The fear that everybody drops everything
    /// and piles onto one rat is answered by <c>BotQuarry.Crowded</c>, which turns away anyone arriving at a
    /// foe that already has enough on it — so this number decides whether help comes, and that one decides
    /// how much. Without the second, raising the first would have been the pile of bots orbiting one monster
    /// this project has already paid for.
    /// </para>
    // Lowered from 400 on 16.09.2026: the overstated alarm measured 50–81/min against ~200 claimed over 900 outcomes
    // (the claim is this times BotGuilds.Worth); a failing friend stays above a steady one. Patrick's decision, that evening.
    public static double Prior { get; set; } = 200.0;

    private readonly BotSlay _fight;

    private readonly Mobile _friend;

    private readonly BaseCreature _foe;

    private readonly bool _own;

    public BotRescue(BotSlay fight, Mobile friend, BaseCreature foe, bool own, Mobile rescuer = null)
    {
        _fight = fight;
        _friend = friend;
        _foe = foe;
        _own = own;

        // <b>Taken at construction, not at the first beat.</b> Expects is read by the auction before Advance
        // ever runs, so a guild tie discovered later is a guild tie that never affected the choice — which
        // is the whole point of it.
        _rescuer = rescuer ?? (own ? friend : null);
    }

    /// <summary>Whether this is hitting back on one's own behalf rather than going to somebody else's aid.</summary>
    public bool Own => _own;

    /// <summary>Who is being gone to, or the bot itself when it is hitting back. See <c>BotReflex.Took</c>.</summary>
    public Mobile Friend => _friend;

    /// <summary>Whether the fight is a stand: not given up for the bot's own health. See <c>BotDefender.Stand</c>.</summary>
    public bool ToTheEnd => _fight.ToTheEnd;

    /// <summary>What is being hit back at, or pulled off the friend. See <see cref="BotDeed.Foe"/>.</summary>
    public override Mobile Foe => _foe;

    /// <summary>Put down for a flight and taken up again: the friend is asked about afresh on the first beat back. See <see cref="BotDeed.Resumes"/>.</summary>
    public override bool Resumes => true;

    public override string Kind => Trade;

    /// <summary>
    /// Somebody of ours in trouble, or something hitting this bot: a call from outside its own business, which
    /// no hold on a trade may refuse. See <see cref="BotDeed.Summons"/>.
    /// </summary>
    public override bool Summons => true;

    public override Map Map => _fight.Map;

    public override Point3D Where => _fight.Where;

    /// <summary>
    /// What answering this cry is worth, which depends on how badly the caller needs it.
    ///
    /// <para>
    /// <b>A flat price made every cry an emergency, and the shard paid for it in dropped work.</b>
    /// BotRescuer raises a cry on every branch — including the branch where the bot is at full health and
    /// winning — because saying so costs nothing. At a flat 400 a minute the answer then outbid almost
    /// everything: measured 08.09.2026, dropped errands went from twenty-odd a window to 45-70, and the
    /// shard's finishing rate sat at 68-74% instead of 85.
    /// </para>
    ///
    /// <para>
    /// So the number now says what it means. A friend below <see cref="BotSlay.FleeAt"/> — the same health
    /// at which a bot gives up its own fight — is worth <see cref="Prior"/>, and that outbids any errand on
    /// the shard, which is what Patrick asked for. A friend still on its feet is worth
    /// <see cref="Steady"/>: help is offered, it wins against ordinary work, and it does not tear a smith
    /// away from a hot forge because somebody two hundred tiles off is trading blows with a rat and winning.
    /// </para>
    /// </summary>
    public override double Expects => (Failing ? Prior : Steady) * BotGuilds.Worth(_friend, _rescuer);

    /// <summary>Who is going, kept so the guild tie between the two can be read. See BotGuilds.Worth.</summary>
    private Mobile _rescuer;

    /// <summary>What answering is worth when the caller is not actually in danger yet.</summary>
    // Lowered from 150 with the prior above, on 16.09.2026, keeping a friend that is failing above one that is not.
    public static double Steady { get; set; } = 100.0;

    /// <summary>Whether the one who called is losing: below the health at which it would flee its own fight.</summary>
    private bool Failing =>
        _friend is { Deleted: false, Alive: true, HitsMax: > 0 } friend
        && friend.Hits < friend.HitsMax * BotSlay.FleeAt;

    public override double Minutes => _fight.Minutes;

    public override SkillName? Trains => _fight.Trains;

    public override int Outlay => 0;

    public override double Coin => _fight.Coin;

    public override int Made => _fight.Made;

    /// <summary>The whole reason this exists as its own undertaking. See the note above.</summary>
    /// <summary>
    /// Whether this may jump the floor protecting whatever the bot is already doing.
    ///
    /// Only for a friend that is losing. A cry from somebody at full health is still answered — it simply
    /// waits for the errand in hand to reach its next review, which is at most fifteen seconds away and is
    /// the whole reason the dwell exists. See Expects.
    /// </summary>
    public override bool Pressing(IBotWilful bot) => Failing;

    public override string Stage =>
        _own
            ? $"hitting back at {_foe?.Name ?? "it"}"
            : $"{_friend?.Name ?? "somebody"} is being set upon by {_foe?.Name ?? "something"}";

    public override bool Bend(IBotWilful bot) => _fight.Bend(bot);

    public override BotDoing Advance(IBotWilful bot)
    {
        _rescuer ??= bot?.Self;

        // The one who called is safe, or beyond saving. Either way this is over, and it is not a failure:
        // the fight happened or it did not, and nothing about the ground was proved bad.
        if (!_own && _friend is not { Deleted: false, Alive: true })
        {
            return BotDoing.Done($"{_friend?.Name ?? "they"} are past helping");
        }

        // The lowest the one called for fell to while this rescue stood, for the tale it will tell (07.10.2026, traits17).
        if (!_own && _friend.Hits < _lowest)
        {
            _lowest = _friend.Hits;
        }

        var doing = _fight.Advance(bot);

        // Saved: a friend made and a tale to tell, once, when the fight ends with the one called for standing. See BotBiography.Rescued.
        if (!_told && !_own && doing.Kind == BotDoingKind.Done && _friend is { Deleted: false, Alive: true })
        {
            _told = true;
            BotBiography.Rescued(_rescuer, _friend, _foe, _lowest == int.MaxValue ? _friend.Hits : _lowest, _friend.HitsMax);
        }

        // <b>Help given across a guild line is remembered by both.</b> Patrick's order of 13.09.2026: mutual
        // aid makes allies. Counted once, on the fight ending well, and only when the two are of different
        // guilds - a guildmate's rescue is already worth ×1.35 to the rescuer and needs no opinion moved.
        if (!_thanked && !_own && doing.Kind == BotDoingKind.Done
            && _rescuer?.Guild is Guilds.Guild ours && _friend?.Guild is Guilds.Guild theirs && ours != theirs)
        {
            _thanked = true;
            BotRegard.Helped(ours.Name, theirs.Name);
        }

        return doing;
    }

    private bool _thanked;

    private bool _told;

    private int _lowest = int.MaxValue;

    /// <summary>A stand is counted where it is taken — won at auction or pressed — never where it is offered. See <c>BotDefender.Stood</c>.</summary>
    public override void Taken(IBotWilful bot)
    {
        if (_own && ToTheEnd)
        {
            BotDefender.Stood(bot?.Self, _foe);
        }
    }

    /// <summary>
    /// The stand's honest price — the bot that died in it — is counted in <c>BotWill.Settle</c> on the death itself, not here: a stand
    /// put down for a flight the bot then died in is dropped here too, with the bot dead, and read as a death in the stand until
    /// 13.10.2026 (w5 review A).
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        _fight.Drop(bot);

        // Whoever was crying has had somebody come; if they are still in trouble they will say so again on
        // their own next beat. Leaving the cry standing would send a second and a third bot at a fight that
        // is already finished.
        if (!_own)
        {
            BotCry.Quiet(_friend);
        }
    }
}
