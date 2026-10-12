using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Items;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// One bot as the debugger has actually seen it: everything here was measured by this file, on this file's
/// own clock, since the moment the debugger first laid eyes on the bot.
///
/// <para>
/// <b>Nothing is read out of a stamp the bot is carrying, and that is deliberate.</b> A bot holds several
/// tick stamps — when it took its work on, when its work last said anything but "working" — and every one
/// of them is unusable for this purpose the moment it has not been set: on some hosts the tick counter is
/// the machine's uptime and starts enormous, so an unseeded stamp does not read as "never", it reads as
/// "eleven days". A debugger built on those would report a population that had been frozen since before the
/// shard started. So the durations below are all of the form "for as long as I have been watching", which is
/// a smaller claim and a true one, and the prompt says so in those words.
/// </para>
///
/// <para>
/// <b>Every symptom carries the number it was raised on.</b> A row that says "stuck" is worth nothing; a row
/// that says "has not left 1441,1502 for 11 minutes while its journey wants 1502,1601, 40 tiles off" is a
/// defect report. This is the same rule the shard's own summaries are held to — a count with no denominator
/// and no unit is a sentence that cannot be checked — and it matters more here than anywhere else, because
/// what reads these rows is a language model, and a model handed an adjective will produce a paragraph of
/// adjectives back.
/// </para>
/// </summary>
public sealed class BotWatch
{
    /// <summary>How long a bot may stand on one tile, while its journey wants it elsewhere, before it counts.</summary>
    public static int FrozenMs { get; set; } = 90000;

    /// <summary>
    /// How long a piece of work may answer "I am working, here" before it counts as suspect.
    ///
    /// <para>
    /// <b>This is the shard's one unjudged answer, and the reason this file exists.</b> Walking is watched
    /// by the journey and finishing and failing end the work; <c>Work</c> means "standing here on purpose"
    /// and nothing questions it, which is correct for a smith at an anvil and is also how three tailors once
    /// held a bench for two hours on a craft lock the engine never released. Every summary counted them as
    /// working. Five minutes is long enough for any honest piece of standing work on this shard and short
    /// enough to catch that.
    /// </para>
    /// </summary>
    public static int ImmortalMs { get; set; } = 300000;

    /// <summary>An undertaking that ended sooner than this was not work; it was a bounce.</summary>
    public static int QuickMs { get; set; } = 15000;

    /// <summary>How long a bot must have been watched before "it has not improved" means anything.</summary>
    public static int SettledMs { get; set; } = 1200000;

    /// <summary>
    /// How long an errand must have been getting nowhere before dropping it counts as giving up.
    ///
    /// <para>
    /// <b>Without this the count is mostly interruptions.</b> A journey is legitimately pushed aside all the
    /// time — something starts hitting the bot, a company forms, a wound needs binding — and the errand
    /// underneath is still far away and still wanted. Requiring that it had also stopped making progress
    /// separates "it was pulled off this" from "it could not do this", and only the second is a defect.
    /// </para>
    /// </summary>
    public static int GaveUpQuietMs { get; set; } = 10000;

    /// <summary>
    /// How wide a patch of ground a bot may cover and still be said not to have gone anywhere, in tiles.
    ///
    /// Two: the tile it is on and its neighbours. Anything wider is a bot that has left, however slowly.
    /// </summary>
    public static int PacingSpan { get; set; } = 2;

    /// <summary>How long it must stay inside that patch, while moving, before it counts.</summary>
    public static int PacedMs { get; set; } = 120000;

    /// <summary>
    /// How many times it must change tile inside the patch before this is pacing rather than standing.
    ///
    /// <para>
    /// <b>It is the number that keeps this measure separate from being frozen.</b> A bot that has not moved
    /// at all is caught by <see cref="FrozenForMs"/> and is a different fault with a different cause; what
    /// this is for is the one that moves the whole time and therefore looks alive to everything else.
    /// </para>
    /// </summary>
    public static int PacingStirs { get; set; } = 3;

    /// <summary>How long a fight may go on with nothing losing health before it is not a fight.</summary>
    public static int FutileMs { get; set; } = 20000;

    private sealed class Trade
    {
        public int Taken;

        public int Quick;

        public long HeldMs;

        /// <summary>
        /// Gold the bot was worth when it let this trade go, less what it was worth when it took it on.
        ///
        /// <para>
        /// <b>The one number that says whether a trade is a trade.</b> Everything else about crafting and
        /// trading on this shard is a count of attempts, and a trade attempted forty times is
        /// indistinguishable from a trade that works until somebody asks what came of it. Negative is a real
        /// and important answer: buying cloth and never selling what is sewn is a trade that costs the
        /// population money every time it is chosen, and it looks busy from every other angle.
        /// </para>
        /// </summary>
        public int Gained;

        /// <summary>
        /// Goods the work itself says it made, as the undertaking counts them.
        ///
        /// <para>
        /// <b>Without this column the gold column libels half the shard.</b> Buying is work here — restock,
        /// acquire, bullion, buying cloth to sew — and every one of those turns coin into goods on purpose,
        /// so measured in coin alone they are losses, every time, by design. On the night of 01.09.2026 the
        /// debugger read the negative rows, generalised, and concluded the shard was suffering "widespread
        /// economic loss leading to deflation" while it was in fact up some twenty-eight thousand gold. Two
        /// facts, two columns, and neither one summed into the other.
        /// </para>
        /// </summary>
        public int Made;

        /// <summary>
        /// Skill the bot gained while holding this work, in tenths of a point as the engine counts.
        ///
        /// <para>
        /// <b>The column that answers the question this whole debugger exists for.</b> Gold says what a bot
        /// has and goods say what it made; neither says whether it is becoming anything. Measured
        /// 02.09.2026: of 397 pieces of work settled in half an hour, 64 taught anything at all — and the
        /// spread was not even close to even. A lesson at the training field gave 3.6 points a go; prowling
        /// gave nought over 83 goes and was the most-taken work on the shard. Without this column that is
        /// invisible, and "fifteen of thirty-eight have not improved in twenty minutes" has no cause
        /// attached to it.
        /// </para>
        /// </summary>
        public int Learned;
    }

    private readonly Dictionary<string, Trade> _trades = [];

    private Point3D _was;

    /// <summary>The patch of ground it has kept to lately, and how often it has changed tile inside it.</summary>
    private int _lowX;

    private int _highX;

    private int _lowY;

    private int _highY;

    private long _patchSince;

    private int _stirs;

    private bool _patched;

    private Point3D _stirred;

    private Mobile _foe;

    private int _foeHits;

    /// <summary>Where the journey was pointed on the last sample, and the closest it has got to it.</summary>
    private Point3D _goal;

    private long _goalSince;

    private bool _going;

    private int _closest;

    private int _slack = 1;

    private string _kind = "-";

    private long _kindSince;

    private int _kindWorth;

    private int _kindMade;

    private int _kindSkill;

    /// <summary>The undertaking itself, kept so that what it made can be read after it has been replaced.</summary>
    private BotDeed _deed;

    private long _stillSince;

    private bool _seen;

    public BotWatch(BotMobile bot, long now)
    {
        Bot = bot;
        Name = bot.Name;
        Class = bot.Class?.Name ?? "no class";
        FirstTick = now;
        _kindSince = now;
        _stillSince = now;
        _was = bot.Location;
        FirstWorth = Coin(bot);
        FirstProgress = bot.Progress;
    }

    public BotMobile Bot { get; }

    public string Name { get; private set; }

    public string Class { get; }

    /// <summary>When the debugger first saw this bot. Every duration below is measured from here at the earliest.</summary>
    public long FirstTick { get; }

    /// <summary>Gold in pack and bank when it was first seen, and now. The pair, never the difference alone.</summary>
    public int FirstWorth { get; }

    public int Worth { get; private set; }

    /// <summary>
    /// The purse and the bank, apart, because on this shard they are not the same money.
    ///
    /// <para>
    /// <b>Merged, they cannot answer the one question worth asking about a bot's money.</b> The engine pays
    /// out of the pack; the bank buys nothing until the bot has walked to one. So a population holding
    /// sixty thousand gold between it can be refused every purchase it attempts, and a single "worth" column
    /// reads as wealth while the shard behaves as though it were destitute. On 02.09.2026 the armourer
    /// refused 848 fittings for want of coin while this file reported an average of sixteen hundred a bot,
    /// and neither number was wrong — they were about different money, and there was no way to tell from
    /// here. Two columns.
    /// </para>
    /// </summary>
    public int Pack { get; private set; }

    public int Bank { get; private set; }

    /// <summary>Everything it knows, in tenths of a point, as the engine sums its skills.</summary>
    public int Skill { get; private set; }

    public double FirstProgress { get; }

    public double Progress { get; private set; }

    public double Mood { get; private set; }

    public string Standing { get; private set; } = "-";

    public string Kind => _kind;

    /// <summary>How long the undertaking in hand has been held, as observed.</summary>
    public long HeldMs { get; private set; }

    /// <summary>
    /// How long it has held no undertaking at all, as observed. Nought while it has work.
    ///
    /// <para>
    /// <b>The state every other measure here was written to ignore, and it is the commonest one.</b> The
    /// roll-call refuses to call a bot stuck unless it is stuck ON something, which is right as far as it
    /// goes: there is nothing to remind or shake in a bot that holds nothing. But a category excluded from
    /// every judgement is a category nobody counts, and on 02.09.2026 it was 28 of 38 — while the roll-call
    /// answered "one bot answered no to everything". The shard's own stall detector had meanwhile fired for
    /// 29 different bots in an hour, every one of them holding the word "nothing", one of them for sixteen
    /// minutes. Two instruments describing the same population, one of them blind by construction.
    /// </para>
    ///
    /// <para>
    /// Reported and not acted on. A bot with no work has nothing to be bent or ended; whatever is wrong is
    /// on the side that offers work, and a hand laid on the bot could only make the measurement worse.
    /// </para>
    /// </summary>
    /// <para>
    /// <b>A bot in a company is never counted here, and that exclusion was bought like the others.</b> The
    /// Bound rung switches the auction off: a squad member is driven by its company, so holding no
    /// undertaking is its ordinary and correct state. On 02.09.2026 two healers were reported idle for 138
    /// seconds each while they were walking through Britain Graveyard behind a squad that was killing a
    /// wraith — asked directly, one of them was "walking to 1386,1480, set out 0s ago" and had moved two
    /// tiles since the count was taken. Same family as the melee false positive and the chasing one: work
    /// that something else is driving looks like no work at all from here.
    /// </para>
    public long IdleMs =>
        _kind == "-" && !string.Equals(Standing, "Bound", StringComparison.Ordinal) ? HeldMs : 0;

    /// <summary>How long it has stood on the same tile, as observed.</summary>
    public long StillMs { get; private set; }

    /// <summary>How much of that standing was done while its own journey wanted it somewhere else.</summary>
    public long FrozenForMs { get; private set; }

    /// <summary>How long its work has answered "working, here" without the debugger seeing anything else.</summary>
    public long WorkingForMs { get; private set; }

    /// <summary>Undertakings seen begin, and how many of them ended inside <see cref="QuickMs"/>.</summary>
    public int Deeds { get; private set; }

    /// <summary>
    /// How many separate pieces of work this bot has taken on, ever.
    ///
    /// <para>
    /// <b>Kept because the name of the work cannot answer "is this the same one".</b> A bot that finishes an
    /// unload and takes another unload has the same Kind in both samples, so anything comparing kinds reads
    /// it as never having finished. See BotAudit, which drew exactly that conclusion and threw a route away
    /// over it on 03.09.2026 at 05:56, about a bot 26 seconds into its work.
    /// </para>
    /// </summary>
    public long Takes { get; private set; }

    public int Quick { get; private set; }

    /// <summary>Roads the shard has refused it, as the bot itself counts them.</summary>
    public int Refusals { get; private set; }

    /// <summary>Samples in which its journey had given up on ever reaching anything.</summary>
    public int Hopeless { get; private set; }

    /// <summary>
    /// How long it has been walking to the same place without ever getting a tile closer to it.
    ///
    /// <para>
    /// <b>A different failure from standing still, and the first version of this file could not see it at
    /// all.</b> A bot pinned against a wall is measured by <see cref="FrozenForMs"/>; a bot pacing a
    /// courtyard, taking a step every beat and ending each one no nearer than it started, moves constantly
    /// and looks perfectly healthy by every measure here. The shard's own walk layer knows about it — it
    /// prints "could not get one tile closer in 8 plans and has dropped it" — but by the time that line is
    /// written the errand is already gone, so a sampler at two seconds sees a clean journey before and a
    /// clean journey after and nothing in between. Measured here, by me, against the closest this bot has
    /// ever been to the place it says it is going.
    /// </para>
    /// </summary>
    public long NoCloserMs { get; private set; }

    /// <summary>
    /// How long it has been going to the place it is going to.
    ///
    /// <para>
    /// <b>Without it the closest-approach figure is a trap, and it caught the debugger on its first
    /// afternoon.</b> An errand three seconds old has been no nearer than it is now, because it has not had
    /// time to be — so the row read "walking to a place 103 tiles off, closest it has been is 103", which
    /// says "it has never got closer" in the same words whether the errand is three seconds old or an hour.
    /// The model read the first as the second and reported Alden as unable to reach its destination, twenty
    /// seconds before Alden finished that piece of work with 494 gold, 52 ingots and the largest gain in
    /// skill on the shard that hour. The figure was true. It could not mean anything yet, and the row did
    /// not say so.
    /// </para>
    /// </summary>
    public long GoingMs { get; private set; }

    /// <summary>How long an errand must have run before its closest approach is worth quoting at all.</summary>
    public static int WorthSayingMs { get; set; } = 20000;

    /// <summary>
    /// How long it has been moving about inside a patch two tiles across without leaving it.
    ///
    /// <para>
    /// <b>The third way of getting nowhere, and the only one that looks like health from every other
    /// angle.</b> A bot that cannot take a step is caught by <see cref="FrozenForMs"/>; a bot walking to a
    /// place it never nears is caught by <see cref="NoCloserMs"/>; a bot stepping back and forth between two
    /// tiles is doing neither. It moves every beat, so nothing that watches for stillness sees it. It may
    /// have no destination at all, so nothing that watches distance sees it either. Every summary on the
    /// shard counts it as a bot going about its business.
    /// </para>
    ///
    /// <para>
    /// Measured as a box rather than as "A then B then A", because the sampler runs every two seconds and a
    /// bot takes ten steps in that time: what an oscillation of two tiles looks like at this rate is not an
    /// alternation, it is a position that keeps landing inside the same small square. The box is
    /// sample-rate independent and the alternation is not.
    /// </para>
    /// </summary>
    public long PacingMs { get; private set; }

    /// <summary>How wide the patch it has kept to is, in tiles, and how often it has changed tile inside it.</summary>
    public int Patch { get; private set; } = 1;

    public int Stirs => _stirs;

    /// <summary>What it is fighting this moment, or null.</summary>
    public string Foe { get; private set; }

    /// <summary>How far off that thing is, and how far above or below it stands.</summary>
    public int FoeAway { get; private set; }

    public int FoeHigh { get; private set; }

    /// <summary>How far this bot's weapon actually reaches, as the engine has it.</summary>
    public int Reach { get; private set; } = 1;

    /// <summary>
    /// How long it has been in a fight with the same creature without that creature's health falling once.
    ///
    /// <para>
    /// <b>This is the only measure here that can tell a fight from the appearance of one.</b> Warmode, a
    /// combatant and a bot swinging on the spot look identical whether the blows are landing or not, and
    /// every summary on the shard counts both as fighting. The health of the thing being hit is the only
    /// witness that cannot be fooled: if it has not moved in twenty seconds, nothing is happening, whatever
    /// the bot thinks it is doing.
    /// </para>
    /// </summary>
    public long SwingingMs { get; private set; }

    /// <summary>
    /// Whether the thing it is fighting is somewhere it cannot be hit from.
    ///
    /// <para>
    /// <b>Two separate ways, and the second is the one nothing else on this shard looks for.</b> Further than
    /// the weapon reaches is ordinary and cures itself in a step or two. More than a person's height apart in
    /// the third dimension does not: that is a creature on a roof, on a floor above, or on a bridge over the
    /// bot's head, and the bot can see it, has chosen it, will walk under it and swing at it for as long as
    /// anything lets it. The fork's own movement notes record five bots forming a party for a wraith on a
    /// crypt roof, three tiles away and twenty units up, walking over and taking off not one point of health.
    /// Sixteen units is the engine's own idea of one floor — see BotArrival.PersonHeight, which is where this
    /// number comes from rather than being chosen here.
    /// </para>
    /// </summary>
    public bool BeyondReach { get; private set; }

    public bool Overhead { get; private set; }

    /// <summary>Whether it has something to fight at all. Read by the roll-call, which must not judge a fight.</summary>
    public bool Fighting => Foe != null;

    /// <summary>
    /// Whether the errand in hand is chasing something that moves.
    ///
    /// <para>
    /// The roll-call asks whether a bot reached the place it was going two minutes ago. For an errand that
    /// follows a creature there is no such place: the destination is wherever that creature now stands, and
    /// measuring the bot against where it stood two minutes ago asks whether the bot caught up with the past.
    /// </para>
    /// </summary>
    public bool Following { get; private set; }

    /// <summary>
    /// Errands given up while the bot was still further from them than arriving would have needed.
    ///
    /// <para>
    /// The other half of the same fact, and it is the countable half: getting nowhere is a duration and can
    /// be argued about, whereas "it wanted to be there, it was fourteen tiles short, and it stopped wanting"
    /// is an event. Three of those about the same bot is the shape of an errand nothing can carry out.
    /// </para>
    /// </summary>
    public int Abandoned { get; private set; }

    /// <summary>How far short it was when it last gave one up. For the phrase, so the number is in it.</summary>
    public int ShortBy { get; private set; }

    /// <summary>Minutes it has spent with nothing on the shard worth doing.</summary>
    public double BarrenMinutes { get; private set; }

    /// <summary>Samples in which it was a ghost.</summary>
    public double DeadMinutes { get; private set; }

    public Point3D Where { get; private set; }

    public string Region { get; private set; } = "-";

    /// <summary>Where its journey wants it, and how far that is. Empty when it is not going anywhere.</summary>
    public Point3D Wants { get; private set; }

    public int WantsAway { get; private set; }

    /// <summary>How near the errand in hand counts as having arrived. Read off the errand, never assumed.</summary>
    public int Slack => _slack;

    public string Why { get; private set; }

    /// <summary>Why it holds nothing, in the auction's own two numbers. Null while it holds work.</summary>
    public string Empty { get; private set; }

    /// <summary>What its work last asked of it: walk, work, done, failed.</summary>
    public string Asked { get; private set; } = "-";

    /// <summary>How suspicious this bot is, and the phrases that made it so. Built fresh on every sample.</summary>
    public double Suspicion { get; private set; }

    public string Symptoms { get; private set; } = "";

    /// <summary>How long it has been watched.</summary>
    public long WatchedMs(long now) => now - FirstTick;

    private static int Coin(BotMobile bot) => (bot.Backpack?.TotalGold ?? 0) + Banker.GetBalance(bot);

    private static (int Pack, int Bank) Purse(BotMobile bot) =>
        (bot.Backpack?.TotalGold ?? 0, Banker.GetBalance(bot));

    /// <summary>One sample. Everything in this class is written here and read everywhere else.</summary>
    public void Sample(long now, long sinceMs)
    {
        var bot = Bot;

        if (bot is not { Deleted: false })
        {
            return;
        }

        Name = bot.Name;
        Where = bot.Location;
        Region = bot.Region?.Name ?? "nowhere";
        (Pack, Bank) = Purse(bot);
        Worth = Pack + Bank;
        Skill = bot.SkillsTotal;
        Progress = bot.Progress;
        Mood = bot.Mood;

        var resolve = bot.Resolve;
        var journey = bot.Journey;

        Standing = resolve?.Standing.ToString() ?? "-";
        Why = resolve?.Because;
        Empty = resolve?.Empty;
        Refusals = bot.Refusals;

        if (!bot.Alive)
        {
            DeadMinutes += sinceMs / 60000.0;
        }

        if (resolve?.Urges is { IsBarren: true })
        {
            BarrenMinutes += sinceMs / 60000.0;
        }

        // What the work asked for last. A struct, so an untouched one reads None rather than throwing, and
        // None is a legitimate state: a bot with nothing on has not been asked for anything.
        var sent = resolve?.Sent ?? default;

        Asked = sent.Kind == BotDoingKind.None ? "-" : sent.Kind.ToString().ToLowerInvariant();

        var wants = journey is { Active: true };

        Following = wants && journey.Current?.Follow != null;

        Wants = wants ? journey.Target : Point3D.Zero;
        WantsAway = wants ? (int)bot.GetDistanceToSqrt(journey.Target) : 0;

        if (journey is { Hopeless: true })
        {
            Hopeless++;
        }

        Fight(bot, sinceMs);

        Travel(now, wants, sinceMs, journey);

        // After Travel, never before: the arrival slack it needs is read off the errand in there, and a bot
        // standing beside the thing it came for must not be called a pacer.
        Pace(now, wants);

        // Standing still, and standing still *while something wants it elsewhere*, counted apart. The first
        // is ordinary — most work on this shard is done standing — and only the second is a symptom.
        if (bot.Location == _was)
        {
            StillMs = now - _stillSince;

            // The same guard as in Travel, and for the same reason: a bot that has arrived and is standing
            // beside the thing it came for is not frozen, however long it stands there. Only distance still
            // to cover makes standing still a symptom.
            if (wants && journey.Walking && WantsAway > Math.Max(1, journey.Arrival.Tiles))
            {
                FrozenForMs += sinceMs;
            }
        }
        else
        {
            _was = bot.Location;
            _stillSince = now;
            StillMs = 0;
            FrozenForMs = 0;
        }

        // The undertaking in hand, by name, and the changes counted. The name is used rather than the object
        // so that a proposer handing out a fresh instance of the same work every review shows up as what it
        // is — the same trade over and over — instead of as thirty different jobs.
        // <b>The undertaking itself, not its name, and the difference is two undercounts.</b> Comparing
        // names misses a bot that finishes a dig and immediately takes another — the commonest thing a
        // gatherer does all day — so both the count and everything derived from it were low. And holding the
        // object is what makes the goods column possible at all: a deed's tally of what it made is filled in
        // on its last leg, which a sampler at two seconds will usually miss, but the object still holds the
        // figure after it has been settled and replaced. Read there, "inscribe: 240 made" stops being
        // "inscribe: 0 made".
        var work = resolve?.Deed;
        var kind = work?.Kind ?? "-";

        if (!_seen)
        {
            _seen = true;
            Takes++;
            _kind = kind;
            _kindSince = now;
            _kindWorth = Worth;
            _kindSkill = Skill;
        }
        else if (!ReferenceEquals(work, _deed))
        {
            var held = now - _kindSince;

            if (!string.Equals(_kind, "-", StringComparison.Ordinal))
            {
                var trade = Note(_kind);

                trade.Taken++;
                trade.HeldMs += held;
                trade.Gained += Worth - _kindWorth;
                trade.Made += _deed?.Made ?? _kindMade;
                trade.Learned += Skill - _kindSkill;

                Deeds++;
                Takes++;

                if (held < QuickMs)
                {
                    trade.Quick++;
                    Quick++;
                }
            }

            _kind = kind;
            _kindSince = now;
            _kindWorth = Worth;
            _kindMade = 0;
            _kindSkill = Skill;
            WorkingForMs = 0;
        }

        _deed = work;
        HeldMs = now - _kindSince;

        // Still sampled as a fallback, for the case where the object has gone before the change was noticed.
        _kindMade = work?.Made ?? _kindMade;

        // Time spent answering "working, here". Reset by any other answer, which is what makes it a measure
        // of silence rather than of standing.
        if (sent.Kind == BotDoingKind.Work)
        {
            WorkingForMs += sinceMs;
        }
        else
        {
            WorkingForMs = 0;
        }

        Weigh(now);
    }

    /// <summary>
    /// Whether it is getting anywhere, and whether it gave up on getting there.
    ///
    /// <para>
    /// Measured against <em>the closest it has ever been</em> to the place, not against the last sample. A
    /// bot that walks four tiles forward and four back gets no closer, and a comparison with the previous
    /// reading calls that progress every other beat.
    /// </para>
    ///
    /// <para>
    /// Giving up is judged against the errand's own arrival slack rather than against zero: several kinds of
    /// work here are done from two or three tiles away and finish there on purpose, so a bot that stops
    /// wanting a place while standing beside it has arrived, not abandoned it.
    /// </para>
    /// </summary>
    private void Travel(long now, bool wants, long sinceMs, BotJourney journey)
    {
        var goal = wants ? journey.Target : Point3D.Zero;

        // The errand ends when the journey stops or when it points somewhere else — and it must be both,
        // because giving up pops only the current errand and leaves whatever was queued behind it running.
        // Watching for the journey to go quiet would therefore have missed exactly the case this exists for.
        if (_going && (!wants || goal != _goal))
        {
            _going = false;

            if (_closest > _slack && NoCloserMs >= GaveUpQuietMs)
            {
                Abandoned++;
                ShortBy = _closest;
            }
        }

        if (!wants)
        {
            NoCloserMs = 0;
            GoingMs = 0;

            return;
        }

        _slack = Math.Max(1, journey.Arrival.Tiles);

        GoingMs = _going ? now - _goalSince : 0;

        if (!_going)
        {
            _going = true;
            _goal = goal;
            _goalSince = now;
            _closest = WantsAway;
            NoCloserMs = 0;
        }
        else if (WantsAway < _closest)
        {
            _closest = WantsAway;
            NoCloserMs = 0;
        }
        else if (WantsAway > _slack)
        {
            NoCloserMs += sinceMs;
        }
        else
        {
            // <b>It is standing where it wanted to stand, and the first version of this counter called that
            // getting nowhere.</b> Work that follows a moving thing — a rescue, a hunt, an escort — points
            // the journey at the target's own tile every beat, so a bot in melee is permanently one tile off
            // and permanently not getting closer. On 01.09.2026 that produced a report about Nessa "walking
            // to 1369,1309 for 46 seconds and never getting closer than 1 tile", which was true in every
            // word: she was next to the thing, hitting it, and finished the piece of work a minute later
            // with 111 gold and 24 leather. Being no nearer than arriving needed is arriving.
            NoCloserMs = 0;
        }
    }

    /// <summary>
    /// Whether it has been wearing a patch of ground out instead of going anywhere.
    ///
    /// <para>
    /// The box grows to hold wherever the bot has been. The moment it would grow wider than
    /// <see cref="PacingSpan"/> the bot has plainly left, and the box starts again from where it now stands
    /// — so this is never a claim about a bot that walked away and came back an hour later, only about one
    /// that has not left at all.
    /// </para>
    ///
    /// <para>
    /// <b>A bot that has arrived is excluded, and that exclusion was bought once already.</b> Work that
    /// follows something moving keeps the journey pointed at its target's own tile, so two bots trading
    /// blows shuffle around a couple of tiles for the whole fight — which is what winning looks like from
    /// outside, and which this would otherwise report every time. See the note in <see cref="Travel"/>: the
    /// same mistake, caught the same way.
    /// </para>
    /// </summary>
    private void Pace(long now, bool wants)
    {
        var at = Where;

        if (!_patched)
        {
            Fresh(at, now);

            return;
        }

        var lowX = Math.Min(_lowX, at.X);
        var highX = Math.Max(_highX, at.X);
        var lowY = Math.Min(_lowY, at.Y);
        var highY = Math.Max(_highY, at.Y);

        if (highX - lowX >= PacingSpan || highY - lowY >= PacingSpan)
        {
            Fresh(at, now);

            return;
        }

        _lowX = lowX;
        _highX = highX;
        _lowY = lowY;
        _highY = highY;

        Patch = Math.Max(highX - lowX, highY - lowY) + 1;

        if (at != _stirred)
        {
            _stirs++;
            _stirred = at;
        }

        var arrived = wants && WantsAway <= _slack;

        PacingMs = _stirs >= PacingStirs && !arrived ? now - _patchSince : 0;
    }

    private void Fresh(Point3D at, long now)
    {
        _patched = true;
        _lowX = _highX = at.X;
        _lowY = _highY = at.Y;
        _stirred = at;
        _patchSince = now;
        _stirs = 0;
        Patch = 1;
        PacingMs = 0;
    }

    /// <summary>
    /// What it is fighting, whether it can reach the thing, and whether the thing is losing any health.
    /// </summary>
    private void Fight(BotMobile bot, long sinceMs)
    {
        var foe = bot.Combatant;

        if (foe is not { Deleted: false, Alive: true } || foe.Map != bot.Map)
        {
            _foe = null;
            Foe = null;
            FoeAway = 0;
            FoeHigh = 0;
            SwingingMs = 0;
            BeyondReach = false;
            Overhead = false;

            return;
        }

        if (!ReferenceEquals(foe, _foe))
        {
            _foe = foe;
            _foeHits = foe.Hits;
            SwingingMs = 0;
        }

        Foe = foe.Name;
        FoeAway = (int)bot.GetDistanceToSqrt(foe.Location);
        FoeHigh = Math.Abs(bot.Z - foe.Z);
        Reach = Math.Max(1, bot.Weapon?.MaxRange ?? 1);

        BeyondReach = FoeAway > Reach;
        Overhead = FoeHigh >= BotArrival.PersonHeight;

        // Its health falling is the proof that something is happening. Anything else — warmode, a combatant,
        // a bot swinging on the spot — is true of a fight that is not a fight.
        if (foe.Hits < _foeHits)
        {
            _foeHits = foe.Hits;
            SwingingMs = 0;

            return;
        }

        SwingingMs += sinceMs;
    }

    /// <summary>What this bot has made of each trade it has held. Read by the population's own tally.</summary>
    public void Tally(Dictionary<string, (int Taken, int Quick, long HeldMs, int Gained, int Made, int Learned, int Bots)> into)
    {
        if (into == null)
        {
            return;
        }

        foreach (var (kind, trade) in _trades)
        {
            var was = into.GetValueOrDefault(kind);

            into[kind] = (
                was.Taken + trade.Taken,
                was.Quick + trade.Quick,
                was.HeldMs + trade.HeldMs,
                was.Gained + trade.Gained,
                was.Made + trade.Made,
                was.Learned + trade.Learned,
                was.Bots + 1
            );
        }
    }

    private Trade Note(string kind)
    {
        if (_trades.TryGetValue(kind, out var trade))
        {
            return trade;
        }

        trade = new Trade();
        _trades[kind] = trade;

        return trade;
    }

    /// <summary>
    /// How badly this bot wants looking at, and why in words with numbers in them.
    ///
    /// <para>
    /// <b>A sum of separately named symptoms, never one score.</b> The score decides who the debugger stands
    /// next to and whose row goes in front of the model; the phrases are what the finding has to be made out
    /// of. A single number would rank the population correctly and would be unable to say anything about any
    /// member of it, which is the failure mode of every "health score" ever written.
    /// </para>
    /// </summary>
    private void Weigh(long now)
    {
        var sb = ValueStringBuilder.Create(256);
        var score = 0.0;

        try
        {
            var said = 0;

            if (FrozenForMs >= FrozenMs)
            {
                score += Math.Min(3.0, FrozenForMs / (double)FrozenMs);

                Say(ref sb, ref said, $"has not left {Where.X},{Where.Y} for {Minutes(FrozenForMs)} while walking to {Wants.X},{Wants.Y}, {WantsAway} tiles off");
            }

            // Named apart from being frozen on purpose: this bot is moving, and every other measure here
            // says it is fine. It is the walking version of the same defect and it is the one that hides.
            if (NoCloserMs >= FrozenMs)
            {
                score += Math.Min(3.0, NoCloserMs / (double)FrozenMs);

                Say(ref sb, ref said, $"has been walking to {Wants.X},{Wants.Y} for {Minutes(NoCloserMs)} and has never got closer than {_closest} tiles, standing {WantsAway} off now");
            }

            // Weighted heavily on purpose. It is the loudest thing a bot can do that nothing else on the
            // shard notices at all, and it has never once been an innocent state.
            if (PacingMs >= PacedMs)
            {
                score += Math.Min(3.0, PacingMs / (double)PacedMs + 1.0);

                Say(
                    ref sb,
                    ref said,
                    $"has spent {Minutes(PacingMs)} treading a patch {Patch} tiles across at {Where.X},{Where.Y}, changing tile {Stirs} times without once leaving it"
                );
            }

            // <b>The heaviest score here, because it is the population's commonest failure and it scored
            // NOTHING until 02.09.2026.</b> Idleness was measured, counted and printed in the aggregate for
            // a whole day — "20 of 38 have held no work for more than three minutes" — and never once
            // reached a finding: the rows the model reasons from are ranked by this score, an idle bot
            // scored zero, so no idle bot was ever put in front of it. Thirty-two findings that day, none
            // about idling, while half the shard stood still. A measurement that nothing ranks by is a
            // measurement nobody acts on, and the person watching the world could see it from the ground
            // when the watcher could not.
            if (IdleMs >= BotVigil.LoiterMs)
            {
                score += Math.Min(4.0, 2.0 + IdleMs / (double)BotVigil.LoiterMs);

                Say(
                    ref sb,
                    ref said,
                    $"has held NO WORK AT ALL for {Minutes(IdleMs)} on the {Standing} rung at {Where.X},{Where.Y} with {Pack}gp in its pack — nothing is being offered to it"
                );
            }

            if (SwingingMs >= FutileMs)
            {
                score += 2.5;

                var why = Overhead
                    ? $"it is {FoeHigh} units of height away from it, which is more than a floor — that thing is on a roof or an upper storey and cannot be hit at all"
                    : BeyondReach
                        ? $"it is {FoeAway} tiles off and this bot's weapon reaches {Reach}"
                        : "it is beside it and in reach, so something else is stopping the blows";

                Say(ref sb, ref said, $"has been fighting {Foe} for {Minutes(SwingingMs)} without its health falling once: {why}");
            }

            if (Abandoned >= 3)
            {
                score += Math.Min(2.0, Abandoned / 3.0);

                Say(ref sb, ref said, $"has given up {Abandoned} errands while still short of them, the last one {ShortBy} tiles out");
            }

            if (WorkingForMs >= ImmortalMs)
            {
                score += Math.Min(3.0, WorkingForMs / (double)ImmortalMs);

                Say(ref sb, ref said, $"its {_kind} has answered \"working, here\" and nothing else for {Minutes(WorkingForMs)}");
            }

            if (Quick >= 4)
            {
                score += Math.Min(2.5, Quick / 4.0);

                Say(ref sb, ref said, $"{Quick} of its {Deeds} undertakings ended inside {QuickMs / 1000}s");
            }

            // <b>Which trade it is bouncing off, by name, and this is the row that names a defect rather
            // than a symptom.</b> "Nine undertakings went nowhere" is a bot having a bad afternoon; "it has
            // taken Peddler eleven times and nine of them were over in twelve seconds" is a proposer that
            // offers work it cannot do, which is a loop between two files and is the commonest shape of
            // defect this shard has produced. The pair of numbers is the finding — one of them alone says
            // nothing.
            var loop = Worst();

            if (loop != null)
            {
                score += 2.0;

                Say(ref sb, ref said, loop);
            }

            if (Refusals >= 4)
            {
                score += Math.Min(2.0, Refusals / 6.0);

                Say(ref sb, ref said, $"{Refusals} roads refused in a row without a step between them");
            }

            if (Hopeless > 0)
            {
                score += 1.0;

                Say(ref sb, ref said, $"its journey has given up on reaching anything {Hopeless} times");
            }

            if (BarrenMinutes >= 5.0)
            {
                score += Math.Min(2.0, BarrenMinutes / 10.0);

                Say(ref sb, ref said, $"{BarrenMinutes:F0} minutes with nothing on the shard worth doing");
            }

            if (DeadMinutes >= 3.0)
            {
                score += 2.0;

                Say(ref sb, ref said, $"a ghost for {DeadMinutes:F0} minutes and not back on its feet");
            }

            var watched = WatchedMs(now);

            if (watched >= SettledMs && Progress <= FirstProgress + 0.0005)
            {
                score += 1.5;

                Say(ref sb, ref said, $"its trade has not moved off {Progress:P0} in the {Minutes(watched)} it has been watched");
            }

            if (watched >= SettledMs && Worth <= FirstWorth)
            {
                score += 1.0;

                Say(ref sb, ref said, $"worth {Worth}gp against {FirstWorth}gp when first seen, {Minutes(watched)} ago");
            }

            Symptoms = sb.ToString();
            Suspicion = score;
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>
    /// The trade this bot takes and drops most, or null if it is not doing that to any of them.
    ///
    /// Three is the floor rather than two, because two of anything is a coincidence and this phrase goes
    /// into a prompt as a claim about a defect.
    /// </summary>
    private string Worst()
    {
        string worst = null;
        var quick = 0;
        var taken = 0;

        foreach (var (kind, trade) in _trades)
        {
            if (trade.Quick > quick)
            {
                worst = kind;
                quick = trade.Quick;
                taken = trade.Taken;
            }
        }

        return quick < 3
            ? null
            : $"it has taken {worst} {taken} times and {quick} of those were over inside {QuickMs / 1000}s, averaging {_trades[worst].HeldMs / Math.Max(1, taken) / 1000}s a go";
    }

    private static void Say(ref ValueStringBuilder sb, ref int said, string phrase)
    {
        if (said++ > 0)
        {
            sb.Append("; ");
        }

        sb.Append(phrase);
    }

    private static string Minutes(long ms) =>
        ms < 90000 ? $"{ms / 1000}s" : $"{ms / 60000}m";

    /// <summary>
    /// One row for the model: who, where, what it is doing, and every measure that has a number.
    ///
    /// Written as sentences with units named. See the note at the top of this file, and the harder-won one
    /// at the top of <c>BotMindSight</c>: the first version of a prompt like this said "39 of 215 stones"
    /// and the model went to market to sell the bot's weight.
    /// </summary>
    public string Row(long now)
    {
        var sb = ValueStringBuilder.Create(512);

        try
        {
            sb.Append("- ");
            sb.Append(Name);
            sb.Append(" the ");
            sb.Append(Class);
            sb.Append(": on the ");
            sb.Append(Standing);
            sb.Append(" rung, holding ");
            sb.Append(_kind == "-" ? "nothing at all" : _kind);
            sb.Append(" for ");
            sb.Append(Minutes(HeldMs));
            sb.Append(", which last asked it to ");
            sb.Append(Asked);
            sb.Append(". At ");
            sb.Append(Where.X);
            sb.Append(",");
            sb.Append(Where.Y);
            sb.Append(" in ");
            sb.Append(Region);

            if (WantsAway > 0)
            {
                sb.Append(", walking to ");
                sb.Append(Wants.X);
                sb.Append(",");
                sb.Append(Wants.Y);
                sb.Append(" (");
                sb.Append(WantsAway);
                sb.Append(" tiles off, set out ");
                sb.Append(Minutes(GoingMs));
                sb.Append(" ago");

                // <b>The closest approach is quoted only once it can mean something.</b> An errand a few
                // seconds old has never been nearer than it is, because it has not had time to be — and
                // "closest it has been is 103" says exactly the same words about a bot that set out three
                // seconds ago and about one that has been failing for an hour. On 01.09.2026 that cost a
                // false report: Alden was called unable to reach its destination twenty seconds before it
                // finished that piece of work with 494 gold and the best gain in skill on the shard that
                // hour. This shard already has the rule and it is written down twice elsewhere — a window
                // too short to have a rate in it is reported as what it was and not divided into one. Same
                // rule, third place.
                if (NoCloserMs >= WorthSayingMs)
                {
                    sb.Append(", and has got no nearer than ");
                    sb.Append(_closest);
                    sb.Append(" tiles for ");
                    sb.Append(Minutes(NoCloserMs));
                }

                sb.Append(")");
            }

            sb.Append(". It has kept to a patch ");
            sb.Append(Patch);
            sb.Append(" tiles across for ");
            sb.Append(Minutes(PacingMs > 0 ? PacingMs : 0));
            sb.Append(", changing tile ");
            sb.Append(Stirs);
            sb.Append(" times inside it. Given up ");
            sb.Append(Abandoned);
            sb.Append(" errands while still short of them. Still for ");
            sb.Append(Minutes(StillMs));
            sb.Append(". ");
            sb.Append(Deeds);
            sb.Append(" undertakings seen, ");
            sb.Append(Quick);
            sb.Append(" of them over inside ");
            sb.Append(QuickMs / 1000);
            sb.Append("s. Worth ");
            sb.Append(Worth);
            sb.Append("gp now against ");
            sb.Append(FirstWorth);
            sb.Append("gp when first seen ");
            sb.Append(Minutes(WatchedMs(now)));
            sb.Append(" ago. Trade at ");
            sb.Append(Progress * 100.0, "F0");
            sb.Append("% of what its class is aiming for, from ");
            sb.Append(FirstProgress * 100.0, "F0");
            sb.Append("%. Contentment ");
            sb.Append(Mood, "F2");
            sb.Append(".");

            if (_kind == "-" && !string.IsNullOrWhiteSpace(Empty))
            {
                sb.Append(" It holds nothing because: ");
                sb.Append(Empty);
                sb.Append(".");
            }

            if (!string.IsNullOrWhiteSpace(Why))
            {
                // 02.09.2026: Because is written once, at the moment work is taken, and never cleared when
                // that work ends — so a bot holding nothing at all was printed as "doing this because:
                // 142/min", which reads as an auction that offered good work to a bot who sat anyway. It is
                // the finished work's reason, not the present one. Orin sat Free for 2s under a 142/min line
                // this way. Say which of the two it is.
                sb.Append(
                    _kind == "-"
                        ? " The last work it took, now over, was chosen because: "
                        : " It says it is doing this because: "
                );
                sb.Append(Why);
                sb.Append(".");
            }

            if (!string.IsNullOrWhiteSpace(Symptoms))
            {
                sb.Append("\n  SYMPTOMS: ");
                sb.Append(Symptoms);
                sb.Append(".");
            }

            return sb.ToString();
        }
        finally
        {
            sb.Dispose();
        }
    }
}
