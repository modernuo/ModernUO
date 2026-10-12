using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot is: its build, what it is born owning, what it may carry, and the one thing it can do
/// that nobody else can.
///
/// <para>
/// <b>Data and limits, never behaviour.</b> A class does not decide anything. Deciding lives in
/// <c>BotWill/</c>, and it reads classes the way it reads the map — as facts about the world. This is
/// the boundary the first version did not have: talents there were wired straight into whichever
/// subsystem noticed them first, so the mage's staff grew its own timer inside the magic code, and
/// there was no place to look up what a mage <em>was</em>. Nine files and one contract replace that.
/// </para>
///
/// <para>
/// <b>Two kinds of member, and the split is deliberate.</b> Identity — name, role, main skill — is
/// abstract and unchangeable: it is what the class <em>is</em>, and a configuration file that could
/// rename a healer into a smith would be a configuration file that can break every muster on the
/// shard. Everything else is a settable property carrying the class's own default, so
/// <c>bots.json</c> can move any number without a rebuild. That matters more here than it looks:
/// this shard is built on a different machine from the one it is designed on, so a number that needs
/// a compiler to change is a number that needs a person to change.
/// </para>
/// </summary>
public abstract class BotClass
{
    /// <summary>How often a mana trickle pays out, for every carrier of one.</summary>
    public const int ManaTrickleIntervalMs = 4000;

    /// <summary>
    /// How often any bot may brew, unless its class says otherwise.
    ///
    /// Brewing is open to everybody who has the skill and the reagents — alchemy is a disposition of
    /// the two casters, not a licence they hold — so the throttle is a property of the world rather
    /// than of a class, and only the healer improves on it.
    /// </summary>
    public const int DefaultBrewIntervalMs = 600000;

    // ---- Defaults, and putting them back. ------------------------------------------------------

    /// <summary>
    /// The class's own numbers. Called once when the class is built, and again before configuration is
    /// applied on top.
    ///
    /// Split out from the constructor for one reason that turns out to matter: applying overrides has to
    /// be repeatable. Written the obvious way — constructor sets defaults, configuration mutates them —
    /// a second pass would land on top of the first, and the merged fields would accumulate rather than
    /// replace. Potion limits are the sharpest case: a limit lifted by a config that is later corrected
    /// would stay lifted, because nothing removes a key. So the numbers can always be put back.
    /// </summary>
    protected abstract void Defaults();

    /// <summary>
    /// Everything back to nothing, then this class's own defaults on top.
    ///
    /// Neutral first rather than only re-running <see cref="Defaults"/>: a class does not state the
    /// talents it lacks, so a value that configuration set and the class never mentions would otherwise
    /// survive a reset. Nothing is a talent until a class claims it.
    /// </summary>
    public void Reset()
    {
        Str = 0;
        Dex = 0;
        Int = 0;
        Skills = [];
        Kit = new BotKit();
        Leanings = [];
        NeedsMeditation = false;
        PotionLimits.Clear();
        BrewIntervalMs = DefaultBrewIntervalMs;
        IntrinsicManaTrickle = 0;
        StaffManaTrickle = 0;
        StaffHue = 0;
        CritChancePerSkill = 0.0;
        CritMultiplier = 3;
        HandsAlwaysFree = false;
        FreeCraftIntervalMs = 0;
        ForageIntervalMs = 0;
        ForageYieldMin = 0;
        ForageYieldMax = 0;
        Stipend = 0;

        Defaults();
    }

    // ---- Identity. Fixed in code, because these are what the class is. -------------------------

    /// <summary>Stable key. Used in configuration, in the summary and in every log line.</summary>
    public abstract string Name { get; }

    /// <summary>What this class fills in a group. See <see cref="BotRole"/>.</summary>
    public abstract BotRole Role { get; }

    /// <summary>
    /// Whether this class casts at all, asked separately from its role.
    ///
    /// A warrior-mage holds the melee line and also throws spells; a muster that counted casters by
    /// role would miss it, and one that filed it under <see cref="BotRole.Caster"/> would put it in
    /// the back rank where its plate and its sword are wasted.
    /// </summary>
    public virtual bool Casts => false;

    /// <summary>
    /// The skill this class is judged by — its trade, whatever the numbers say. Null when the birth
    /// roll settles it, in which case the chosen weapon's skill is the main skill.
    ///
    /// Stated outright rather than derived from the highest target, and that is the whole point. A
    /// smith must want Mining above Blacksmithing, because smelting is a Mining check and a smith who
    /// cannot smelt has nothing to forge; the first version inferred the main skill from the largest
    /// target and so produced champion smiths who were grandmaster <em>miners</em> with a
    /// journeyman's hammer. The title, the rank and every report read this instead of guessing.
    ///
    /// Null is not a gap. A plain warrior has no trade beyond fighting, and which blade it fights with
    /// is genuinely decided by the roll — pinning a main skill here would make two thirds of them
    /// swordsmen on paper who are maces in the hand.
    /// </summary>
    public abstract SkillName? MainSkill { get; }

    /// <summary>
    /// Whether this class is born already holding its trade, at the exact skills it declares.
    ///
    /// <para>
    /// <b>False for everybody but the captain, and the default is the interesting half.</b> An ordinary bot
    /// is given fifty, thirty and twenty points across its top three skills and has to earn the rest — that
    /// is what makes the shard's progress visible, and it is why a warrior born on Monday is worth watching
    /// on Friday. A class that sets this asks for the opposite and had better have a reason that is not
    /// "it would be stronger": the captain's is that both of its offices are impossible without it. It
    /// cannot lead a company into ground that has killed somebody while still learning, and it cannot teach
    /// what it does not know.
    /// </para>
    /// </summary>
    public virtual bool Seasoned => false;

    /// <summary>
    /// What share of its targets a seasoned class is actually born with. One means "born finished".
    ///
    /// <para>
    /// <b>It exists because raising the targets without it would have changed nothing at all.</b> A seasoned
    /// bot is given its declared target outright — see <c>BotMobile.Learn</c> — so on 02.09.2026, when every
    /// class's targets went to 100 to give the population somewhere to grow, the three seasoned classes
    /// would have been born at 100 and stood idle from their first second, which is the exact problem the
    /// change was making. Born strong and still with somewhere to go is the thing that was wanted, and it
    /// takes two numbers to say: what it aims at, and how much of that it starts with.
    /// </para>
    /// </summary>
    public virtual double Seasoning => 1.0;

    /// <summary>
    /// Whether this class may call a company together for a place rather than for a quarry, and keep it out.
    ///
    /// <para>
    /// <b>The whole of what authority is on this shard.</b> Nobody is obliged to follow anybody: a captain
    /// gets an offer no other bot is ever made — see <c>BotPatrol</c> — and the bots it calls on come because
    /// they were free and go back to their own business when it ends. Modelling rank any other way would
    /// replace a bot's motivation with somebody else's, which this project has refused to do since its first
    /// week.
    /// </para>
    /// </summary>
    public virtual bool Leads => false;

    /// <summary>
    /// Whether this class answers something closing on it by drawing a second weapon rather than by
    /// retreating.
    ///
    /// <para>
    /// Read by <c>BotSlay</c> at exactly one moment: when a quarry comes inside the standoff of a bot built
    /// to fight at range. Everything else about the fight is the shard's ordinary code, and that is the
    /// point — a class's fighting identity should be one fact the combat asks about, not a second combat.
    /// </para>
    /// </summary>
    public virtual bool Closes => false;

    /// <summary>
    /// Whether this class may open a class for casters, as <see cref="Leads"/> does for fighters.
    ///
    /// <para>
    /// <b>A separate flag rather than a second meaning for <see cref="Leads"/>, because they are separate
    /// offices.</b> Leads is the whole of authority on this shard — it is what makes a captain the only bot
    /// ever offered a patrol — and a teacher of magic has no business calling companies together. Folding
    /// the two into one flag would have handed the sage a company and the captain a lectern, and neither is
    /// what either is for.
    /// </para>
    /// </summary>
    public virtual bool Tutors => false;

    /// <summary>
    /// The only trades this class may be offered, by proposer name. Empty — for everybody but one — means
    /// "whatever the shard has".
    ///
    /// <para>
    /// <b>A gate rather than a preference, and the difference is the whole reason it is a list and not a
    /// number.</b> Everything else about what a bot does is settled by the auction weighing offers in gold a
    /// minute, and that arithmetic is the one part of this shard that has never needed a thumb on it. A class
    /// whose work is meant to be chosen for reasons the arithmetic cannot see therefore cannot be expressed
    /// by scoring: price its errand high and the auction has been rigged, price it honestly and the bot goes
    /// mining. So the offers it is not meant to take are never made — see <c>BotWill</c>, where this is read
    /// once per proposer per review and counted when it refuses.
    /// </para>
    ///
    /// <para>
    /// <b>It binds the free rung only.</b> Mending, flight and the reflex that answers a blow live below the
    /// auction and fire whether or not anything is choosing; a list that could switch those off would be a
    /// class that can decide to bleed. See <see cref="BotStanding"/>.
    /// </para>
    /// </summary>
    public virtual string[] Sworn => [];

    /// <summary>
    /// The spells this class fills its book with first, in order, before the cheapest gap. Empty — for most — means
    /// cheapest first, which is how books have always been filled here.
    ///
    /// <para>
    /// <b>A book filled cheapest first is a book of curses.</b> Spell ids run by circle, so the first gaps in a young
    /// caster's book are Clumsy, Create Food and Feeblemind. On the morning of 14.09.2026 a healer with fifteen gold to
    /// its name spent it on Weaken, Reactive Armour, Night Sight, Feeblemind, Create Food and Clumsy while its book
    /// was still short of Greater Heal and Protection. What a class learns first is part of what it is — RimWorld's
    /// passions and The Sims' trait-filtered advertisements both say so — and nobody watching would read that
    /// shopping list as a healer's.
    /// </para>
    /// </summary>
    public virtual int[] BookFirst => [];

    /// <summary>
    /// Whether this class's contentment is about the state of the island rather than about its own purse and
    /// its own idleness.
    ///
    /// <para>
    /// <b>The ordinary two halves are boredom and need — see <c>BotMobile.Mood</c> — and both of them read
    /// backwards on a bot that is paid nothing on purpose.</b> Need is measured against what the work on
    /// offer costs, so a bot that buys almost nothing reads as comfortable however the island is doing.
    /// Boredom is relieved by being paid, so a bot that gives its takings away is never relieved at all: it
    /// would climb to its ceiling on the first evening and stay there, reporting a bot in despair while it
    /// worked and a bot in despair while it stood still — one number, no information.
    /// </para>
    ///
    /// <para>
    /// A class that says this is asked a different question instead, and the class is the only thing that
    /// can say which: <c>BotMobile.Mood</c> hands the answer to whatever the class actually grieves over.
    /// </para>
    /// </summary>
    public virtual bool Grieves => false;

    /// <summary>
    /// Whether this class stands out of the share-out when a company divides what it killed.
    ///
    /// <para>
    /// <b>Its own flag rather than a second meaning for <see cref="Grieves"/>, and the two really are
    /// separate facts.</b> Grieving is about what makes a bot content; abstaining is about who gets the
    /// gold off a corpse. They happen to be true of the same class today, which is exactly the situation in
    /// which folding them into one is tempting and wrong — the next class that is paid strangely would
    /// arrive with a mood it never asked for, and this project has already paid several times for a number
    /// that was doing two jobs.
    /// </para>
    ///
    /// <para>
    /// Read by <c>BotSpoils</c> and nowhere else, and it never empties a share-out: see the note there.
    /// </para>
    /// </summary>
    public virtual bool Unpaid => false;

    /// <summary>
    /// Whether this class buys itself a horse when it can afford one.
    ///
    /// <para>
    /// <b>A flag rather than a list of class names, so that the next one costs nothing.</b> The gatherer is
    /// the first because its day is the walk — out to the rock, back to the forge, out again, under a pack
    /// heavy enough that stamina is what it actually runs out of. It is the one trade whose takings are
    /// bounded by distance rather than by skill or by what it meets. Nothing else about the arrangement is
    /// about mining, and nothing in <c>BotStable</c> asks what class this is.
    /// </para>
    /// </summary>
    /// <summary>
    /// <b>Every class, by Patrick's order of 05.09.2026.</b> It was the gatherer and the architect, on the
    /// reasoning that their day is the walk. That reasoning was never wrong; it was too narrow. Every trade
    /// on this island walks — to a wood, to a counter, to a forge, to a square that has killed people — and
    /// the flag is kept rather than deleted because a class that should not ride can still say so.
    /// </summary>
    public virtual bool Rides => true;

    /// <summary>
    /// Whether this class will stop to go through a corpse at all.
    ///
    /// <para>
    /// <b>True for everybody who lives by earning, and false is a statement about formation rather than about
    /// greed.</b> A company that stops to rob what it killed is a company strung out across a quadrant with
    /// its healer forty tiles from its warriors — which is precisely the arrangement that gets all of it
    /// killed. For a class whose whole duty is to walk somewhere dangerous together and come back, refusing
    /// the corpse is worth more than what is in it.
    /// </para>
    ///
    /// <para>
    /// Distinct from <see cref="Unpaid"/>, which is about the division of what a company took: a bot can
    /// gather spoils it takes no share of, and one can take a share of spoils it did not gather. Two facts,
    /// two flags — this project has paid several times over for one number doing two jobs.
    /// </para>
    /// </summary>
    public virtual bool Scavenges => true;

    /// <summary>
    /// Whether the crown replaces what this class spends: bandages, reagents and ammunition.
    ///
    /// <para>
    /// <b>Provisioning is not payment, and the difference is the whole reason this is a flag.</b> Coin handed
    /// to a bot enters the population and competes with every price on the shard — the Baron's stipend is one
    /// such tap and was allowed once, deliberately, with a note. Supplies replaced in the pack never become
    /// money: nothing can be sold on this shard from a bound stack, so a provisioned class costs the economy
    /// nothing at all while never standing in a shop when it should be walking.
    /// </para>
    /// </summary>
    public virtual bool Provisioned => false;

    /// <summary>
    /// Whether this class fights only what has already laid hands on it.
    ///
    /// <para>
    /// For a medic, and it is the difference between a healer and a fifth fighter. Its station is already at
    /// the back of the formation; this makes the same thing true of its instincts, so it does not walk
    /// forward to join a fight its company is winning and is then not there for the one it is losing.
    /// Hitting back when something reaches it is a reflex and stays one — see <c>BotMobile.OnDamage</c>,
    /// which is not a decision and cannot be talked out of.
    /// </para>
    /// </summary>
    public virtual bool DefendsOnly => false;

    /// <summary>
    /// Whether this class is run by the decision layer at all.
    ///
    /// <para>
    /// True for everybody who earns a living: the auction is how a bot with a trade decides what is worth
    /// doing next, and it is the whole of this project's answer to that question. False for a class under
    /// standing orders, whose work is not one option among many and must not be displaced by one — see
    /// <c>BotRangers</c>, which keeps its own company, picks its own ground and walks it on its own clock.
    /// </para>
    ///
    /// <para>
    /// A flag rather than a price, and that distinction was paid for over an evening. Every attempt to keep
    /// such a class on task by pricing its work higher, or by narrowing what it may be offered, failed the
    /// same way: a fight is worth more per minute than a patrol by any honest reckoning, so the patrol was
    /// dropped, and what came back afterwards was nothing at all.
    /// </para>
    /// </summary>
    public virtual bool Bidding => true;

    /// <summary>
    /// Whether this class stands outside every war on the island: in no guild, in no company, tolled by nobody, and nobody's
    /// target in a guild's quarrel.
    ///
    /// <para>
    /// <b>Patrick's answer of 30.09.2026 on the traders: neutral in wars — not attacked in guild wars and not taking part in
    /// them.</b> A flag rather than a class name, so the next neutral class costs nothing; what it closes is said once, in
    /// <c>BotTrading.Neutral</c> and <c>BotTraders/README.md</c> (01.10.2026, build 385).
    /// </para>
    /// </summary>
    public virtual bool Neutral => false;

    /// <summary>
    /// Whether this class is an office: a post the island holds exactly once, at any population.
    ///
    /// <para>
    /// <b>Patrick's rule of 07.10.2026, said in anger after it cost a wipe: the Captain, the Baron, the Architect and the Sage
    /// exist once each, however many bots the island has.</b> Build 425 wrote it into <c>bot-population.json</c> (a count of 1)
    /// and nowhere else, so a stale file, a newcomer laid over the mix or a critical floor could still make a second. It is a
    /// fact of the class now, read by every path that creates a bot: <see cref="BotPopulationConfig"/> treats a configured
    /// count above 1 as 1 and says so, <c>BotGrowth</c> never draws, lays over or answers a want with an office that is held,
    /// <c>BotPopulation.Reclaim</c> keeps one holder from the save, and <c>BotPopulation.Raise</c> refuses a second at the
    /// door, whoever asked. A count of 0 still means none.
    /// </para>
    /// </summary>
    public virtual bool Office => false;

    // ---- Build. Defaults in code, overridable from configuration. ------------------------------

    /// <summary>
    /// Gold this class is kept at in the bank by something other than its own work, or nought for everybody
    /// who lives on what it earns.
    ///
    /// <para>
    /// Settable, because it is a number and every number on a class is settable; and read in exactly two
    /// places — <c>BotStipend</c>, which pays it, and <c>BotPurse</c>, which stops banking the pocket of
    /// anybody who has one. See the first of those for why a second faucet is allowed once.
    /// </para>
    /// </summary>
    public int Stipend { get; set; }

    /// <summary>Starting Strength. The three stats total 100 across every class, as in the first version.</summary>
    public int Str { get; set; }

    /// <summary>Starting Dexterity.</summary>
    public int Dex { get; set; }

    /// <summary>Starting Intelligence.</summary>
    public int Int { get; set; }

    /// <summary>
    /// Skills this class works towards, excluding whichever weapon skill the birth roll settles.
    ///
    /// A target is a want, not an achievement: a bot with everything it wanted stops going anywhere,
    /// so rank raises these rather than filling them in.
    /// </summary>
    public IReadOnlyList<(SkillName Skill, double Target)> Skills { get; set; } = [];

    /// <summary>What the class is born owning. See <see cref="BotKit"/>.</summary>
    public BotKit Kit { get; set; } = new();

    /// <summary>
    /// The ways a newborn of this class may lean: towards which of its skills, how often, how far. Empty is the class's own
    /// deal for every bot, which is what a seasoned class keeps. See <see cref="BotLeaning"/> and <see cref="BotBirth"/>
    /// (01.10.2026, Patrick's point 7).
    /// </summary>
    public IReadOnlyList<BotLeaning> Leanings { get; set; } = [];

    /// <summary>
    /// Whether this skill is one the class is actually for.
    ///
    /// <para>
    /// <b>What makes "getting better" mean getting better at your own trade.</b> The decision layer values
    /// work by the skill it produces, and without this a warrior mining all night scores exactly like a miner
    /// mining all night — the arithmetic would say it was thriving while its own trade stood still. Off-trade
    /// gain still counts, because a warrior who learns to mine has learned something; it counts for less.
    /// </para>
    ///
    /// <para>
    /// The weapon options are included, because which blade a bot ends up with is settled by a roll at birth
    /// and the skill that swings it is as much this class's trade as anything written in
    /// <see cref="Skills"/> — which deliberately leaves it out for exactly that reason.
    /// </para>
    /// </summary>
    public bool Wants(SkillName skill)
    {
        if (MainSkill == skill)
        {
            return true;
        }

        var wanted = Skills;

        for (var i = 0; i < wanted.Count; i++)
        {
            if (wanted[i].Skill == skill)
            {
                return true;
            }
        }

        return Offered(Kit.Melee, skill)
            || Offered(Kit.Ranged, skill)
            || Kit.Sidearm.HasValue && Kit.Sidearm.Value.Skill == skill;
    }

    private static bool Offered(IReadOnlyList<BotWeaponOption> options, SkillName skill)
    {
        for (var i = 0; i < options.Count; i++)
        {
            if (options[i].Skill == skill)
            {
                return true;
            }
        }

        return false;
    }

    // ---- Limits. ------------------------------------------------------------------------------

    /// <summary>
    /// Whether this class refuses armour that would stop it meditating.
    ///
    /// Answered by asking the engine — <c>BaseArmor.MeditationAllowance</c> — rather than by listing
    /// plate, chain and ring here. A list is a second source of truth about the contents of the world
    /// and would be wrong the first time somebody edits an armour definition; the engine already
    /// knows the answer for every piece that exists.
    /// </summary>
    public bool NeedsMeditation { get; set; }

    /// <summary>
    /// How many potions of each family the bot will carry. Anything absent is capped at one.
    ///
    /// One of each is tight on purpose, and the consequence is the interesting part: a caster brewing
    /// faster than it drinks cannot hoard, so the second bottle goes to the auction. A potion stops
    /// being a private supply and becomes goods — which is the thing the first version's economy was
    /// missing, since the auction could only ever redistribute what had been looted off corpses.
    /// </summary>
    /// Read-only as a reference so that <see cref="Reset"/> can always clear it and nothing can leave it
    /// null; the contents are what varies.
    public Dictionary<BotPotionKind, int> PotionLimits { get; } = [];

    /// <summary>
    /// How many of this family the bot may hold. Two unless the class says otherwise.
    ///
    /// <para>
    /// <b>One was deliberate and it was killing bots, which the counter said in as many words.</b> The
    /// reasoning above is sound — a tight limit turns a surplus bottle into goods rather than a hoard — but
    /// one bottle is one swallow, and a bot that has taken a bad thirty seconds is empty for as long as it
    /// takes to walk to a shop. In one window: seventeen beats spent under fifteen per cent health, one
    /// swallow, and sixteen refusals for an empty pack. A potion is the only mending in this game that works
    /// while something is hitting you, so an empty pack at that moment is simply a death.
    /// </para>
    ///
    /// <para>
    /// Two rather than more, so the argument above keeps its force: a brewer still cannot hoard, the third
    /// bottle still goes to the auction, and what changes is only that surviving one bad moment no longer
    /// costs a bot the ability to survive the next one.
    /// </para>
    /// </summary>
    public int PotionLimit(BotPotionKind kind) =>
        PotionLimits.TryGetValue(kind, out var limit) ? limit : 2;

    /// <summary>
    /// Whether this class may brew mana potions.
    ///
    /// Restricted to the two casting trades while every other potion is open to anybody with the
    /// skill. Mana potions are outside the era and exist only because this shard needs an answer to
    /// "how does a caster recover while being hit"; letting a smith produce them would make the one
    /// item that cannot be replaced by anything else into a commodity.
    /// </summary>
    public virtual bool CanBrewManaPotion => Role is BotRole.Caster or BotRole.Medic;

    /// <summary>How long between brews. The healer is the only class that improves on the default.</summary>
    public int BrewIntervalMs { get; set; } = DefaultBrewIntervalMs;

    // ---- Talents. A default of nothing means "this class has no such talent". -------------------

    /// <summary>
    /// Mana returned every <see cref="ManaTrickleIntervalMs"/> with no item involved. The
    /// warrior-mage's, and nobody else's.
    /// </summary>
    public int IntrinsicManaTrickle { get; set; }

    /// <summary>Mana returned by this class's staff, when the staff is actually in its hands.</summary>
    public int StaffManaTrickle { get; set; }

    /// <summary>
    /// The colour of this class's staff. Zero for anybody who is not issued one.
    ///
    /// A property of the class rather than of the item because it is the only thing about a caster that
    /// is readable across a courtyard: blue is the mage and green is the healer. One item type, two
    /// hues — the difference between them is the trickle, and that is a number on the class.
    /// </summary>
    public int StaffHue { get; set; }

    /// <summary>
    /// What the bot actually gets back, given whether it is holding its staff.
    ///
    /// The larger of the two rather than their sum, and this single line is the whole ruling: a
    /// warrior-mage who picks up a staff gets a staff's worth of mana, not a staff's worth on top of
    /// its own. Its talent buys it the right to wear plate and hold a sword while still recovering —
    /// that is the advantage, and stacking would turn it into a better mage than the mage.
    /// </summary>
    public int ManaTrickle(bool staffInHand) =>
        Math.Max(IntrinsicManaTrickle, staffInHand ? StaffManaTrickle : 0);

    /// <summary>
    /// Chance of a critical shot per point of the governing skill. Zero for every class but the archer.
    ///
    /// Scaled by skill rather than flat because a talent handed out whole at birth is not something
    /// the bot can work towards, and wanting to get better is the only motive this project has. At the
    /// archer's rate that is roughly one shot in thirty for a novice and one in ten for a grandmaster.
    /// </summary>
    public double CritChancePerSkill { get; set; }

    /// <summary>What a critical shot multiplies damage by.</summary>
    public int CritMultiplier { get; set; } = 3;

    /// <summary>Chance of a critical shot at the given skill value, from 0 to 1.</summary>
    public double CritChance(double skill) => Math.Clamp(skill * CritChancePerSkill, 0.0, 1.0);

    /// <summary>
    /// Whether this class never has to put its weapon away to bandage, drink or cast.
    ///
    /// The brawler's, and it is the least showy talent here and probably the strongest. Bandaging
    /// needs both hands, so every armed bot in the first version was forbidden to bandage in contact
    /// — which left a wounded bot two choices, both bad: stand still and die, or run and lose the
    /// fight. A bot that fights with its hands has never had that problem and does not need a new
    /// mechanic to be given the advantage; it needs the existing restriction not to apply to it.
    /// </summary>
    public bool HandsAlwaysFree { get; set; }

    /// <summary>
    /// How long between free crafts, or zero for a class that gets none.
    ///
    /// The crafter's: one attempt yields two items and charges materials for one. Aimed squarely at
    /// the measured bottleneck rather than at the craft skill — a pack holds about twelve ore, which
    /// is twenty ingots, which is one or two helmets, and then back to the mine. Doubling the output
    /// of one swing an hour is worth roughly an extra trip underground.
    /// </summary>
    public int FreeCraftIntervalMs { get; set; }

    /// <summary>
    /// How often this class may go and look for herbs in the woods, or nought for a class that may not.
    ///
    /// <para>
    /// <b>The one thing a caster runs out of that no skill in this era can make.</b> Reagents are shop goods:
    /// nothing picks them, nothing grows them, and a shard whose shopkeepers happen not to stock sulphurous
    /// ash is a shard where a whole trade quietly ends — which has already happened here and left one line at
    /// boot behind it. A sage who can walk out and come back with a bag of herbs is the population's answer
    /// to that, and it is rationed by this so that it stays an answer rather than a tap.
    /// </para>
    /// </summary>
    public int HerbIntervalMs { get; set; }

    /// <summary>How long between reagent searches, or zero for a class that cannot forage.</summary>
    public int ForageIntervalMs { get; set; }

    /// <summary>Fewest of one reagent a search turns up.</summary>
    public int ForageYieldMin { get; set; }

    /// <summary>
    /// Most of one reagent a search turns up.
    ///
    /// A handful rather than a full order on purpose. A caster orders fifteen at a time, so no single
    /// search settles one — which keeps the gatherer a supplier of casters rather than a one-off
    /// answer to their shortage, and keeps the shortage itself worth paying to fix.
    /// </summary>
    public int ForageYieldMax { get; set; }
}
