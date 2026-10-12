using System;
using Server.BotAI.V2;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.Mind;

/// <summary>
/// A real piece of the shard's work, taken up because a mind asked for it, and measured because a mind
/// predicted something about it.
///
/// <para>
/// <b>It forwards everything and invents nothing.</b> The work inside is the same object the shard's own
/// proposer would have handed out — the same digging, the same hunt, the same shopping trip — so a thinking
/// bot does exactly what the others do, by exactly the same code, and there is no second implementation of
/// anything to drift out of step. Two members are this wrapper's own and they are the only two that could
/// be: what it is <em>called</em>, and what it is <em>expected to be worth</em>.
/// </para>
///
/// <para>
/// <b>The name matters more than it looks.</b> Reported as <c>mind-hunt</c> rather than <c>hunt</c>, the
/// ledger files a thinking bot's hunts separately from the population's, so the two learn about the same
/// ground independently and neither one's experience is quietly averaged into the other's. It is still a
/// kind of work and not one instance of it, which is the rule the ledger key has to satisfy.
/// </para>
///
/// <para>
/// <b>And the takings are measured here rather than taken from the ledger.</b> What the mind needs to be
/// shown is the thing it predicted — gold a minute — measured over the same stretch of time by something
/// that is not the mind. Pack and bank together, because banking money is not spending it.
/// </para>
/// </summary>
public sealed class BotMindDeed : BotDeed
{
    private readonly BotDeed _work;

    private readonly BotMind _mind;

    /// <summary>What the auction is asked to weigh: the work's own worth and nothing of the model's.</summary>
    private readonly double _bids;

    /// <summary>What the mind said this would come to. Judged afterwards; bid with never.</summary>
    private readonly double _foretells;

    /// <summary>The name the mind chose by, which is the name it will be told about.</summary>
    private readonly string _trade;

    /// <summary>The choice this deed came out of, claimed if and only if the shard starts this deed.</summary>
    private readonly BotMindChoice _choice;

    private long _began;

    private int _opened;

    private string _ending = "dropped";

    private bool _settled;

    private bool _claimed;

    public BotMindDeed(BotMind mind, BotDeed work, BotMindChoice choice, Mobile body)
    {
        _mind = mind;
        _work = work;
        _choice = choice;

        // Stood in until the deed is claimed, so that nothing here is ever unset. See Claim for where these
        // two actually get their value.
        _began = Core.TickCount;
        _opened = Worth(body);

        // <b>What is bid, and what is foretold, are two different numbers — and making them one number is
        // how a model was taught to bid nothing.</b> The mind's prediction used to be both: the figure the
        // auction weighed the offer by, and the figure the mind was afterwards judged against. A number that
        // is a promise and a wager at the same time can be won by lying in one direction, and the models
        // found it within a day. Aldric wrote itself the rule <em>"never select prowl for profit; always
        // predict zero return on this shard"</em> — arrived at, in its own words, to avoid "being overruled
        // by the shard's arithmetic" — and then bid nought on three trades running, which the auction duly
        // refused. The scoreboard for 25.08.2026 read 24 decisions and 2 taken up.
        //
        // So the bid is the work's own worth, lifted by a fixed amount that says only "a mind asked for
        // this" — nothing the model says touches it, in either direction. The prediction is kept apart, bid
        // with nothing, and judged honestly. A forecast that costs nothing to be right about is the only
        // kind worth measuring.
        _bids = work.Expects * Insistence;

        // Not clamped. A ceiling on the prediction was a protection for the auction, and the auction no
        // longer reads it; a mind that answers ninety thousand a minute is a mind that should be shown to
        // have been ninety thousand out.
        _foretells = Math.Max(0.0, choice?.Expect ?? work.Expects);

        // <b>One word for one trade, everywhere.</b> The menu offers <c>Scribe</c>; the deed it hands back
        // calls itself <c>inscribe</c>; and the recital of what past choices came to used to use the second
        // word. Not one of the seventeen trades matched its own history: Hunter answered as <em>hunt</em> or
        // as <em>prowl</em>, Shopper as <em>restock</em>, Seeker as <em>acquire</em>. So every rule the three
        // minds had ever written was filed under a word that never appears on the menu — "Prowl on this
        // shard yields less than 10 gold/min; avoid selecting it" is unactionable advice about a trade that
        // cannot be chosen. The name the mind chose by is the name it is told about afterwards.
        _trade = choice?.Intent ?? work.Kind;
    }

    /// <summary>
    /// What a mind's asking for a thing is worth on top of the thing itself.
    ///
    /// <para>
    /// Deliberately the same as <see cref="BotAppraisal.Inertia"/>: a mind's preference counts for exactly
    /// as much as the fact that a bot is already doing something. Enough to settle a close call in the
    /// mind's favour, not enough to hold a bot on work that is plainly worse.
    /// </para>
    /// </summary>
    public static double Insistence { get; set; } = 1.25;

    /// <summary>What the work inside is, so the log and the ledger say which trade this was.</summary>
    public BotDeed Work => _work;

    public override string Kind => $"mind-{_work.Kind}";

    public override Map Map => _work.Map;

    public override Point3D Where => _work.Where;

    public override double Expects => _bids;

    /// <summary>
    /// The work's own estimate of how long it will take, and never the model's.
    ///
    /// This was the second half of the same wager. <see cref="BotAppraisal"/> judges distance by the ratio of
    /// working time to walking time, so a mind that answered "sixty minutes" made every journey on the map
    /// look free — a lever on the auction that had nothing to do with being right about anything. The mind
    /// still says how long it expects to be; it says it into the log, where a prediction belongs.
    /// </summary>
    public override double Minutes => _work.Minutes;

    public override SkillName? Trains => _work.Trains;

    public override int Outlay => _work.Outlay;

    public override bool AtCounter => _work.AtCounter;

    public override bool Braves => _work.Braves;

    public override double Coin => _work.Coin;

    public override int Made => _work.Made;

    public override bool Alongside => _work.Alongside;

    /// <summary>
    /// Forwarded, like everything else about the work. A wrapper that answered for itself here would put a
    /// thinking bot at a sprint on the one errand written to be taken at a walk — and it would do it only for
    /// the four bots that think, which is the hardest kind of difference to notice.
    /// </summary>
    public override bool Hurries => _work.Hurries;

    /// <summary>
    /// Forwarded for the reason directly above, and it stopped being cosmetic on 09.09.2026.
    ///
    /// <para>
    /// This flag used to be read by <c>BotAppraisal</c> as a factor, so a wrapper that lost it cost a
    /// thinking bot a discount and nothing else. It is now read by <c>BotWill.Auction</c> as the rank that
    /// decides what a bot too heavy to walk may take at all - and Porter is on the menu the minds are shown,
    /// so a mind that asked to go and unload would have had its own answer weighed as work that needs a
    /// step. Four bots on this shard think; they would have been the only four still loading themselves to
    /// a standstill, which is the hardest kind of difference to notice.
    /// </para>
    /// </summary>
    public override bool Standing => _work.Standing;

    /// <summary>Forwarded, for the reason the two above give: a wrapper that answers for itself here would
    /// let the four thinking bots alone throw away goods a guild had paid for.</summary>
    public override bool Committed => _work.Committed;

    public override string Stage => _work.Stage;

    public override bool Pressing(IBotWilful bot) => _work.Pressing(bot);

    public override bool Bend(IBotWilful bot) => _work.Bend(bot);

    /// <summary>
    /// The moment this stopped being an offer and became the work in hand.
    ///
    /// <para>
    /// <b>Both the claim and the clock belong here rather than in the constructor.</b> A deed is built every
    /// time the auction asks, and most of them are built only to be weighed and thrown away; the one that is
    /// advanced or dropped is the one that was actually started. Stamping the clock at construction would
    /// measure a losing offer's idle minutes into the winner's rate — the same mistake as measuring a
    /// formation from where the squad was standing rather than from the enemy, and just as invisible in the
    /// arithmetic afterwards.
    /// </para>
    /// </summary>
    private void Claim(IBotWilful bot)
    {
        if (_claimed)
        {
            return;
        }

        _claimed = true;
        _began = Core.TickCount;
        _opened = Worth(bot?.Self);

        _mind.Began(_choice);
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        Claim(bot);

        var doing = _work.Advance(bot);

        // The only place the ending is knowable. Nothing tells a dropped undertaking why it ended, so it is
        // caught here on the way past instead of being guessed at afterwards.
        _ending = doing.Kind switch
        {
            BotDoingKind.Done => "finished",
            BotDoingKind.Failed => "failed",
            _ => _ending
        };

        return doing;
    }

    public override void Drop(IBotWilful bot)
    {
        // Only a deed the shard committed to is ever dropped, so this is the second of the two doors work
        // can start behind: committed and then given up before its first beat.
        Claim(bot);

        _work.Drop(bot);

        if (_settled)
        {
            return;
        }

        _settled = true;

        var minutes = Math.Max(0.01, (Core.TickCount - _began) / 60000.0);
        var gained = Worth(bot?.Self) - _opened;

        // The gold and the time, never a rate worked out here. Whether these two make a rate at all is the
        // mind's question, and it has a floor for it: see BotMind.WorthCountingMs.
        _mind.Settle(_trade, _foretells, gained, minutes, _ending);
    }

    /// <summary>Everything this bot could spend, in gold: what it carries and what it has put away.</summary>
    private static int Worth(Mobile body)
    {
        if (body == null)
        {
            return 0;
        }

        var carried = body.Backpack?.TotalGold ?? 0;

        return carried + Banker.GetBalance(body);
    }
}
