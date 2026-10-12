namespace Server.BotAI.V2;

/// <summary>
/// One undertaking: a piece of work with stages, held until it finishes, fails or is dropped.
///
/// <para>
/// <b>Its stages are its own business.</b> Mining is not "walk to a vein" — it is dig, then smelt, then bank
/// what came of it, and an undertaking that ends at the vein leaves a bot standing underground holding ore
/// nobody can buy. So the undertaking, not the brain, knows the sequence: it is asked what to do now, over
/// and over, and it answers with a place, with "work here", or with an ending. The brain never learns what
/// ore is.
/// </para>
///
/// <para>
/// <b>This is what replaces the first version's one big method.</b> There, choosing was a single function of
/// 1209 lines inside a file of 7985 that referenced 57 other modules, so every change to any behaviour was a
/// change to that file. Here a new kind of work is a new folder with a proposer and a subclass of this, and
/// nothing in <c>BotWill/</c> is touched at all.
/// </para>
///
/// <para>
/// <b>It is also where the bot's state lives while the work is in progress.</b> An undertaking is created per
/// bot by its proposer and held on that bot's <see cref="BotResolve"/> — so a proposer keeps no table keyed
/// by serial, and a deleted bot takes its half-finished business with it.
/// </para>
/// </summary>
public abstract class BotDeed
{
    /// <summary>
    /// Short, stable key for what kind of work this is — <c>mine</c>, <c>smelt</c>, <c>hunt</c>. It is the
    /// key the ledger measures against and the word that appears in the log, so it names a <em>kind</em> of
    /// work and never one instance of it: "mine" learns from thirty trips, "mine-vein-4193" learns from one.
    /// </summary>
    public abstract string Kind { get; }

    /// <summary>Which map the work happens on. An undertaking on another map is not offered.</summary>
    public abstract Map Map { get; }

    /// <summary>
    /// Where the work happens, roughly. Used for two things only — how far away it is, and which patch of
    /// ground the ledger files the outcome under — so the centre of a mine is as good as a particular vein.
    /// </summary>
    public abstract Point3D Where { get; }

    /// <summary>
    /// What the takings are expected to come to, in gold-equivalent per minute, before this bot's own
    /// experience is taken into account. The proposer's claim, and the only number it is asked for.
    ///
    /// <para>
    /// It is a prior, not a promise: <see cref="BotLedger"/> blends it with what actually happened the last
    /// few times this bot did this here, and measurement wins as evidence accumulates. That is the whole
    /// learning mechanism, and it needs no model — a proposer that is systematically optimistic gets
    /// corrected by the shard rather than argued with.
    /// </para>
    /// </summary>
    public abstract double Expects { get; }

    /// <summary>
    /// Which skill this work trains, or null for work that trains nothing.
    ///
    /// <para>
    /// Named rather than inferred, and credited only when the undertaking actually <em>finishes</em>. Both
    /// halves are anti-exploit measures against the metric this project is built on. Reward the raw rate of
    /// skill gain and the best available behaviour is the cheapest repeatable twitch that gains skill — a
    /// training dummy, a spell cast at nothing, two bots sparring in a field for ever. Tying the credit to a
    /// finished piece of work means the gain has to arrive alongside ore, a corpse or a delivery.
    /// </para>
    /// </summary>
    public virtual SkillName? Trains => null;

    /// <summary>
    /// Gold that must be in hand before this can start — reagents, tools, ammunition, a share of a pot.
    ///
    /// <b>This is what need is measured against.</b> Not a comfort threshold: the first version compared
    /// every purse to a flat 250 and so was born with the entire population reading as short of money, and a
    /// signal that is on for everybody always is not a signal. What a bot cannot afford is what it was about
    /// to try to do.
    /// </summary>
    public virtual int Outlay => 0;

    /// <summary>
    /// Whether this work changes money over a shopkeeper's counter, which the engine does through the backpack.
    ///
    /// <para>
    /// <b>A counter is paid out of the pack and pays into it, and a pack holds a hundred and twenty-five things.</b> A
    /// bill under two thousand gold is taken from the backpack, so the coin for it is drawn out of the bank into the pack
    /// first, and what a shopkeeper pays for goods is put into the pack. A pack at the engine's cap with no pile of gold
    /// to add to can do neither, and the work fails at the counter on its first beat. On 14.09.2026 Hale carried 197 oil
    /// cloths nobody would buy, handed back by the market past the cap, and failed a trip to the shops 8,719 times in
    /// half an hour; the trip is unpaid work, so nothing in the auction learned from any of them.
    /// <see cref="BotAppraisal"/> asks <see cref="BotYield.Pocket"/> before such work is weighed at all.
    /// </para>
    /// </summary>
    public virtual bool AtCounter => false;

    /// <summary>
    /// How much of the takings arrive as money rather than as skill or as goods, from 0 to 1.
    ///
    /// The one place where being short of money changes what a bot picks. Everything here is measured in one
    /// currency so that wants can be compared, but a purse of skill does not buy a pickaxe — so a bot that
    /// needs coin discounts work that pays in anything else, in proportion to how badly it needs it.
    /// </summary>
    public virtual double Coin => 1.0;

    /// <summary>
    /// How long the work itself is expected to take, in minutes, once the bot is there. Used to weigh the
    /// walk against the work: half an hour of digging is worth a five-minute walk and five minutes of digging
    /// is not.
    /// </summary>
    public virtual double Minutes => 5.0;

    /// <summary>
    /// Gold-equivalent of goods this has produced so far and not sold — ingots, cloth, bandages, potions.
    ///
    /// Counted as takings in its own right, so that making something for the population is not automatically
    /// beaten by hawking loot at a shopkeeper. That is not tidiness: bot-to-bot trade moves gold about,
    /// while selling to a shopkeeper <em>creates</em> it, so a brain that only ever counted coin would drive
    /// the whole population into the one faucet the first version already drained by 110,900 in a night.
    /// </summary>
    public virtual int Made => 0;

    /// <summary>
    /// What the bot should do now. Called once per decision while this undertaking is held, and it is the
    /// only method that matters.
    ///
    /// <para>
    /// The undertaking may look at anything it likes about the bot — where it is, what it carries, whether
    /// <see cref="BotJourney.Arrived"/> is true of its own last request — and it advances its own stages. It
    /// must be cheap: it runs on the population's beat, per bot.
    /// </para>
    /// </summary>
    public abstract BotDoing Advance(IBotWilful bot);

    /// <summary>
    /// The walk this undertaking asked for turned out to be impossible, and it is being told so. Return true
    /// to carry on — with somewhere else in mind — or false to be given up.
    ///
    /// <para>
    /// The same shape one level up as movement's own rule that the goal is untouchable and the way bends: a
    /// second forge is a decision only the work knows how to make, so the brain asks instead of guessing.
    /// The default is to give up, because a subsystem that has nothing to say here should not have its bot
    /// looping.
    /// </para>
    /// </summary>
    public virtual bool Bend(IBotWilful bot) => false;

    /// <summary>
    /// Whether a bend of this work is trouble, which lifts a <see cref="Steadfast"/> hold. True by default: a walk told
    /// its way is blocked is the event the hold is meant to yield to.
    ///
    /// <para>
    /// <b>False for work whose bends are routine.</b> The captain's circuit round the training field has posts with no
    /// road — (1462–1464, 1497–1498) at the field the population uses — and <c>BotLesson.Bend</c> simply goes on to the
    /// next post. On 22.09.2026 at 12:21, sixteen seconds after Faron closed the roll with two paid students on the
    /// field, that bend lifted the hold the class had just been given, and at the next review he "dropped drill …
    /// outbid by scout at 35/min": two lessons of 0.7 and 0.5 points where ten minutes were paid for. A skipped post is
    /// not the class being blocked.
    /// </para>
    /// </summary>
    public virtual bool BendIsTrouble => true;

    /// <summary>
    /// Minutes this work, once <see cref="Steadfast"/>, is held for whatever it pays; nought for the ordinary reckoning,
    /// which is the expected pay stretched and capped (<c>BotWill.HoldMs</c>).
    ///
    /// <para>
    /// A class is paid for by the minute at a rate the auction reads as 1 to 13 a minute, so the ordinary hold on it was
    /// a minute and a half to eight; the lesson is ten. Work with a clock of its own and a promise already paid for names
    /// its own hold here, and the cap does not apply to it — the hold's own watchdogs still run.
    /// </para>
    /// </summary>
    public virtual double HoldsFor => 0.0;

    /// <summary>
    /// Whether this will not keep. Asked of an offer only, and it answers one question: may this jump the
    /// floor that protects whatever the bot has already taken on.
    ///
    /// <para>
    /// <b>Written for the thing standing next to the bot.</b> Almost everything on offer here is a place —
    /// a vein, a forge, a counter — and a place is still there in half a minute, which is exactly why the
    /// dwell exists and why it is right. A creature is not: it walks, it is claimed by somebody else, it
    /// kills the bot's neighbour and moves on. A bot that answers "I will consider it after the next thirty
    /// seconds of digging" is a bot that walks past an ogre, and that is what this is for.
    /// </para>
    ///
    /// <para>
    /// The default is false and should stay false for nearly everything: a rung where everything is pressing
    /// is a rung with no dwell, and no dwell is the first version's bot that changed its mind four times a
    /// second and finished nothing.
    /// </para>
    /// </summary>
    public virtual bool Pressing(IBotWilful bot) => false;

    /// <summary>
    /// Whether <paramref name="other"/> is the same work as this one, so pressing it would only start this over.
    ///
    /// <para>
    /// <b>Two doors into one piece of work.</b> Cassia the Mage was pressed into practising hiding at 22:26:40 on
    /// 17.09.2026 by the practice urge, and ten seconds later by the temptation to rob — "tempted, but it hides at 10.9
    /// of the 30 a robbery wants" — which dropped the stint it was already serving and began another. The bot went on
    /// practising, so nothing looked broken, but the five-minute stint never ran: every ending was a drop of a fifth of
    /// a minute with nothing earned and nothing learnt, and the ledger read it as work abandoned.
    /// </para>
    ///
    /// <para>
    /// The default is false, and a piece of work that can sensibly be re-aimed — a fight moved to a nearer outlaw —
    /// must leave it so. Say yes only where a second press adds nothing the first is not already doing.
    /// </para>
    /// </summary>
    public virtual bool Repeats(BotDeed other) => false;

    /// <summary>
    /// Whether this work goes on alongside a squad rather than being displaced by one.
    ///
    /// <para>
    /// <b>Almost nothing may say yes.</b> A squad owns where its members stand — it rebases the bottom of
    /// every member's journey each beat — so a private errand to somewhere else is overwritten within the
    /// second, and that is why joining one sets everything else aside. The exception is work whose whole
    /// subject <em>is</em> the squad: it asks the company for nothing, sends the bot nowhere, and is there
    /// to see the thing through and be paid for it. Without the exception such work would be handed out and
    /// then frozen by the very squad it just called together, and the takings — which arrive as a share of
    /// what the company killed — would be credited to nothing at all.
    /// </para>
    /// </summary>
    /// <summary>
    /// Whether this work is not about money, and must not be refused for failing to earn any.
    ///
    /// <para>
    /// <b>One deed on this shard is paid nothing on purpose, and the arithmetic threw it away for exactly
    /// that.</b> The Baron's rounds pay nothing by design — they are where he is between harrowings — and
    /// while he walks them he spends: on kit, on the stipend, on what a company costs. The ledger measured
    /// that honestly and concluded rounds were worth -40.5 gold a minute, whereupon
    /// <see cref="BotAppraisal"/>'s "expected to pay nothing" veto refused them. With harrowings unavailable
    /// — a fresh population has no grandmasters, so no company can be raised for damned ground — the Baron
    /// was offered one thing, refused it, and stood in Britain for twelve minutes with 600gp in his pack.
    /// </para>
    ///
    /// <para>
    /// The flag rather than a floor on the estimate: a floor would quietly rescue every unprofitable errand
    /// on the shard, including the ones that <i>ought</i> to be refused, and the reason this one is different
    /// is a fact about the work rather than about its price.
    /// </para>
    /// </summary>
    public virtual bool Unpaid => false;

    /// <summary>
    /// Whether the claim is a price somebody posted and holds — a reward in escrow — rather than a guess about what the
    /// work will pay.
    ///
    /// <para>
    /// <b>Such a claim is wrong to correct towards the trade's average, and right to correct by how much of it is earned.</b>
    /// The island corrects a claim by what work of its kind has paid (<see cref="BotCommons.Corrected"/>), which is the right
    /// question for a sweep, whose worth is a fact about sweeping; an errand's worth is a fact about the errand — a hundred
    /// gold for a look, two thousand for a head — and pulling every one of them towards what errands paid on average makes
    /// the rich and the poor ones read alike. Posted work is corrected by the share of posted claims actually earned
    /// instead (<see cref="BotCommons.Realised"/>): what can go wrong with an errand is not its price but whether it is
    /// done. False by default. Answered true by the board's errands (build 95).
    /// </para>
    /// </summary>
    public virtual bool Posted => false;

    /// <summary>
    /// Whether this work can be done by a bot that cannot take a single step.
    ///
    /// <para>
    /// <b>Almost nothing can, and the default says so.</b> A bot past its carrying ceiling is refused every
    /// move by the engine, so an errand that begins with a walk is an errand it will hold, fail, and be
    /// offered again — Joss stood on one tile for thirteen minutes taking and dropping thirty-two of them,
    /// and Roderic reached 271 stones of a 218 ceiling on 08.09.2026 while taking peddling at 183 a minute
    /// over the unloading that would have freed him at 74.
    /// </para>
    ///
    /// <para>
    /// The one thing that is true here is putting goods on the market, because a stall holds its wares
    /// wherever the seller stands. So <c>BotUnload</c> answers true and everything else keeps the default.
    /// </para>
    ///
    /// <para>
    /// <b>What is done with the answer changed on 09.09.2026, and the change is the point of the flag.</b>
    /// It was first a veto in <c>BotAppraisal</c>, which left heavy bots with nothing at all to do for
    /// twenty-seven minutes at a stretch; then a factor there, which left them doing exactly what they had
    /// been doing, because a fiftieth under a fifth root is 0.46. It is read in <c>BotWill.Auction</c> as a
    /// <em>rank</em> now: for a bot past its carrying ceiling, everything that answers true is compared only
    /// with the others that do, and everything else is compared beneath them. A price cannot express "this
    /// cannot be started" at any value; an order can, and it keeps the floor as well, since a bot with
    /// nothing standing on offer simply has one rank and the auction it always had.
    /// </para>
    /// </summary>
    public virtual bool Standing => false;

    public virtual bool Alongside => false;

    /// <summary>
    /// Whether this work has already spent something that dropping it will not get back.
    ///
    /// <para>
    /// <b>False for nearly everything, and it has to be, because giving work up cheaply is how a population
    /// stays responsive.</b> A hunt abandoned halfway has cost some walking; a dig abandoned halfway has
    /// cost some swings. Nothing is owed to either and the auction is right to move on. The exception is
    /// work with a purchase in the middle of it: <c>BotSupply</c> draws the guild's money, buys a lot at a
    /// shop, and only then sets off for the hall, so an errand dropped after the first leg has converted
    /// the guild's coin into goods in a courier's pack and delivered none of it.
    /// </para>
    ///
    /// <para>
    /// Measured 09.09.2026, and it was a defect of mine hours old. Making the carrying ceiling a rank rather
    /// than a discount (see <c>BotWill.Auction</c>) meant a courier that went over its ceiling <em>by
    /// buying the lot</em> immediately had the errand taken off it by the unloading, which then sold the
    /// guild's goods where it stood. Supply errands dropped after the money was spent went from ten in the
    /// hour before to eighty in the hour after, thirty-eight of them to the unloading, and three hundred and
    /// sixty sewing kits were bought to put two hundred and eighteen on a shelf.
    /// </para>
    ///
    /// <para>
    /// The cure has two halves and this is the second. The first is not to send a courier for more than it
    /// can carry, which is where the state should never arise (<c>BotSupplier.Fits</c>); this is what
    /// happens when it arises anyway. It does not make the work immortal - see <c>BotDeed.Pressing</c>,
    /// which still takes precedence, as does the carrying ceiling itself, because a bot that cannot walk
    /// cannot deliver either and nothing is served by holding it to a promise it cannot keep.
    /// </para>
    /// </summary>
    public virtual bool Committed => false;

    /// <summary>
    /// Whether this is paperwork around somebody else's work rather than work: posted from where the bot stands, over in a
    /// beat, and never a reason to take a bot off what it is doing.
    ///
    /// <para>
    /// <b>An order is a few seconds of asking, and it was taking brewers off their batches.</b> Its escrow is declared as
    /// what it made (see <c>BotOrder.Made</c>, which stopped the ledger reading an order as a loss), and a hundred gold made
    /// in a fifth of a minute is five hundred a minute to the ledger — so an order outbid nearly anything a bot was doing.
    /// On build 49, at 05:16:58 on 15.09.2026, two minutes after the boot, ten brewers put their batches down in the same
    /// second for an armour order at 337 to 355 a minute, fifteen in all, and twenty pieces of work had been dropped
    /// "outbid by order" in 05:16–05:17; build 46 dropped seven brews the same way two and a half minutes after its boot, six of
    /// them in one second. Paperwork waits for the work in hand to end and is taken at the next choice, where it still
    /// wins whatever it wins.
    /// </para>
    /// </summary>
    public virtual bool Paperwork => false;

    /// <summary>
    /// Whether where this work goes is a guess — ground picked because something might be there — so that another
    /// guess of the same kind is no reason to turn round.
    ///
    /// <para>
    /// <b>A prowl walks to a dart, and every review threw another.</b> Work that is not steadfast is open to a better
    /// offer once its dwell is up, and against a walk whose worth is a guess about the ground, a fresh guess about other
    /// ground is nearly always a little better somewhere. On build 54 (15.09.2026) 32 prowls were dropped "outbid by
    /// prowl", every one of them between 2.0 and 2.9 minutes in and fourteen at exactly 2.0 — the end of the dwell —
    /// for a new dart often hundreds of tiles away: Aric put down a walk to (1291, 1314) for one to (928, 1632) at
    /// 14/min against 2. 242 prowls went that way across the day's thirteen sessions. The walk is the search (see
    /// <c>BotProwl.ArriveWithin</c>), and the events that end it are already there — arriving, something worth
    /// fighting coming into reach, the road refusing — and a second guess is none of them.
    /// </para>
    ///
    /// <para>
    /// Only the same kind. Anything else still outbids a guess exactly as before, so a peddle or a hunt takes a bot off
    /// a prowl at the first review it wins.
    /// </para>
    /// </summary>
    public virtual bool Guess => false;

    /// <summary>
    /// Whether standing perfectly still, in one place, with the same thing to say about itself, is what this
    /// work looks like when it is going <em>well</em>.
    ///
    /// <para>
    /// <b>The stall watch tests two things and this is the answer to one of them.</b> It asks whether the
    /// bot has moved and whether its <see cref="Stage"/> has changed, and takes the work off it when neither
    /// has for four minutes — which is right for a walk to somewhere unreachable and exactly wrong for a
    /// drill field. On 03.09.2026 it cancelled eight lessons: five students standing in their ranks, whose
    /// stage says how many points they have picked up to one decimal place and therefore does not move at
    /// all while the last few tenths of a skill come in at a hundredth a beat, and three captains calling a
    /// class that nobody had yet joined. Every one of those students had already paid its fee.
    /// </para>
    ///
    /// <para>
    /// <b>A display string is not a progress measure, and that is the whole lesson.</b> Rounding is a fact
    /// about the sentence, not about the work. Rather than widen the decimal — which moves the boundary and
    /// keeps the mistake — work that stands still on purpose says so, and is left to the clock it already
    /// has: a lesson runs out at <c>BotSchool.LessonMs</c>, a roll closes at <c>GatherMs</c>. Whoever set a
    /// clock owns ending the thing, which is the same rule the companies were taught.
    /// </para>
    /// </summary>
    public virtual bool Still => false;

    /// <summary>
    /// Whether this is work a bot sees through once it has begun: held against better-paid offers for as long
    /// as it reckoned it would take, and put down — rather than thrown away — when something that will not
    /// wait takes the bot off it, then taken up again when that is over.
    ///
    /// <para>
    /// <b>Three drops in ten on this shard happened at one instant, and none of them was a decision.</b> On the
    /// morning of 14.09.2026, 224 of the 734 undertakings dropped were dropped between 1.95 and 2.25 minutes in
    /// — the moment <c>BotWill.DwellCapMs</c> stopped protecting them — and the bot took the same trade up
    /// again within ten minutes in four cases out of five: mining 85%, cooking 86%, peddling 81%. Thirty-one of
    /// the mining drops came on the walk to the fire with the ore already in the pack. Nothing about the work
    /// had changed at two minutes; the auction had simply started listening again, and the first offer worth
    /// half as much again took the bot off a trip it then started over from nothing.
    /// </para>
    ///
    /// <para>
    /// The research this follows is old and unambiguous. Kinny and Georgeff (IJCAI 1991) set agents that
    /// reconsider at every new opportunity against agents that never reconsider at all, and the first did
    /// worse than blind commitment in any world that changes more slowly than the agent works; a bold agent
    /// that reconsiders only when something happens to its own plan beat both of them everywhere. A seam, a
    /// fire and a counter change slowly. A creature does not, which is why hunting is left out of this.
    /// </para>
    ///
    /// <para>
    /// False by default, so that a new kind of work behaves as work always has until somebody decides that
    /// abandoning it is the defect. Answering true is a promise about <see cref="Resumed"/>: a clock inside
    /// the work that measures time since the last sign of progress must be put right when the work is taken
    /// up again, or the pause itself reads as a stall.
    /// </para>
    /// </summary>
    public virtual bool Steadfast => false;

    /// <summary>
    /// Whether this work is taken up again after an interruption even when the bot comes back hurt.
    ///
    /// <para>
    /// The resumption rule refuses a bot below half its health, because a miner should not go back to the
    /// seam bleeding. Mending is the one kind of work that a hurt bot should go straight back to — it was
    /// put down for a flight precisely because the bot was hurt — and without this it was settled as
    /// dropped every time: 29 self-bandagings "interrupted by flee" in half an hour on 28.09.2026, none of
    /// them taken up again, all of them counted against the finished share.
    /// </para>
    ///
    /// <para>
    /// <b>Peaceful work resumes hurt by default (29.09.2026).</b> The refusal was written for the seam, and it was
    /// costing the seam: in three hours of build 272, 85 pieces of work with nothing to fight in them — mining,
    /// cooking, peddling, unloading, restocking, reclaiming — were put down for a flight or a rescue and settled
    /// "not taken up again: came back with N of M health". A miner walking back to its rock at forty per cent is
    /// not walking into anything; the bandage is offered against the walk and takes it. So the default is now
    /// "nothing to fight and not braving anything", and work with a <see cref="Foe"/> or that <see cref="Braves"/>
    /// danger keeps the old rule.
    /// </para>
    /// </summary>
    public virtual bool ResumesHurt => Foe == null && !Braves;

    /// <summary>
    /// Whether this work is put down for an interruption and taken up again afterwards, rather than dropped.
    ///
    /// <para>
    /// <b>Steadfast was two promises in one word, and the second was the cheaper one to keep.</b> Steadfast work is
    /// held against ordinary offers <em>and</em> put down for what will not wait; fighting work must not be held —
    /// a creature changes faster than an auction — but a hunt interrupted by a rescue, a rescue by a flight and a
    /// corpse being gone through by anything are all things a bot should come back to. On build 272 about a
    /// hundred drops in three hours read "interrupted by flee" or "interrupted by rescue" against work that was not
    /// steadfast and so was thrown away. Defaults to <see cref="Steadfast"/>, so every steadfast deed resumes as
    /// before; a deed answers true here on its own to resume without being held.
    /// </para>
    /// </summary>
    public virtual bool Resumes => Steadfast;

    /// <summary>
    /// Whether this is a call from outside the bot's own business — somebody of ours in trouble, a wound to
    /// bind, the guild's muster, a war company, a lesson already paid for — and so allowed through the hold
    /// that keeps steadfast work from being outbid.
    ///
    /// <para>
    /// <b>The line between an event and an opportunity, which is the whole of rational commitment.</b> A
    /// miner offered a deer at twice the rate goes on mining; a miner whose neighbour is being set upon by an
    /// ogre does not, and the mining is put down rather than thrown away. Without this flag the hold would
    /// have kept a miner swinging at a rock while a reaper hit it, because "hitting back" is offered on the
    /// same rung as a better seam.
    /// </para>
    /// </summary>
    public virtual bool Summons => false;

    /// <summary>
    /// Whether work put down under this one keeps without <c>BotWill.AsideCapMs</c>'s clock: taken up again however long this lasted
    /// (07.10.2026, arena27). For a hold the bot has no say in and that is no work of its own — an entrant waiting at the arenas between
    /// its bouts, which runs past the ten minutes from round two on and would otherwise drop every winner's work. False by default.
    /// </summary>
    public virtual bool Shelters => false;

    /// <summary>
    /// Whether work put down for this one is kept for as long as this lasts, the aside clock (<c>BotWill.AsideCapMs</c>) running
    /// only from the moment this ends (12.10.2026, w2). For a company's call: the company's length is the company's, not the
    /// bot's, and on 09–11.10 every piece of work put down for an enlistment that outlasted the ten minutes was dropped "not
    /// taken up again after enlist" (3 472 in 38.7 h, 1 351 in 10.2 h — a third of all drops) while the same work put down for
    /// a shorter one was taken up again 95 % of the time and finished 90 % of the time. Unlike <see cref="Shelters"/> there is
    /// still a ceiling, <c>BotWill.KeptCapMs</c>, because a company that never breaks up is not a reason to go back to a seam
    /// six hours later. False by default.
    /// </summary>
    public virtual bool Keeps => false;

    /// <summary>
    /// Whether this work goes where the fighting is on purpose: a hunt, a prowl, a company's sweep or delve, the band, the
    /// harrowing, going through what was killed, plunder, escorting a fighter, scouting, and running away.
    ///
    /// <para>
    /// Asked by <see cref="BotAppraisal"/> before it keeps work out of ground where bots have been dying lately (see
    /// <see cref="BotPeril.Lethal"/>). Such ground is dealt with by exactly this work, so keeping it out would lock the
    /// remedy behind the condition it cures; everything else - a miner, a pedlar, a cook, a bot going back for its own
    /// corpse - is kept out until the ground has gone quiet. False by default, so that new work stays out of a killing
    /// field until somebody decides it belongs there.
    /// </para>
    /// </summary>
    public virtual bool Braves => false;

    /// <summary>
    /// The strength this work takes to where it goes: the bot, or the company it is in (<see cref="BotQuad.Strength"/>).
    /// Work that raises its own company on its first beat says what it will raise — see BotHarrow and BotProwl. Set against
    /// the hostile strength there by <see cref="BotPeril.Overwhelms"/>.
    /// </summary>
    public virtual double Brings(Mobile body) => BotQuad.Strength(body);

    /// <summary>
    /// The creature this work is closing on or fighting, or null for work that fights nothing in particular.
    ///
    /// <para>
    /// <b>One fight, two errands, and the second threw the first away.</b> <see cref="BotDefender"/> offers "hitting back
    /// at" whatever is hitting a bot, on the <c>Hunted</c> rung, and a hunt is hit by its own quarry at the first
    /// exchange — so the hunt was dropped "interrupted by rescue" for a rescue of the same fight, with the same
    /// <see cref="BotSlay"/> underneath and nothing gained but a drop and the kill filed under another trade. The
    /// defender asks this before it offers: work whose foe is the thing hitting the bot is already the answer.
    /// </para>
    /// </summary>
    public virtual Mobile Foe => null;

    /// <summary>
    /// Whether this work is, for the moment, work no rider may do, so the bot is to stay on foot while it lasts.
    ///
    /// <para>
    /// <b>The engine refuses a miner in the saddle and tells only a client.</b> <c>Mining.CheckHarvest</c> answers "You
    /// can't mine while riding" through <c>SendLocalizedMessage</c>, which reaches no bot and is not one of the harvest
    /// system's own sentences that <c>BotHeard</c> hears, so a gatherer that had bought a horse swung at full rock for
    /// the rest of the session and every rock read as a run of missed rolls. Asked by <c>BotStable.Ride</c>, which calls
    /// the horse up on every beat a rider is on foot. False by default: most work does not care.
    /// </para>
    /// </summary>
    public virtual bool Afoot => false;

    /// <summary>
    /// This work has just been chosen and is now the bot's. Called once, by <see cref="BotWill"/>, the moment it wins —
    /// never for an offer that lost, and not when put-down work is taken up again.
    ///
    /// <para>
    /// <b>For a place that can be spoken for, and an offer must not speak for it.</b> A proposer that reserves what it
    /// offers holds it for every bot that turns the offer down; a deed that reserves on its first beat reserves too late,
    /// because every idle bot is asked in the same step after a boot and all of them choose before any of them moves.
    /// Between the two is this: the population's auctions run one bot at a time, so a place taken here is seen by the
    /// very next proposer asked. See <see cref="BotAttend"/>.
    /// </para>
    /// </summary>
    public virtual void Taken(IBotWilful bot)
    {
    }

    /// <summary>
    /// This work was put down for something that would not wait and is being taken up again. Put right any
    /// clock that measures time since the last progress, and nothing else. See <see cref="Steadfast"/>.
    /// </summary>
    /// <summary>
    /// This work is being put down for something that would not wait, to be taken up again. Leave whatever is held
    /// for it held — the point of putting down rather than dropping — and tell whoever else stands in it. See
    /// <see cref="Steadfast"/>; the harrow uses it to bring the company back with the leader.
    /// </summary>
    public virtual void Paused(IBotWilful bot)
    {
    }

    public virtual void Resumed(IBotWilful bot)
    {
    }

    /// <summary>
    /// Why this work, put down and about to be taken up again, is no longer worth taking up — or null when it is (01.10.2026,
    /// build next). Asked by <c>BotWill</c> after its own refusals (death, the aside cap, health, another map); a reason given here
    /// settles the work not taken up again. A hunt whose quarry is far away by then is the case it was written for.
    /// </summary>
    public virtual string Stale(IBotWilful bot) => null;

    /// <summary>
    /// <b>An ending that is neither work done nor work failed, kept out of the finished share (01.10.2026, build next).</b> Read
    /// when the work ends Done: a levied enlistment its company let go before the member ever stood with it is no work at
    /// all, and on build 384 3495 of 3658 finished enlistments were such — "company N went on without it", 0.2–0.4 minutes
    /// after the press. Counted apart (<c>BotWill.KeptOut</c>), as self-defence is.
    /// </summary>
    public virtual bool KeptOut => false;

    /// <summary>
    /// This undertaking is over, however it ended. Release whatever was being held for it — a claimed vein,
    /// a reserved order — and nothing else; the takings are counted by the brain.
    /// </summary>
    public virtual void Drop(IBotWilful bot)
    {
    }

    /// <summary>
    /// Whether the bot should run while doing this, or walk.
    ///
    /// <para>
    /// <b>True for everything, and the one exception is the reason it exists.</b> Running is otherwise a
    /// property of the population — a setting read once, tempered only by whether the bot has the breath for
    /// it — and that is right for almost all of this: a bot crossing half a map to a vein, to a fight, to a
    /// counter is a bot whose errand is at the far end and whose walk is pure cost. But an errand whose whole
    /// content <em>is</em> the walking looks absurd taken at a sprint, and looking absurd is a real fault on a
    /// shard somebody watches from a client: a Baron sprinting laps of his own town reads as a bot with a bug,
    /// not as a bot at rest.
    /// </para>
    ///
    /// <para>
    /// Asked of the undertaking rather than set on the bot, because the bot is the wrong owner of the answer —
    /// it changes with what is being done and it has to change back by itself. See <c>BotMobile.Running</c>,
    /// which still has the last word: breath beats intent, and nothing here can make a tired bot run.
    /// </para>
    /// </summary>
    public virtual bool Hurries => true;

    /// <summary>Which stage it is on, in words, for the log. Null when there is nothing useful to say.</summary>
    public virtual string Stage => null;

    public override string ToString()
    {
        var stage = Stage;

        return stage == null ? Kind : $"{Kind}: {stage}";
    }
}
