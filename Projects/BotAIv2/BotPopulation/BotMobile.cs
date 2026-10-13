using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Guilds;
using Server.Items;
using Server.Logging;
using Server.Misc;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// An autonomous inhabitant of the shard. <b>The object all seven other subsystems were written against
/// and waiting for.</b>
///
/// <para>
/// <b>It derives from <see cref="PlayerMobile"/> rather than <c>BaseCreature</c>, and that single choice is
/// what makes this project's measure of work possible.</b> Every player system applies without being
/// reimplemented: use-based skill gain, fame and karma, banking, criminal flags, a lootable corpse. Skill
/// gain is the point — the whole decision layer values work by the skill it produces, and skill here is the
/// engine raising a number after its own check, not us deciding a number should go up. On a creature there
/// would be nothing to measure and the metric would have to be invented.
/// </para>
///
/// <para>
/// The cost is that nothing drives it: <see cref="PlayerMobile"/> has no think loop. <see cref="BotBeat"/>
/// supplies one, and <see cref="Beat"/> below is the whole of what a bot does per turn — three calls, in an
/// order that matters.
/// </para>
///
/// <para>
/// <b>Nothing here holds a <c>NetState</c>.</b> The engine tolerates that, and one consequence is load
/// bearing: <see cref="Mobile.Move"/> skips its movement throttle when the net state is null, so a bot's
/// pace is set purely by how often it is beaten. Content that assumes a connected client is the standing
/// hazard of this approach, and the reason the population starts small.
/// </para>
///
/// <para>
/// <b>Saved bots are not reused.</b> The world save will contain these — they are Mobiles like any other —
/// so the population is purged and rebuilt on every world load. See
/// <see cref="BotPopulation.PurgeSaved"/> for why that is the honest choice rather than laziness: half the
/// state a bot needs lives in objects that would have to be rebuilt anyway, and a kit handed out twice is a
/// bot with two of everything.
/// </para>
/// </summary>
public class BotMobile : PlayerMobile, IBotWilful, IBotAside
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMobile));

    /// <summary>
    /// How much of its health a bot needs to count as help in somebody else's fight.
    ///
    /// A quarter. The first version formed companies whose founder could not fight, found nobody able, and
    /// disbanded in the same tick — over and over, because "is it alive" was the only test.
    /// </summary>
    public static double FitFraction { get; set; } = 0.25;

    /// <summary>
    /// How far a bot reckons the fight it is in when something hits it.
    ///
    /// Ten tiles, which is more than a sword's reach on purpose: a caster strikes from eight and never
    /// closes, so a bot that only counted what was next to it would be assessing the wrong fight.
    /// </summary>
    public static int NoticeRange { get; set; } = 10;

    /// <summary>
    /// The name hue of the shard's champion — the healers' yellow. One number, read by the championship that sets it
    /// and by the dashboard that shows it, so the two cannot disagree about who is champion.
    /// </summary>
    public static int ChampionHue { get; set; } = 0x35;

    /// <summary>Whether the population runs rather than walks. Sets the beat as well as the pace.</summary>
    public static bool Runs { get; set; }

    /// <summary>Only so that a bot which came back from a save has something to delete. See the class note.</summary>
    public BotMobile(Serial serial) : base(serial)
    {
    }

    public BotMobile()
    {
    }

    /// <summary>The mobile this bot is — itself. See <see cref="IBotSquadMember.Self"/> for why not "Body".</summary>
    public Mobile Self => this;

    /// <summary>What this bot is. Data and limits; it decides nothing. See <see cref="BotClass"/>.</summary>
    public BotClass Class { get; private set; }

    /// <summary>Where it is going, and what it has put aside to do first.</summary>
    public BotJourney Journey { get; } = new();

    /// <summary>What the walker said on this bot's last beat. For the lines that ask why a bot is not moving.</summary>
    public BotWalkResult LastWalk { get; private set; }

    /// <summary>Which quadrant this bot was in when it was last looked at, and whereabouts in it.</summary>
    private (int Map, int X, int Y) _quadWas;

    private Point3D _quadAt;

    private bool _quadSeen;

    /// <summary>
    /// Whether anything has laid a finger on this bot since it entered the quadrant it is standing in.
    ///
    /// <para>
    /// This is what makes a crossing count as evidence about the ground rather than merely as traffic over
    /// it. The order was "three <em>free</em> crossings", and free is the operative word: a bot that walked
    /// across a square while something was chewing on it has learned that the square is dangerous, not that
    /// it is fine. Without this the very ground where the fighting happens would be credited fastest, since
    /// it is also the ground bots walk over most.
    /// </para>
    /// </summary>
    private bool _quadClean;

    /// <summary>What it has resolved, feels and has learned.</summary>
    public BotResolve Resolve { get; } = new();

    /// <summary>
    /// Which squad it belongs to, or null. Held here, set by the registry — never looked up in a table
    /// keyed by serial.
    /// </summary>
    public BotSquad Squad { get; set; }

    /// <summary>What it was given at birth and cannot lose. See <see cref="BotBond"/>.</summary>
    public BotBond Bond { get; private set; }

    /// <summary>The class this bot was when it was last saved, by name. Null on a bot that was never saved.</summary>
    public string Was { get; private set; }

    /// <summary>The kind of work in hand when it was last saved, and where that work was. See <c>BotPopulation.Reclaim</c>.</summary>
    public string WasDoing { get; private set; }

    public Map WasDoingOn { get; private set; }

    public Point3D WasDoingAt { get; private set; }

    /// <summary>Whether it could actually take part in a fight.</summary>
    public bool AbleToFight =>
        !Deleted && Alive && Map != null && Map != Map.Internal && Hits >= HitsMax * FitFraction;

    /// <summary>
    /// When this bot is next due a turn, and whether it has ever had one. Owned by <see cref="BotBeat"/>.
    ///
    /// The flag is not decoration: a tick count of zero is a legitimate reading on hosts that pass the
    /// machine's uptime counter through, so "never beaten" cannot be spelled as "stamp is zero".
    /// </summary>
    public long DueTick { get; internal set; }

    public bool Scheduled { get; internal set; }

    /// <summary>
    /// Whether something has happened to this bot that will not wait for its next turn. Set by <see cref="Hurry"/>, read and
    /// cleared by <see cref="BotBeat"/>, which has it decide — and only decide — at its next look, a tenth of a second off.
    /// </summary>
    public bool Urgent { get; internal set; }

    /// <summary>
    /// <b>Decide now, step when due (06.10.2026, react14).</b> A bot takes a turn every step — two hundred milliseconds at a run,
    /// four hundred at a walk, a multiple of that for a leader pacing its column — and a turn was the only moment it decided
    /// anything. The first blow of a bout, health crossing the failing line, a creature seen coming, a fight just ended and a
    /// friend calling are events, and an event is answered in the beat it arrives: the clock looks every hundred milliseconds
    /// and gives a bot so marked its decision without its step. Free to call twice; one decision comes of it.
    /// </summary>
    public void Hurry() => Urgent = true;

    /// <summary>The decision <see cref="Hurry"/> asked for: deciding and nothing else. The step waits for the turn. See BotBeat.</summary>
    public void React()
    {
        if (Deleted || !Alive || Fallen || Map == null || Map == Map.Internal)
        {
            return;
        }

        var began = System.Diagnostics.Stopwatch.GetTimestamp();

        BotWill.Decide(this);

        BotWill.Spent(System.Diagnostics.Stopwatch.GetTimestamp() - began);
    }

    /// <summary>Whether this bot is lying dead, and since when. Read by the population's reviver.</summary>
    public bool Fallen { get; private set; }

    /// <summary>Bots the world save returned dead and handed to the reviver rather than deleted. See <see cref="Revive"/>.</summary>
    public static long BackAsGhost { get; private set; }

    /// <summary>Whether a refused resurrection has already been reported for this death. One line, not one a beat.</summary>
    public bool ReviveComplained { get; set; }

    /// <summary>
    /// The corpse this bot left behind, or null.
    ///
    /// Remembered at the moment of death rather than looked for afterwards: the engine hands the corpse
    /// straight to the death hook, and sweeping the world for one later would be both expensive and unable to
    /// say whose it was.
    /// </summary>
    public Corpse Remains { get; set; }

    /// <summary>
    /// Roads proved impossible since this bot last actually moved. See <see cref="BotPopulation.Rescue"/>.
    ///
    /// Reset by a single step, which is what makes it mean "cannot get anywhere" rather than "picked badly":
    /// a bot that is walking is a bot that is not stranded, whatever it has been refused.
    /// </summary>
    public int Refusals { get; set; }

    /// <summary>
    /// Played out for this session: offered nothing new, and leaving the world the first time it is free. Set by
    /// <see cref="BotRest"/>; not saved, because the play it is read from is.
    /// </summary>
    public bool Tired { get; set; }

    /// <summary>Until when a paid night at an inn keeps regenerating this bot. See BotInns (29.09.2026).</summary>
    public DateTime WellRestedUntil { get; set; }

    /// <summary>The town this bot lives in, held on the body so asking costs a field read. Kept by BotResidenceStore, not here (29.09.2026).</summary>
    public BotResidence.Record Residence { get; set; }

    public long FellTick { get; private set; }

    /// <summary>
    /// How far along its own trade this bot is, from nought to one.
    ///
    /// <para>
    /// <b>The one number that says whether this population is going anywhere.</b> Money says what a bot has;
    /// this says whether it is becoming something. It is the share of what its class declared it was working
    /// towards that it has actually reached — the weapon it was handed included, because that skill is chosen
    /// by the roll and is as much part of the trade as the ones written down.
    /// </para>
    ///
    /// <para>
    /// Capped per skill at that skill's own target, so a bot cannot make its vector look good by being
    /// enormously better than it needed to be at one thing. A target of forty does not want fifty.
    /// </para>
    /// </summary>
    public double Progress
    {
        get
        {
            var klass = Class;

            if (klass == null)
            {
                return 0.0;
            }

            var wanted = 0.0;
            var reached = 0.0;
            var wants = klass.Skills;

            for (var i = 0; i < wants.Count; i++)
            {
                var (skill, target) = wants[i];

                if (target <= 0.0)
                {
                    continue;
                }

                wanted += target;
                reached += Math.Min(target, Skills[skill].Base);
            }

            var weapon = Bond?.Weapon;

            if (weapon != null && weapon.Value.Target > 0.0)
            {
                wanted += weapon.Value.Target;
                reached += Math.Min(weapon.Value.Target, Skills[weapon.Value.Skill].Base);
            }

            return wanted <= 0.0 ? 0.0 : reached / wanted;
        }
    }

    /// <summary>
    /// How content this bot is, from nought to one: neither bored nor short of money.
    ///
    /// Both halves are already kept by the decision layer and neither is a mood in the usual sense — boredom
    /// is what grows when nothing is happening, and need is a fact about the purse against what this bot was
    /// about to try to do. One number out of the two is for a person reading a table; the bot itself never
    /// uses it.
    /// </summary>
    public double Mood
    {
        get
        {
            // <b>A class may be content by something else, and one is.</b> Both halves below are about this
            // bot's own circumstances — how long since it worked, and whether it can afford what it was about
            // to try — and both read backwards on a bot that is paid nothing on purpose: need is nought
            // for ever because it buys almost nothing, and boredom never falls because relief comes from
            // being paid. See BotClass.Grieves. The question asked instead is about the island, which is
            // the only thing the Baron's contentment was ever meant to be about.
            if (Class is { Grieves: true })
            {
                return Grief();
            }

            var urges = Resolve.Urges;

            return Math.Clamp(1.0 - (urges.Boredom + urges.Need) / 2.0, 0.0, 1.0);
        }
    }

    /// <summary>
    /// Contentment for a class that grieves: one, less what is left standing.
    ///
    /// <para>
    /// Every one of the dead within reach whose ground has not been harrowed off the board takes a share off
    /// it, and a shard where nobody has died anywhere reads as one. The dead never decay and never fall on
    /// their own, so this only ever comes down when somebody deals with the ground — which is why one number
    /// answers both halves of what he grieves over: the deaths of others, and the squares nobody has
    /// cleared.
    /// </para>
    /// </summary>
    private double Grief()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return 1.0;
        }

        var standing = BotPeril.Unavenged(map, Location, BotPopulation.Roam);

        return Math.Clamp(1.0 - standing / (double)Math.Max(1, GriefAt), 0.0, 1.0);
    }

    /// <summary>
    /// How many unavenged dead count as complete misery.
    ///
    /// Four, and it is a scale rather than a threshold: it decides how fast contentment falls, not whether
    /// anything happens. Nothing branches on it.
    /// </summary>
    public static int GriefAt { get; set; } = 4;

    /// <summary>
    /// Makes this thing a bot of the given class: a face, a build, a trade it has started rather than
    /// finished, and the kit that goes with it.
    ///
    /// <para>
    /// <b>The kit is handed over before the weapon skill is set, and the order is the point.</b> Which blade
    /// a class ends up with is a roll made by <see cref="BotOutfit"/>, and the skill that swings it comes
    /// back on the bond — so asking the bond afterwards is the only way to train the weapon the bot actually
    /// holds. The first version set the skill from the profile and rolled the weapon separately, which is
    /// how it produced bots trained in swords carrying maces.
    /// </para>
    /// </summary>
    public void Become(BotClass klass, string name, bool female) => Become(klass, name, female, null);

    /// <summary>The people this bot was born of, by its name. See BotPeoples.</summary>
    public BotPeoples.People People => BotPeoples.Of(Name);

    /// <summary>The same, with the people: the engine's race, the body, the skin and hair the race deals, a beard where the people wears one. See BotPeoples (29.09.2026).</summary>
    public void Become(BotClass klass, string name, bool female, BotPeoples.People people)
    {
        if (klass == null)
        {
            return;
        }

        // <b>Without this a dead bot is not a ghost, it is gone.</b> The engine's own death path reads
        // <c>Mobile.OnDeath</c> as "if this is not a player, delete it" — so a bot with the flag unset leaves a
        // corpse and is removed from the world outright, and the population quietly shrinks every time
        // something wins a fight. Nothing sets it for us: <c>PlayerMobile</c>'s constructor does not, because
        // for a real player it is set when a client attaches, and a bot never has one.
        //
        // It is also what <c>Alive</c> is computed from — <c>!Deleted && (!Player || !Body.IsGhost)</c> — so
        // with it unset a dead bot reads as alive, the revive refuses on that very check without a word, and
        // the clock skips the bot for ever because it is still marked fallen. That is the motionless bot at
        // the graveyard, and it is the same one line.
        Player = true;

        Class = klass;

        Name = name;
        Female = female;

        // The race before the body: the body is the race's, and the client draws an elf only as body 605/606.
        Race = people?.Race ?? Race.Human;
        Body = female ? Race.FemaleBody : Race.MaleBody;
        Hue = Race.RandomSkinHue();
        HairItemID = Race.RandomHair(female);
        HairHue = Race.RandomHairHue();

        if (people is { Beards: true } && !female)
        {
            FacialHairItemID = Race.RandomFacialHair(false);
            FacialHairHue = HairHue;
        }

        // Before anything is handed over: everything below needs somewhere to put things.
        AddItem(new Backpack { Movable = false });

        Build(klass);
        Learn(klass);
        LearnCommon();

        // Whose company it keeps, which the engine itself understands: guildmates read as Notoriety.Ally,
        // so this is the one standing statement about which bots are on the same side. See BotGuilds.
        BotGuilds.Enrol(this);

        Bond = BotOutfit.Give(this, klass);

        Clothe();
        LearnWeapon();

        // A temperament: the same points, leaned towards one skill of the class's own (01.10.2026, Patrick's point 7). After the
        // weapon, because a swordsman leans into the blade the roll handed it; the seasoned classes are born as they declare.
        BotBirth.Lean(this, klass);

        Hits = HitsMax;
        Stam = StamMax;
        Mana = ManaMax;

        // <b>Said once, with the numbers, because a class that claims to be born finished is a claim and not
        // a fact until something reads it back.</b> Everything else about a bot's build can be inferred from
        // watching it work for an hour; "it arrived at Expert" cannot, and a silent failure here would look
        // exactly like a captain having an unlucky day. This shard's own rule is that the thresholds a
        // subsystem actually runs with belong in its startup lines, and a birth meant to be unlike every
        // other birth is a startup line.
        if (klass.Seasoned)
        {
            var sb = ValueStringBuilder.Create(160);

            try
            {
                Recite(ref sb, klass);

                logger.Information(
                    "{Name} was raised a seasoned {Class}, holding its trade already: {Skills}",
                    Name,
                    klass.Name,
                    sb.ToString()
                );
            }
            finally
            {
                sb.Dispose();
            }
        }
    }

    /// <summary>The skills a seasoned bot actually woke up with, read back off the bot rather than off the class.</summary>
    private void Recite(ref ValueStringBuilder sb, BotClass klass)
    {
        var wanted = klass.Skills;
        var said = 0;

        for (var i = 0; i < wanted.Count; i++)
        {
            var skill = Skills[wanted[i].Skill];

            if (skill == null)
            {
                continue;
            }

            if (said++ > 0)
            {
                sb.Append(", ");
            }

            sb.Append(skill.Info.Name);
            sb.Append(' ');
            sb.Append(skill.Base, "F1");
        }

        if (said == 0)
        {
            sb.Append("nothing at all, which is a defect");
        }
    }

    /// <summary>
    /// One turn. Three calls, and the order is the whole of the contract between the subsystems.
    ///
    /// <para>
    /// Decide first, because deciding may put a new destination at the bottom of the journey. Then take one
    /// step towards whatever the journey now holds. Then report what that step did, because two of its
    /// outcomes — proven unreachable, and getting nowhere — are facts the decision layer has to learn from,
    /// and it cannot see them any other way.
    /// </para>
    /// </summary>
    /// <summary>
    /// Lets go of a creature this bot is nominally fighting and cannot possibly hit.
    ///
    /// <para>
    /// <b>Combat here is a reflex and walking is work, and nothing joins the two.</b> A bot is put into
    /// combat by being attacked, by a company's focus, or by an undertaking that has since ended — and once
    /// its combatant is set, nothing ever unsets it. If there is no work in hand there is nothing to close
    /// the distance either, so the bot stands where it is, in warmode, swinging at something it cannot
    /// reach, for as long as that creature lives.
    /// </para>
    ///
    /// <para>
    /// <b>Measured 02.09.2026.</b> Lysa the Warrior stood in Britain Graveyard holding no work at all,
    /// "fighting" a wraith eighteen tiles away with a weapon that reaches one, twenty-four seconds without
    /// its target's health falling once — and two of the three bots then in a fight anywhere on the shard
    /// were in the same state. It also blinds the debugger's own roll-call, which excuses anything in a
    /// fight from the stuck test on the reasonable grounds that a bot trading blows is busy: a phantom fight
    /// is therefore a bot that cannot be reported as stuck by anything.
    /// </para>
    ///
    /// <para>
    /// Only with nothing in hand. A bot walking to its quarry is out of reach the whole way, and that is the
    /// errand working; the deed is what will close the distance, and there is no deed here.
    /// </para>
    /// </summary>
    private void Unfixate()
    {
        if (Resolve.Deed != null || Combatant is not Mobile foe)
        {
            return;
        }

        var reach = Math.Max(1, Weapon?.MaxRange ?? 1);

        if (foe is { Deleted: false, Alive: true } && foe.Map == Map && InRange(foe.Location, reach))
        {
            return;
        }

        Combatant = null;
        Warmode = false;
        Unfixated++;
    }

    /// <summary>Fights let go of because nothing could ever have reached them. For the summary.</summary>
    public static long Unfixated { get; private set; }

    public void Beat()
    {
        if (Deleted || !Alive || Map == null || Map == Map.Internal)
        {
            return;
        }

        // Before deciding: a bot standing at a counter puts away what it is not going to spend. Cheap — a
        // count of the purse and one distance check — and it is the only moment the question can be asked,
        // because nothing here would ever choose to make the trip on its own. See BotPurse.
        BotPurse.Bank(this);

        // The crown's money, for the one class that is given some. Its own clock inside, and it costs a
        // dictionary lookup for everybody else. See BotStipend for why a second faucet is allowed once.
        BotStipend.Keep(this);

        // (Goods paid for were fetched off the auction's board here, and a stall's price was moved towards the bids; the
        // auction went on 01.10.2026 and goods now arrive with a carrier who walks them over. See BotDeliver.)

        // A crafter saying out loud what it is short of. Here rather than as an errand: an order is half a minute of paperwork
        // and it loses every auction it enters against a bench that pays three hundred a minute. See BotStores.
        BotStores.Keep(this);

        // And supper, which is a condition like the rest of this family: eating is no journey, and an
        // errand for it would lose every auction it entered and never happen. See BotMeal.
        BotMeal.Keep(this);

        // And a night well slept, which regenerates for a while afterwards. See BotInns.
        BotInns.Regen(this);

        // And its beasts: set on what it fights, called back, fed. See BotPets (Patrick's third addition of 29.09.2026).
        BotPets.Keep(this);

        // And what it carries and wears: an unidentified thing identified, a death robe taken off. See BotTidy.
        BotTidy.Keep(this);

        // A horse, bought out of a trip this bot was already making and called up whenever it is on foot.
        // Both are conditions rather than events, exactly like being dressed and like banking, and both cost
        // a class flag for everybody who does not ride. See BotStable.
        BotStable.Keep(this);
        BotStable.Ride(this);

        // <b>Wearing the right thing is a condition, not an event, and it was written as an event.</b>
        // BotArms made this exact argument about weapons a week ago — "being armed was treated as an event
        // and it is a condition" — and armour was left on the old footing: a re-arm happened at birth, after
        // a death, after a shopping trip and when a delivery was fetched, and at no other moment. So a
        // cuirass off a corpse, a helm out of a company's share-out and a pair of gloves handed over by any
        // route nobody thought of all sat in the pack until the bot happened to die. Asked on a clock, every
        // one of those closes at once, including the ones not yet invented.
        Dress();

        Trickle();

        Gasp();

        Unfixate();

        Rank();

        // Timed in two halves, so "the bots are slow" can be answered with which half. See BotBeat.Describe.
        var began = System.Diagnostics.Stopwatch.GetTimestamp();

        BotWill.Decide(this);

        var decided = System.Diagnostics.Stopwatch.GetTimestamp();

        BotWill.Spent(decided - began);

        // What the bot can see before it steps: a creature it cannot beat near its way is gone round, waited out or turned back
        // from before it is set upon. Timed with the walking. See BotSight (03.10.2026, sight13).
        BotSight.Look(this);

        var result = BotWalk.Advance(this, Journey, Running);

        LastWalk = result;

        BotWalk.Spent(System.Diagnostics.Stopwatch.GetTimestamp() - decided);

        Cross();

        BotWill.Note(this, result);

        // A refusal is proof that nothing at that destination can be reached from here. Counted, and cleared
        // by any step at all — so this rises only for a bot that is getting nowhere in the literal sense.
        //
        // <b>A stalled step is not a refusal.</b> Stalled is "the engine would not take this step and the
        // next tick tries again" — a bot on the next tile, a door, a corner of a fight — and it was counted
        // here until 03:15 on 14.09.2026, so twelve of them in a row, two and a half seconds boxed in at a
        // claim square or in the press of a battle, read as stranded and the bot was carried home with its
        // errand wiped. Nine carries in twenty-two minutes under build 16, every one of them from open
        // ground ("TooBig", the tile open in most directions), none from a pocket. A bot that really cannot
        // move is the stall watch's, which waits minutes rather than ticks.
        switch (result)
        {
            case BotWalkResult.Refused:
            case BotWalkResult.GaveUp:
                {
                    if (++Refusals >= BotPopulation.StrandedLimit)
                    {
                        BotPopulation.Rescue(this);
                    }

                    break;
                }
            case BotWalkResult.Stepped:
            case BotWalkResult.Arrived:
            case BotWalkResult.Improvised:
                {
                    Refusals = 0;

                    break;
                }
        }
    }

    /// <summary>
    /// The mana a caster's class gives it back, paid out on its own clock.
    ///
    /// <para>
    /// <b>Designed, documented at length, and connected to nothing.</b> <c>BotClass.ManaTrickle</c> carries a
    /// careful ruling about staves and warrior-mages, the mage's staff is hued blue and the healer's green
    /// precisely so that a watcher can tell which trickle is which — and not one line of this project ever
    /// called it. So the only mana a caster on this shard has ever had back is the engine's own regeneration,
    /// which on this era is <c>(Int + Meditation) / 2</c> with no meditation bonus, because nothing makes a
    /// bot meditate either. Cedric stood in a fight with a troll on 25.08.2026 holding <em>two</em> of fifty
    /// mana and one attack spell in a book of seven, and the log said so in as many words while every
    /// number in it was correct.
    /// </para>
    ///
    /// <para>
    /// Only while alive and only up to the pool, and deliberately not while the bot is at full mana — the
    /// point of a trickle is to get a caster back into a fight, not to be a number that ticks.
    /// </para>
    /// </summary>
    private void Trickle()
    {
        var klass = Class;

        if (klass == null || Mana >= ManaMax)
        {
            return;
        }

        if (Core.TickCount - _trickledTick < BotClass.ManaTrickleIntervalMs)
        {
            return;
        }

        _trickledTick = Core.TickCount;

        // Asked of what is actually in the hands, every time, because a caster that has drawn a blade or had
        // its staff knocked about is a caster whose trickle has changed. The class's own ruling decides what
        // holding one is worth; this only reports the fact.
        var given = klass.ManaTrickle(Weapon is BaseStaff);

        if (given > 0)
        {
            Mana = Math.Min(ManaMax, Mana + given);
        }
    }

    private long _trickledTick;

    /// <summary>
    /// The share of health at which a bot swallows a bottle whatever else it is doing.
    ///
    /// Fifteen per cent: two or three more blows. Deliberately far below <c>BotMend.Gulp</c>, which is the
    /// threshold for <em>choosing</em> to drink as part of looking after itself — this is the one for having
    /// run out of choices.
    /// </summary>
    public static double Critical { get; set; } = 0.15;

    /// <summary>
    /// A bottle at death's door, as a reflex rather than as a decision.
    ///
    /// <para>
    /// <b>The rule was already right and it was in the wrong place.</b> <c>BotMend</c> says it plainly — a
    /// potion is the only mending in this game that works while something is hitting you: a cast is destroyed
    /// by a blow, a bandage slips with every one, a bottle cannot be interrupted at all. But the only code
    /// that ever reached for one lived inside the <em>mending</em> undertaking, and mending has to win an
    /// auction on the failing rung against <c>BotFugitive</c>. When flight wins — which it often does, and
    /// should — the bot turns and runs holding two unopened bottles, and one flight in six ends in a corpse.
    /// Aldric was kicked to death by a group on 25.08.2026 without drinking either of them.
    /// </para>
    ///
    /// <para>
    /// So it is not a decision at all. This shard's own rule is that survival belongs to reflexes and not to
    /// the deciding layer — the minds are told as much in their standing instruction — and a swallow is the
    /// most reflexive act there is. It costs a share and a pack lookup on a beat, and it fires only for a bot
    /// that is about to stop existing.
    /// </para>
    /// </summary>
    /// <summary>
    /// Bots that dropped under the bar, and what came of it. Three named outcomes, not one silence.
    ///
    /// <para>
    /// <b>"Nobody drinks their potions" is three completely different faults and they all looked the same.</b>
    /// The reflex was written, correct and called every beat — and there was no way to tell whether it was
    /// never firing, firing and finding an empty pack, or firing and being refused by the engine's own
    /// cooldown. Counted per beat rather than per bot, deliberately: a bot held under the bar for ten
    /// seconds is ten readings of "still dying", which is the thing worth seeing.
    /// </para>
    /// </summary>
    public static long Gasps { get; private set; }

    public static long Drank { get; private set; }

    public static long Dry { get; private set; }

    public static long Refused { get; private set; }

    /// <summary>Swallows that only happened because the weapon was put away first.</summary>
    public static long Freed { get; private set; }

    /// <summary>Clears the potion counters. Called with the rest of the population's own.</summary>
    public static void ForgetGasps()
    {
        Gasps = 0;
        Drank = 0;
        Dry = 0;
        Refused = 0;
        Freed = 0;
    }

    public static string DescribeGasps() =>
        Gasps == 0
            ? "nobody has been under a sixth of their health"
            : $"{Gasps} times a bot fell under {Critical:P0} health: {Drank} bottles swallowed ({Freed} of them after putting the weapon away), {Dry} found no bottle in the pack, {Refused} were refused by the engine";

    /// <summary>Whether this bot is already known to be under the bar. See <see cref="Gasp"/>.</summary>
    private bool _gasping;

    private void Gasp()
    {
        if (HitsMax <= 0 || Hits > HitsMax * Critical)
        {
            _gasping = false;

            return;
        }

        // <b>Counted once per fall rather than once per beat, and the first version of this lied.</b> A beat
        // is a fifth of a second per bot: one bot lying at a tenth of its health for a minute produced three
        // hundred readings and looked, in the summary, like the whole population dying at once — 252 against
        // 17 the window before, with a single death in each. What is worth counting is how often a bot went
        // under the bar, which is an event, not how long it stayed there, which is a duration.
        if (!_gasping)
        {
            _gasping = true;
            Gasps++;
        }

        var bottle = BotMend.Bottle(Backpack, BotPotionKind.Heal);

        if (bottle == null)
        {
            Dry++;

            return;
        }

        // CanDrink carries the engine's own cooldown, so this is safe to ask on every beat: a bot that has
        // just drunk simply gets false until the delay is up.
        if (!BotMend.Swallow(this, bottle))
        {
            // <b>And the commonest refusal is a full pair of hands, which is every bot on this shard.</b>
            // The engine wants a free hand for a bottle — "You must have a free hand to drink a potion" —
            // and a bot holding a blade and a shield, a bow, or a halberd has none, ever. So the reflex fired
            // correctly, found its bottle, and was turned down every single time: 27 beats under fifteen per
            // cent in one window, 27 refusals, nought swallowed. The only class it ever worked for was the
            // brawler, who fights with his fists. Putting the weapon away for the swallow is what a person
            // does, and Rearm picks it back up on the next beat.
            if (Stow() && BotMend.Swallow(this, bottle))
            {
                Drank++;
                Freed++;

                return;
            }

            Refused++;

            return;
        }

        Drank++;

        logger.Information(
            "{Name} was at {Share:P0} and drank a bottle where it stood",
            Name,
            Hits / (double)HitsMax
        );
    }

    /// <summary>
    /// Something hit it. Everything a bot has to be *told* rather than notice happens here.
    ///
    /// <para>
    /// <b>Told, not observed, and this is the rung the first version never fired.</b> A caster strikes from
    /// eight tiles and never closes, so every test of the form "is something next to me" is a test that
    /// never fires: six bots stood in a ring while a lich killed them one at a time, and not one rung of
    /// their survival ladder noticed.
    /// </para>
    ///
    /// <para>
    /// The decision is <see cref="BotThreat"/>'s and it is binary: put the road aside and deal with this, or
    /// keep walking and hit back on the move. Standing still is not an option at any number.
    /// </para>
    /// </summary>
    /// <summary>
    /// A blow inside a duel is not a crime. Everything else is the engine's rule: an innocent struck outside one
    /// flags the striker, and a murderer's victim is as innocent as the engine says it is. See <c>BotDuel</c>.
    /// </summary>
    public override bool IsHarmfulCriminal(Mobile target) =>
        !BotDuel.Between(this, target) && base.IsHarmfulCriminal(target);

    /// <summary>
    /// Something moved near this bot. The engine raises this for every body within its update range of a mover, so what is
    /// asked here is two comparisons: is it a creature, and has it taken this bot as its combatant. Only then is anybody told.
    ///
    /// <para>
    /// <b>The one event the engine gives a body about a creature coming for it before the blow (06.10.2026, react14).</b> A
    /// creature's AI sets its combatant and walks; nothing is raised on the one it chose. Its steps are, on everybody near.
    /// See <see cref="BotReflex.Seen"/>.
    /// </para>
    /// </summary>
    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (m is BaseCreature creature && ReferenceEquals(creature.Combatant, this) && Alive)
        {
            BotReflex.Seen(this, creature);
            Menace(creature);
        }
    }

    /// <summary>Whether a creature seen coming for a bot puts it on the rung a blow does. A switch, for a running shard.</summary>
    public static bool SeesComing { get; set; } = true;

    /// <summary>
    /// How near a creature that has taken this bot as its combatant must have come for the bot to answer it before it strikes.
    ///
    /// Eight tiles: two seconds or so for most things that run, which is time to square up or to be gone, and near enough that
    /// the creature is plainly coming rather than merely looking. The instrument's first reading put the average first sight
    /// at seven and a half.
    /// </summary>
    public static int MenaceRange { get; set; } = 8;

    /// <summary>Whether a creature that strikes from range is answered at its own reach rather than at <see cref="MenaceRange"/>. A switch.</summary>
    public static bool MenaceByReach { get; set; } = true;

    /// <summary>
    /// How near this creature must have come to be answered before it strikes: <see cref="MenaceRange"/> for what has to close,
    /// and for a caster or an archer the distance it notices a body at (<c>RangePerception</c>), which is the distance it
    /// begins striking from, up to the distance the rung can answer anything at (<c>BotDefender.Reach</c>, twelve: past it
    /// neither the fight nor the flight sees the creature, and a bot on the rung with nothing to take is worse than one off it).
    /// A flat eight tiles is inside a mage's first spell.
    /// </summary>
    public static int MenaceRangeOf(BaseCreature creature) =>
        MenaceByReach && creature is { AI: AIType.AI_Mage or AIType.AI_Archer }
            ? Math.Clamp(creature.RangePerception, MenaceRange, Math.Max(MenaceRange, BotDefender.Reach))
            : MenaceRange;

    /// <summary>How often, at most, a bot not yet on the rung asks whether what is coming for it is something to answer.</summary>
    public static int MenaceAskMs { get; set; } = 500;

    /// <summary>Bots put on the rung by a creature seen coming, before any blow; and looks that left the bot where it was.</summary>
    public static long Menaces { get; private set; }

    public static long MenacesPassed { get; private set; }

    private bool _menaceAsked;

    private long _menaceAskedTick;

    /// <summary>
    /// <b>A creature that has taken this bot as its combatant has stepped within <see cref="MenaceRange"/> of it: the bot is on
    /// the Hunted rung from now, not from the blow (06.10.2026, react14).</b> What the rung then decides is what it has always
    /// decided, a few seconds sooner: <c>BotDefender</c> offers the fight when the bot and its own near can win it
    /// (<c>BotThreat.Decide</c>), cries out and runs for a town when they cannot, and <c>BotFugitive</c> offers the flight from
    /// what is plainly too much while the bot still has all its health and the creature has not closed.
    ///
    /// <para>
    /// Not for a member of a company, whose company answers what attacks it; not for the bot's own quarry coming at it, nor
    /// for a bot already running. Renewed by every step the creature takes while the rung stands, at the cost of two
    /// comparisons; the odds are asked by the rung's proposers on the bot's decision, never here.
    /// </para>
    /// </summary>
    private void Menace(BaseCreature creature)
    {
        if (!SeesComing || Class is { Bidding: false } || creature.Controlled || creature.Summoned
            || !InRange(creature.Location, MenaceRangeOf(creature)))
        {
            return;
        }

        var resolve = Resolve;

        if (resolve == null)
        {
            return;
        }

        var now = Core.TickCount;

        // <b>A member tells its company (06.10.2026, react16).</b> Its own rung is its company's business; the company is what
        // answers, and it is told now rather than at the blow. See BotSquads.Coming.
        if (Squad != null)
        {
            if (!_menaceAsked || now - _menaceAskedTick >= MenaceAskMs)
            {
                _menaceAsked = true;
                _menaceAskedTick = now;

                if (BotThreat.Hostile(this, creature))
                {
                    BotReflex.Warned(this);
                    BotSquads.Coming(this, creature);

                    // <b>And the member squares up itself to what it and its mates near can beat (react17).</b> As a lone bot
                    // does, and as the blow's reflex does for a member already; the press leaves it on the thing
                    // (BotSquad.DefendReach). The company's enemy is the company's; this member only stops being passive.
                    var span = Math.Max(NoticeRange, Math.Max(Math.Abs(creature.X - X), Math.Abs(creature.Y - Y)));

                    if (MembersMeet && Resolve.Deed is not BotBolt && !BotLadder.Failing(this) && !BotQuarry.Shunned(creature)
                        && !ReferenceEquals(Squad.Focus, creature) && BotThreat.Decide(this, span) == BotStand.Fight)
                    {
                        MembersMet++;

                        Engage(creature);
                        BotReflex.Met(this);
                    }
                }
            }

            return;
        }

        // Still coming: the rung is kept up without asking anything again. A second creature coming while the rung stands is on
        // it too, and the stopwatch is told so (105 first blows on build 405 read "inside the range and never put on the rung",
        // and this was one of the ways).
        if (resolve.Menaced && now - resolve.MenaceTick < BotLadder.MenacedMs)
        {
            BotWill.Menaced(this);
            BotReflex.Warned(this);

            return;
        }

        // Being struck already: the rung is this one, and the blow's own clock keeps it.
        if (BotLadder.Hunted(resolve))
        {
            return;
        }

        if (_menaceAsked && now - _menaceAskedTick < MenaceAskMs)
        {
            return;
        }

        _menaceAsked = true;
        _menaceAskedTick = now;

        var deed = resolve.Deed;

        if (deed is BotBolt || ReferenceEquals(deed?.Foe, creature) || !BotThreat.Hostile(this, creature))
        {
            MenacesPassed++;

            return;
        }

        Menaces++;

        BotWill.Menaced(this);
        BotReflex.Warned(this);
        Hurry();

        // <b>And it squares up now to what it and its own near can beat, as it does at a blow.</b> The reflex in OnDamage, a
        // few seconds sooner: the same question (BotThreat.Decide), the same answer (Engage: the strongest thing here is the
        // target, and the body walks to it over whatever road it was on). What it cannot beat is left to the rung, which runs
        // from it or cries out; a creature no road of ours reaches is not walked at.
        // Asked as far out as the thing stands: one that strikes from twelve tiles is not weighed by a look of ten.
        var look = Math.Max(NoticeRange, Math.Max(Math.Abs(creature.X - X), Math.Abs(creature.Y - Y)));

        if (resolve.Deed is not BotBolt && !BotLadder.Failing(this) && !BotQuarry.Shunned(creature)
            && BotThreat.Decide(this, look) == BotStand.Fight)
        {
            MenacesMet++;

            Engage(BotThreat.Strongest(this, NoticeRange) ?? creature);
            BotReflex.Met(this);
        }
    }

    /// <summary>Of <see cref="Menaces"/>, the bots that squared up at once to what was coming, having judged it beatable.</summary>
    public static long MenacesMet { get; private set; }

    /// <summary>Whether a company's member squares up itself to a creature coming for it that it and its mates near can beat. A switch.</summary>
    public static bool MembersMeet { get; set; } = true;

    /// <summary>Members that squared up themselves to what was coming for them. See <see cref="Menace"/>.</summary>
    public static long MembersMet { get; private set; }

    /// <summary>
    /// What a loss of health sets going at once, asked before the health is taken off: the bot's own decision when the blow
    /// opens a bout or takes it under its failing line (<see cref="Hurry"/>), and the nearest of ours able to mend when it
    /// leaves the bot a patient (<c>BotSurgeon.Call</c>).
    /// </summary>
    private void Alarm(int amount, bool opens)
    {
        if (HitsMax <= 0 || amount <= 0)
        {
            if (opens)
            {
                Hurry();
            }

            return;
        }

        var after = Hits - amount;
        var failAt = HitsMax * BotLadder.Fraction(this);
        var fails = Hits > failAt && after <= failAt;

        if (opens || fails)
        {
            Hurry();
        }

        // In a company the answer to a member failing is the company's: its medics and its press, now. See BotSquads.Hurry.
        if (fails && Squad != null)
        {
            BotSquads.Hurry(Squad);
        }

        var hurtAt = HitsMax * BotMend.Hurt;

        // The failing fact is passed in: Hits still read the blow away here (OnDamage runs before the engine writes them), so the
        // surgeon cannot ask the patient (13.10.2026, w5 review B).
        if (fails || Hits >= hurtAt && after < hurtAt)
        {
            BotSurgeon.Call(this, fails);
        }
    }

    /// <summary>The cure's own counters for the <c>Reflexes:</c> line: what was hurried, who was put on the rung early, who was called.</summary>
    public static string DescribeAlarms() =>
        $"{BotBeat.Hurried} decisions taken between turns for an event that would not wait; "
        + $"{Menaces} bots put on the hunted rung by a creature seen coming within {MenaceRange} tiles before any blow, {MenacesMet} of them squaring up to it at once as beatable, and {MembersMet} members of a company squaring up themselves ({MenacesPassed} looks left alone: its own quarry, a flight already begun, or nothing it may strike); "
        + $"{BotWill.Alerts} bots told to look up at once for somebody near; {BotSurgeon.Describe2()}; {BotCry.DescribeCalls()}; {BotDefender.DescribeStands()}; {BotSquads.DescribeAnswers()}";

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        base.OnDamage(amount, from, willKill);

        if (from == null || from == this || Deleted || !Alive || willKill)
        {
            // A poison's tick crosses a line as surely as a blow: the stopwatch hears every loss of health. See BotReflex.
            BotReflex.Damaged(this, amount, null, willKill);

            if (!Deleted && Alive && !willKill)
            {
                Alarm(amount, false);
            }

            return;
        }

        // Whether this blow opens a bout: asked before the blow is written down. See Alarm.
        var opens = !BotLadder.Hunted(Resolve);

        BotWill.Hurt(this);
        BotSquads.Note(this, from);
        BotVoice.Struck(this, from);

        // <b>A creature's blow on guarded ground is answered by a call for the guards, whatever the bot is doing (08.10.2026,
        // guards30).</b> Only a run for a town called them, and 10 of the 21 deaths against the map on build 429 fell inside a
        // town after the run had ended: a plain flight that stopped there, a mend there (Wilhelm, Ysolde, Norbert). See
        // BotHaven.CallGuards, which keeps one clock per bot with the runner's own calls.
        if (from is BaseCreature { Controlled: false } && BotHaven.Guarded(Map, Location))
        {
            BotHaven.CallGuards(this, from, "struck in town");
        }

        // The stopwatch: a blow starts the clock that the first decision about it stops. After the company has heard the blow,
        // so that a company with an enemy reads as the answer it is. See BotReflex (06.10.2026).
        BotReflex.Damaged(this, amount, from, false);

        // And what will not wait for this bot's turn: its own decision at the first blow of a bout and at the failing line, and
        // a healer's when the blow leaves it a patient. See Alarm (06.10.2026).
        Alarm(amount, opens);

        // Where the island is dangerous, learned from the one place it cannot be guessed at. See BotPeril:
        // this hook and the death hook are the only two facts that map, and a blow costs it a dictionary
        // lookup and two additions.
        BotPeril.Struck(Map, Location);

        // The same blow against the island's standing reputation rather than against this minute's danger.
        // See BotQuad: the two maps answer different questions and are both fed from here, because this hook
        // and the death hook below are the only two places on this shard that know a bot was hurt.
        BotQuad.Struck(Map, Location, false);

        // And this crossing is no longer a free one. See _quadClean.
        _quadClean = false;

        // On the ground, by order — and the half of the order that matters is the rest of the sentence: a
        // rider that has been set upon fights. Here rather than in any decision, so it happens whether or not
        // anything is thinking about this bot.
        BotStable.Throw(this);

        // And the same blow, remembered against this bot rather than against the ground. See
        // BotResolve.Beaten: it is the only honest answer to "is armour worth anything to you".
        Resolve.Bruise(Core.TickCount);

        // <b>Hitting back is a reflex and not a decision, and that distinction is worth twenty seconds of
        // being beaten.</b> Fighting back used to be a piece of work like any other — the rescue, "hitting
        // back at a harpy" — which meant it had to win an auction on the bot's next decision. For most of the
        // population that is a moment; for a bot with a mind behind it the decisions come every twenty
        // seconds, and an orc that caught one stood there hitting it for exactly that long before it turned
        // round. Nobody waits for a review to swing back.
        //
        // <b>It costs nothing, which is why it may be unconditional.</b> Combat in this engine is its own
        // timer: a bot with a combatant swings while it walks, works, flees or stands, and setting one does
        // not change what the bot is <em>doing</em>. That was the confusion the old ordering rested on — it
        // guarded this line with the same test that guards picking a fight, and the two are not the same act.
        // A bot running for its life still swings at what it passes; BotBolt clears the combatant on its own
        // beat, so flight still wins where flight was chosen.
        //
        // <b>Except on a run for a town, and except for a bot outmatched that has not struck its attacker (02.10.2026).</b>
        // The guards of a town come for a criminal, and a creature is criminal for striking a bot that is innocent to it —
        // which the bot stops being the moment it swings back, for two minutes (Notoriety: CheckAggressor; see BotHaven).
        // One reflex swing therefore makes the guards blind to the thing the bot is running from; a bot that has judged it
        // cannot win (BotThreat.Outmatched) keeps its hands down, and a bot on the run for a town never raises them. The
        // odds are asked here only for a bot that would otherwise have been asked them below.
        var failing = BotLadder.Failing(this);
        var fleeing = Resolve.Deed is BotBolt;
        var stand = failing || fleeing ? BotStand.Nothing : BotThreat.Decide(this, NoticeRange);

        BotReflex.Stood(this, stand);

        if (BotBolt.HoldsSwing && from is BaseCreature)
        {
            if (Resolve.Deed is BotBolt { ForTown: true })
            {
                return;
            }

            if (stand == BotStand.Outmatched && !BotHaven.Aggressed(this, from))
            {
                BotBolt.SwingsHeld++;

                return;
            }
        }

        Warmode = true;
        Combatant = from;

        // <b>A bot whose health is going does not answer a blow by picking a fight.</b> The ladder has it on
        // Failing and something up there is getting it out — mending or running — and this reflex was quietly
        // undoing both: it squared the bot up to the largest thing in the field on every blow, which is a bot
        // that has stopped moving. That is how one of these ended up standing in a ring of three at a third of
        // its health, and the ladder's own note says why it is wrong in as many words.
        //
        // Everything below this line is <em>choosing</em> a fight, which is a decision. Hitting back at
        // whatever is already hitting you is above it, and always happens.
        // <b>The King's Rangers never do any of this, and that is the whole of their doctrine.</b> They do
        if (failing)
        {
            return;
        }

        // Already leaving. A blow landing is not news to a bot running from the thing that landed it, and the
        // reflex below would rebase its journey onto the creature — walking it back into the fight it decided
        // to leave, at the one moment it has no health to spare for the round trip.
        if (fleeing)
        {
            return;
        }

        if (stand == BotStand.Nothing)
        {
            return;
        }

        // Outmatched keeps the errand: several times over is exactly when walking on beats standing. What
        // gets hit back at is then whatever is actually swinging — squaring up to the worst thing in the
        // field is not hitting back on the move, it is starting the fight that was just refused. The
        // combatant is already this attacker, set above, so there is nothing left to do here.
        if (stand != BotStand.Fight)
        {
            return;
        }

        // The strongest thing here, not whatever hit last: a graveyard's nearest resident is a skeleton, and
        // the first version's parties reliably committed to the skeleton while the lich went on casting.
        // Falling back to the attacker matters too — Strongest only counts creatures, and a bot can be hit by
        // something that is not one.
        Mobile quarry = BotThreat.Strongest(this, NoticeRange) ?? from;

        Engage(quarry);
    }

    /// <summary>
    /// Squares up to something: makes it the target and walks the body to it.
    ///
    /// Only once per quarry. Every blow would otherwise push another errand, and four blows would fill the
    /// queue with the same fight four times over.
    /// </summary>
    private void Engage(Mobile quarry)
    {
        if (quarry is not { Deleted: false, Alive: true } || quarry.Map != Map)
        {
            return;
        }

        Combatant = quarry;
        Warmode = true;

        if (!ReferenceEquals(Journey.Current?.Follow, quarry))
        {
            Journey.Interrupt(Map, quarry, BotArrival.Beside, "fight");
        }

        // Squared up on the blow: the decision the stopwatch stops at. See BotReflex.
        BotReflex.Engaged(this);
    }

    /// <summary>
    /// A ranger looks up, sees something hostile, and goes for it. No decision, no auction, no weighing.
    ///
    /// <para>
    /// <b>By order, and it is the difference between a patrol and five bots jogging past a war.</b> Every
    /// other bot on this shard picks its fights: it asks whether the thing is worth killing, whether it can
    /// be beaten, whether something better pays. That is right for a miner and ruinous for a company whose
    /// entire duty is to walk into unread ground and find out what lives there — a ranger that walks past a
    /// hostile has failed the errand it is on, because the whole product of the errand is knowing what is
    /// out there, and what is out there is that.
    /// </para>
    ///
    /// <para>
    /// A reflex on the beat rather than an offer to the auction, for the same reason the potion is: the
    /// auction reconsiders every several seconds and a fight starts inside one of them. It costs one spatial
    /// query on the beat, and only for bots of one class.
    /// </para>
    /// </summary>
    /// <summary>
    /// Puts whatever is in the hands into the pack, so that a bottle can be drunk. True when a hand came free.
    ///
    /// <para>
    /// Both hands, because the engine's test is for a free one and a shield occupies the other: a bot that
    /// stowed only its blade would still be refused. Nothing is dropped and nothing is unbound — the items go
    /// into the bot's own pack, and <see cref="Rearm"/> puts them back on the next beat it is allowed to.
    /// </para>
    /// </summary>
    private bool Stow()
    {
        var pack = Backpack;

        if (pack == null)
        {
            return false;
        }

        var freed = false;

        for (var i = 0; i < TwoHands.Length; i++)
        {
            var held = FindItemOnLayer(TwoHands[i]);

            if (held == null)
            {
                continue;
            }

            if (pack.TryDropItem(this, held, false))
            {
                freed = true;
            }
        }

        return freed;
    }

    /// <summary>The two layers a weapon, a shield or a staff can occupy. What the engine calls a full pair of hands.</summary>
    private static readonly Layer[] TwoHands = [Layer.OneHanded, Layer.TwoHanded];

    /// <summary>
    /// The guild's abbreviation, in front of the name rather than behind it.
    ///
    /// <para>
    /// <b>Patrick's order of 09.09.2026: he wants to see whose band a bot is in before he reads its name.</b>
    /// The engine puts it behind — <c>Mobile.AddNameProperties</c> builds the label as
    /// <c>prefix, name, suffix</c> and hangs <c>[ABC]</c> off the suffix — so the abbreviation moves by
    /// being written into the prefix instead. One cliloc, 1050045, feeds both the tooltip and the name that
    /// appears over the head on a single click, so moving it here moves it in both places at once.
    /// </para>
    ///
    /// <para>
    /// Overridden here and nowhere else, so it is true of this population and of nothing in the engine: the
    /// server assembly is not ours to edit, and a shard-wide change to how every mobile is labelled is a far
    /// larger thing than what was asked for.
    /// </para>
    ///
    /// <para>
    /// What is deliberately dropped for a bot in a guild is the <c>Lord</c>/<c>Lady</c> fame prefix, because
    /// there is one prefix and the abbreviation is now in it. No bot on this shard is near the ten thousand
    /// fame that earns it; if one ever is, this is the line that decided.
    /// </para>
    /// </summary>
    public override void AddNameProperties(IPropertyList list)
    {
        if (Guild is not Guild guild || string.IsNullOrEmpty(guild.Abbreviation))
        {
            base.AddNameProperties(list);

            return;
        }

        var title = PropertyTitle && !string.IsNullOrEmpty(Title) ? $" {Title}" : " ";

        // ~1_PREFIX~~2_NAME~~3_SUFFIX~, the same line the engine writes, with the band in front.
        list.Add(1050045, $"[{guild.Abbreviation.FixHtmlFormattable()}] \t{Name ?? " "}\t{ApplyNameSuffix(title)}");

        if (DisplayGuildTitle)
        {
            list.Add(guild.Name.FixHtml());
        }
    }

    /// <summary>A veteran's title after its name, in the world's label (07.10.2026, traits17). Untouched for everybody else. See BotTraits.Suffix.</summary>
    public override string ApplyNameSuffix(string suffix) => base.ApplyNameSuffix(BotTraits.Suffix(this, suffix));

    /// <summary>A red is refused a step from open ground into a guarded town, whatever drew the step. See <c>BotOutlaw.WalksRound</c>.</summary>
    public override bool Move(Direction d) => BotOutlaw.Steps(this, d) && base.Move(d);

    /// <summary>
    /// What this bot brought to the fight that killed it (<c>BotThreat.Power</c>), taken while it still held its weapon.
    ///
    /// <para>
    /// <b>By <see cref="OnDeath"/> the engine has already moved the weapon off the body</b> (<c>Mobile.Kill</c> empties the
    /// layers right after <see cref="OnBeforeDeath"/>), so the Kept line weighed a ghost's fists: Beatrix "fell with 204 of
    /// strength" at 03:10:24 on build 395, a bot weighed at 2163 against its roads a minute before and a minute after, and the dead
    /// read a median ten times weaker than their killers partly for it (03.10.2026). See <c>BotKept.Fell</c>.
    /// </para>
    /// </summary>
    public double PowerAtDeath { get; private set; }

    /// <summary>
    /// The poison on the body as it died. <c>Mobile.Kill</c> sets <c>Poison</c> to null after <see cref="OnBeforeDeath"/> and before
    /// <see cref="OnDeath"/>, so <c>BotBolt.Died</c>'s "poison none" read every one of build 396's 149 deaths in flight as clean,
    /// whatever had bitten them (03.10.2026, sight13).
    /// </summary>
    public Poison PoisonAtDeath { get; private set; }

    public override bool OnBeforeDeath()
    {
        PowerAtDeath = BotThreat.Power(this);
        PoisonAtDeath = Poison;

        return base.OnBeforeDeath();
    }

    /// <summary>
    /// It died. The bond decides what it keeps, the decision layer counts what the work came to, and the
    /// squad stops counting it as help.
    /// </summary>
    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        // <b>Where it fell, taken before anything moves the ghost (29.09.2026, build 319).</b> BotWill.Died ends a delve,
        // the party surfaces its members by carrying them home, and the killing field and the Kept line below read Location
        // after that: Gimrund killed at (5331, 1345) in the Orc Caves was filed at (1591, 1447) in Britain, and Liesl and
        // Dalla the same way — blood on the island's roads for deaths in a cave.
        var fellAt = Location;
        var fellMap = Map;

        // A death in flight says how the flight went. See BotBolt.Died.
        (Resolve?.Deed as BotBolt)?.Died(this, fellAt);

        // A death on guarded ground is counted with its killer, and one within a minute of reaching a town says the town did
        // not save it. See BotHaven.Died.
        BotHaven.Died(this, fellAt, fellMap);

        Fallen = true;
        FellTick = Core.TickCount;
        ReviveComplained = false;

        // Whatever it had not yet decided about what killed it is counted as that. See BotReflex.
        BotReflex.Died(this);

        // A death where it lives counts towards moving house; asked here, before a delve's end carries the ghost home.
        // See BotResidence.Fell (29.09.2026).
        BotResidence.Fell(this, Location);

        // Everything the bind does not cover is in there: the tools, the herbs, the bandages and the purse.
        // Nothing to go back for when the crown takes the body away. See the ranger branch below.
        Remains = c as Corpse;

        // A murderer killed by the Baron's patrol is caught. Asked here, while the killer is still known.
        BotOutlaw.Fell(this, LastKiller);

        // A wanted bot brought down in town is taken; an ambush counts its carrier and its bandits (07.10.2026, crime17).
        BotCrime.Fell(this, LastKiller);

        // Said plainly, every time.
        //
        // <b>A death used to be invisible unless the bot happened to be holding work.</b> The decision layer
        // logs what an undertaking came to, so a bot killed between jobs left no line anywhere — which made
        // "nobody died" and "everybody died and none of them got up" the same log. That is not a diagnosis
        // anybody can make, and it cost an argument about whether resurrection worked at all.
        logger.Information(
            "{Name} the {Class} was killed at {Where}; it should rise again in {Wait}s",
            Name,
            Class?.Name,
            Location,
            BotPopulation.ReviveMs / 1000
        );

        // Ammunition first: the corpse is the only place the count can still be trimmed from.
        BotBinding.TrimAmmunition(this, Bond, c);

        // The company it fell in, taken before its work is settled: settling an enlistment takes the member off the roster, and
        // the Kept line read "a company of 1" for every bot that died enlisted (03.10.2026). See BotKept.Fell.
        var company = Squad;

        BotWill.Died(this);

        // The death as an event and as last words, for the page and whoever is standing near.
        var felledBy = LastKiller;

        // Into its life's story, and against whoever of ours did it: a first death, a war's kill, a murder (07.10.2026, traits17).
        // Asked before the guild's grudge below, while the war it may have ended still stands. See BotBiography.Died.
        BotBiography.Died(this, felledBy, (c as Corpse)?.Killer, fellMap, fellAt);

        // A death nobody could have survived closes the ground round it at once; an ordinary one is counted towards a
        // killing field. See BotBarred.Fell (29.09.2026).
        BotBarred.Fell(fellMap, fellAt, felledBy, $"{Name} the {Class?.Name}");

        // And this bot's own record of where it dies: ground it has died on twice lately is not offered to it again for a
        // while, whatever the work. See BotBreaker.FellAt (01.10.2026, build 388).
        BotBreaker.FellAt(this, fellMap, fellAt, felledBy);

        BotEvents.Post(
            "death",
            this,
            $"killed by {felledBy?.Name ?? "something"} at ({fellAt.X}, {fellAt.Y})",
            null,
            BotJson.Object(
                w =>
                {
                    w.WriteString("killer", felledBy?.Name);
                    w.WriteString("killerKind", felledBy?.GetType().Name);
                    w.WriteString("class", Class?.Name);
                }
            )
        );

        BotVoice.Died(this, felledBy);

        // What the spawners near kept, set beside what this bot's company brought, while it is still in one. See BotKept.
        BotKept.Fell(this, fellMap, fellAt, company);

        // And what this bot had made of its killer in the minute before: never seen, beatable, a danger it walked on by, or one
        // its walk had changed for. See BotSight.Died.
        BotSight.Died(this, felledBy);

        // And the danger zones count it where it fell: evidence for the zone, and a zone that keeps killing reads deadlier.
        BotZones.Fell(fellMap, fellAt);

        // And the zone watch counts it for the map or against it, if a creature that attacks on sight did it (07.10.2026). See
        // BotZoneWatch.Died.
        BotZoneWatch.Died(fellMap, fellAt, felledBy ?? (c as Corpse)?.Killer);

        BotSquads.Leave(this);

        // <b>And whose hand it was, if it was a hand of ours.</b> Only when the killer is another bot of
        // another guild: everything on this island that kills bots is a monster, and a guild bearing a
        // grudge against an ogre would be a quarrel nobody could ever settle. Read off the corpse rather
        // than off the last blow, because the corpse is what the engine itself credits.
        if ((c as Corpse)?.Killer is BotMobile { Deleted: false } slayer && slayer != this)
        {
            BotRegard.Killed((slayer.Guild as Guilds.Guild)?.Name, (Guild as Guilds.Guild)?.Name);

            // A kill of another guild's member counts towards the slayer's standing at its guild's next election. See BotElection.
            BotElection.Slew(slayer, this);

            // And whether it happened inside a square somebody is claiming, which is the one question
            // "did another guild beat them" is answered from. See BotClaim.Bled.
            BotClaim.Bled(
                Map,
                Location,
                (slayer.Guild as Guilds.Guild)?.Name,
                (Guild as Guilds.Guild)?.Name
            );
        }

        // <b>A bot killed by another bot says nothing about what lives on the ground.</b> The peril map and the quadrant
        // record are the island's memory of its monsters — they send hunters, captains and Barons, and keep novices off —
        // and "everything on this island that kills bots is a monster" stopped being true when the guilds went to war.
        // The war of 17:33 on 24.09.2026 left The Blade's own squares dire on its 29 dead ("(1275, 1395), where 14 have
        // died"), and at the next boot five of its members each took the harrow of a square whose killers had gone home.
        if ((c as Corpse)?.Killer is BotMobile { Deleted: false } killer && killer != this)
        {
            BotQuad.FellToBot();
        }
        else
        {
            // The heaviest single reading the peril map ever takes, and the only unambiguous one. A bot that did
            // not come back is the evidence a captain is actually looking for.
            BotPeril.Fell(Map, Location);

            // <b>Whose death it was, because one of them is worth five of the others as evidence.</b> A Baron is
            // the best-armed thing this population can field and is sent to bad ground on purpose; ground that
            // killed him will kill anybody, and BotQuad.BaronWorth is sized so one such death is enough on its
            // own to make a square dire and raise a great hunt for it immediately.
            BotQuad.Fell(Map, Location, Class is BotBaron ? BotQuad.BaronWorth : BotQuad.DeathWorth);
        }


        Journey.Finish();

        Warmode = false;
    }

    /// <summary>
    /// Notices when this bot has walked out of one quadrant and into another, and tells the map about it.
    ///
    /// <para>
    /// Read off where the bot is standing rather than hooked into movement, because a bot changes quadrant by
    /// several routes that have nothing to do with walking — it is raised into one, it is shoved off a tile,
    /// it dies and is put back on its feet somewhere else. One test in the one place every bot passes through
    /// on every turn covers all of them, and costs two divisions and a comparison.
    /// </para>
    /// </summary>
    private void Cross()
    {
        if (Map == null || Map == Map.Internal || Deleted)
        {
            return;
        }

        // What is living in this square, counted on its own clock rather than on the crossing. A bot that
        // settles in one square to mine for ten minutes crosses no boundary at all, and that is exactly the
        // square whose creatures are worth knowing about.
        BotQuad.Look(this);

        var now = BotQuad.Key(Map, Location);

        if (!_quadSeen)
        {
            _quadSeen = true;
            _quadWas = now;
            _quadAt = Location;
            _quadClean = true;

            // Raised into it, which is the one arrival nobody walked. The square still counts as stood in.
            BotQuad.Crossed(Map, Location, Location);

            return;
        }

        if (now == _quadWas)
        {
            return;
        }

        // Only a crossing that nothing interrupted teaches the map anything good. A bruised one still marks
        // the square as trodden, which is the other half of what Crossed does.
        if (_quadClean)
        {
            BotQuad.Crossed(Map, _quadAt, Location);
        }
        else
        {
            BotQuad.Seen(Map, Location);
        }

        _quadWas = now;
        _quadAt = Location;
        _quadClean = true;
    }

    /// <summary>
    /// The share of its stamina a bot keeps in hand before it will run.
    ///
    /// A fifth, and there is a gap on purpose: at a bare "any stamina at all" a bot alternates between running
    /// and walking every step as the last point comes and goes.
    /// </summary>
    public static double RunAbove { get; set; } = 0.2;

    /// <summary>
    /// Whether this bot should be running <em>now</em>, as opposed to whether the population runs at all.
    ///
    /// <para>
    /// <b>The distinction was missing, and it cost more than anything else found tonight.</b> This shard is
    /// configured with <c>stamina.cannotRunWhenFatigued</c>, so a bot out of breath is refused a running step
    /// — and every step a bot took was a running step, because running was a population-wide setting read once.
    /// So a tired bot did not slow down; it stopped, permanently, and the only trace was a hundred refused
    /// steps and "made no progress". Two gatherers were found that way carrying well under half their limit
    /// with nothing wrong except zero stamina.
    /// </para>
    ///
    /// <para>
    /// Walking is always allowed here (<c>cannotWalkWhenFatigued</c> is off), so falling back to a walk is
    /// always better than not moving — and stamina comes back while walking, so the bot picks the run up again
    /// by itself.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The undertaking has a say as well as the stamina, and the order is deliberate: an errand may ask to be
    /// taken at a walk — see <see cref="BotDeed.Hurries"/> — but nothing may ask a bot with no breath to run.
    /// </remarks>
    public bool Running =>
        Runs && StamMax > 0 && Stam > StamMax * RunAbove && (Resolve.Deed?.Hurries ?? true) && !Hidden;

    /// <summary>How often the title is worked out again. Skills move slowly; this is generous.</summary>
    private const int RankEveryMs = 30000;

    private bool _ranked;

    private long _rankedTick;

    /// <summary>
    /// Keeps the rank under the name honest, the way the engine spells it: "Alden, Grandmaster Miner".
    ///
    /// <para>
    /// <b>Written through <see cref="BotRank"/> rather than into <see cref="Mobile.Title"/>, and the
    /// difference is the whole of this note.</b> The first version put the skill title straight into
    /// <c>Title</c>, which reads as the obvious thing to do and is two bugs at once. <c>Title</c> means
    /// <em>custom</em> title to this engine: <c>AddNameProperties</c> glues it to the name with a bare space,
    /// so a bot in the world read "Perri Apprentice Swordsman" while a player reads "Arold, Grandmaster
    /// Alchemist" — and worse, <c>Titles.ComputeTitle</c> takes a non-empty <c>Title</c> as a signal to
    /// <em>skip</em> the skill-title branch entirely, which is the one branch that writes the comma. Setting
    /// it therefore both mangled the punctuation and switched off the code that would have got it right.
    /// </para>
    ///
    /// <para>
    /// So the rank is no longer stored anywhere the engine will second-guess. A bot is a
    /// <c>PlayerMobile</c>, so leaving <c>Title</c> alone makes it render exactly as a player does — nothing
    /// glued into the world label, and the engine's own comma on the paperdoll — and the rank is kept here,
    /// in the bot's own field, for the dashboard and the log to read.
    /// </para>
    ///
    /// <para>
    /// On a clock rather than every beat: it walks the whole skill list, and a title cannot change faster than
    /// a skill does.
    /// </para>
    /// </summary>
    private void Rank()
    {
        var now = Core.TickCount;

        if (_ranked && now - _rankedTick < RankEveryMs)
        {
            return;
        }

        _ranked = true;
        _rankedTick = now;

        // <b>Out of what it earned, never out of what it was handed.</b> Everything in Common is given to
        // every bot at birth, so a granted skill at a hundred makes the whole population read "Grandmaster
        // Merchant" — which is what happened within the hour of Item Identification being handed out on
        // 08.09.2026. A title is a claim about achievement; a handout is not one, and the same reasoning
        // already keeps BotHarrow.Master from mustering forty-nine grandmasters for the same reason.
        BotRank = Titles.GetSkillTitle(this, Earned());
    }

    /// <summary>
    /// What this bot's highest skill would call it — "Grandmaster Miner" — kept apart from
    /// <see cref="Mobile.Title"/> so the engine goes on formatting the name its own way. See <see cref="Rank"/>.
    /// </summary>
    public string BotRank { get; private set; }

    /// <summary>The highest skill this bot actually worked up to, ignoring anything every bot is given.</summary>
    private Skill Earned()
    {
        Skill best = null;

        for (var i = 0; i < Skills.Length; i++)
        {
            var skill = Skills[i];

            if (skill == null || Granted(skill.SkillName))
            {
                continue;
            }

            if (best == null || skill.BaseFixedPoint > best.BaseFixedPoint)
            {
                best = skill;
            }
        }

        return best;
    }

    /// <summary>Whether this is one of the skills handed to every bot. See <see cref="Common"/>.</summary>
    public static bool Granted(SkillName skill)
    {
        for (var i = 0; i < Common.Length; i++)
        {
            if (Common[i].Skill == skill)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a language model is choosing this one's work. Set by BotMindAI when it takes a body.
    ///
    /// <para>
    /// <b>A flag rather than a title, and that is the whole point of it.</b> The model's name used to be
    /// written into <see cref="Mobile.Title"/> so that a thinking bot could be told from the other thirteen
    /// — which put "of qwen3.5:9b" over its head in the world, where nothing else wears a maker's mark, and
    /// meanwhile stopped the engine printing the skill title that belongs there. Which bot is thinking is a
    /// fact for the dashboard, not for the field, so it lives here and is shown as "(AI)" in the one place
    /// that asks.
    /// </para>
    ///
    /// <para>
    /// Held on this side of the wall because BotAIv2 knows nothing of BotMindAI and must not: the minds are a
    /// separate assembly that depends on this one, never the other way round.
    /// </para>
    /// </summary>
    public bool Minded { get; set; }

    /// <summary>
    /// When this bot last came back from the woods with herbs. See <see cref="BotClass.HerbIntervalMs"/>.
    ///
    /// A flag beside the tick, never "is the tick zero": on some hosts the counter is the machine's uptime
    /// passed straight through, so zero is a legitimate reading and useless as "never".
    /// </summary>
    public bool Herbed { get; set; }

    public long HerbTick { get; set; }

    /// <summary>
    /// When this bot last got a piece for nothing. See <see cref="BotClass.FreeCraftIntervalMs"/>.
    ///
    /// A flag beside the tick for the same reason as the herbs: on some hosts a tick count starts at the
    /// machine's uptime, so nought is a legitimate reading and useless as "never".
    /// </summary>
    public bool Crafted { get; set; }

    public long CraftTick { get; set; }

    /// <summary>
    /// A bot does not put its weapon down to cast.
    ///
    /// <para>
    /// <b>The engine empties both hands on every cast, and for a person that is right.</b>
    /// <c>Spell.Cast</c> calls this, it drops whatever is held into the pack, and a player picks it back up
    /// afterwards because a player can see that it happened. A bot cannot: the only thing that re-armed it
    /// was a check on a five-second clock, so a caster in a fight spent most of its time standing there with
    /// empty hands, and every one of those re-arms cost an item move and a line in the log. It is also the
    /// same fault the first version had.
    /// </para>
    ///
    /// <para>
    /// <b>Overridden rather than undone afterwards, which is the difference between a rule and a tidy-up.</b>
    /// Putting the staff back the instant the spell was away worked and was still a repair: the hands emptied
    /// several times a minute and something had to notice each time. Nothing empties them now. The engine
    /// clears hands as a courtesy to the caster — nothing later checks that they are empty, and casting
    /// proceeds exactly as it did — and it is overridable precisely so that a mobile which cannot manage its
    /// own kit may decline. <c>Changeling</c> declines it too.
    /// </para>
    /// </summary>
    public override void ClearHands()
    {
    }

    /// <summary>Whether the one line below has been said. Once is evidence; every time is noise.</summary>
    private static bool _saidDisarmed;

    /// <summary>How long between saying what a bot put back on. See Rearm.</summary>
    public static int SayEveryMs { get; set; } = 60000;

    private static bool _saidWorn;

    private static long _wornTick;

    /// <summary>
    /// Something has come off this bot. Said once, with the call stack, when it was a weapon in a hand.
    ///
    /// <para>
    /// <b>A trap rather than a rule, and it is here because the question could not be answered by reading.</b>
    /// Casters keep losing the staff they were born with — the pack has it back a few seconds later and the
    /// re-arm puts it on again, over and over, and it happens in town. Nothing in this project takes a held
    /// weapon off except <see cref="Draw"/>, which only a class that closes ever calls and no caster is. So
    /// the hand is being emptied by the engine, and the one thing that reliably says which of the engine's
    /// several ways it was is the stack at the moment it happens.
    /// </para>
    ///
    /// <para>
    /// Once for the life of the shard. A stack trace is expensive and this is a question, not a measurement:
    /// the first answer is the whole answer, and asking again costs the population nothing but frames.
    /// </para>
    /// </summary>
    public override void OnItemRemoved(Item item)
    {
        base.OnItemRemoved(item);

        // A bot being deleted takes its kit with it, and the engine reports every piece of it through here.
        // That is not a disarming, it is a funeral — and the boot-time purge of last session's bots fired
        // this thirty-three times before anything had happened at all.
        if (Deleted || !Alive || World.Loading)
        {
            return;
        }

        if (_saidDisarmed || item is not BaseWeapon || item.Layer is not (Layer.OneHanded or Layer.TwoHanded))
        {
            return;
        }

        _saidDisarmed = true;

        logger.Information(
            "{Name} has had its {Item} taken out of its hands. Whatever did it is here: {Where}",
            Name,
            item.GetType().Name,
            new StackTrace(false)
        );
    }

    /// <summary>Whether the line below has been said. Once is evidence; every time is noise.</summary>
    private static bool _saidWorse;

    /// <summary>
    /// Something has gone on this bot. Said once, with the call stack, when a weapon of its own kind went into a hand while
    /// a better one lay in its pack.
    ///
    /// <para>
    /// <b>The other half of Maeve's bows, and a trap for the same reason <see cref="OnItemRemoved"/> is one.</b>
    /// <see cref="Rewield"/> puts the better weapon in the hand; something put the bound one back every fifteen seconds on
    /// build 58, and every place in this project that equips a weapon either chooses by the same worth or dresses a bot
    /// at birth. Reading did not find it, so the stack at the moment it happens is asked instead — once for the life of
    /// the shard.
    /// </para>
    /// </summary>
    public override void OnItemAdded(Item item)
    {
        base.OnItemAdded(item);

        if (_saidWorse || Deleted || !Alive || World.Loading || item is not BaseWeapon weapon
            || weapon.Layer is not (Layer.OneHanded or Layer.TwoHanded) || Rank(weapon) != 2)
        {
            return;
        }

        var pack = Backpack;

        if (pack == null)
        {
            return;
        }

        var bar = Worth(weapon) * WeaponMargin;
        var carried = pack.Items;

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is not BaseWeapon other || (other is BaseRanged) != (weapon is BaseRanged)
                || Rank(other) != 2 || !Suits(other) || Worth(other) <= bar)
            {
                continue;
            }

            _saidWorse = true;

            logger.Information(
                "{Name} has had a {Worse} put in its hands with a better {Better} in its pack. Whatever did it is here: {Where}",
                Name,
                weapon.GetType().Name,
                other.GetType().Name,
                new StackTrace(false)
            );

            return;
        }
    }

    /// <summary>Back on its feet: the kit comes back, and so does the health to use it.</summary>
    public override void OnAfterResurrect()
    {
        base.OnAfterResurrect();

        Fallen = false;

        BotBinding.Restore(this, Bond);

        // What death kept, and what it handed back, are both in the pack. Put them on before walking off.
        Rearm();

        Hits = HitsMax;
        Stam = StamMax;
        Mana = ManaMax;
    }

    /// <summary>
    /// Gone for good. Every subsystem that counts this bot is told, because a count that is never released
    /// does not look wrong — it looks like a busier shard.
    /// </summary>
    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        BotWill.Forget(this);
        BotSquads.Leave(this);
        BotPopulation.Forget(this);
        BotSight.Forget(this);

        Journey.Finish();

        Bond = null;
        Class = null;
        Squad = null;
    }

    /// <summary>
    /// Somebody wants this tile. Whether to give it up is the squad's arithmetic, not this bot's opinion —
    /// a blade holds its place and a mage does not.
    /// </summary>
    public bool StepAsideFor(Mobile asker)
    {
        if (asker == null || Deleted || !Alive || !BotSquads.ShouldYield(this, asker))
        {
            return false;
        }

        var away = BotSquads.YieldAwayFrom(this, asker);

        Direction = away;

        return Move(away);
    }

    /// <summary>
    /// Stats as the class asks for them, held to the cap this character actually has.
    ///
    /// Over-cap raw stats are not an error the engine refuses; they are a character who can never gain
    /// another point and whose sheet does not add up. So the excess is taken back proportionally rather than
    /// clipped off whichever stat happened to be largest.
    /// </summary>
    private void Build(BotClass klass)
    {
        var str = Math.Max(1, klass.Str);
        var dex = Math.Max(1, klass.Dex);
        var pow = Math.Max(1, klass.Int);

        var cap = StatCap;
        var total = str + dex + pow;

        if (cap > 0 && total > cap)
        {
            var scale = cap / (double)total;

            str = Math.Max(1, (int)(str * scale));
            dex = Math.Max(1, (int)(dex * scale));
            pow = Math.Max(1, (int)(pow * scale));
        }

        RawStr = str;
        RawDex = dex;
        RawInt = pow;
    }

    /// <summary>
    /// The hundred points of character creation, spent in the order the class's own skills matter to it.
    ///
    /// <para>
    /// Fifty, thirty and twenty — and never above what the class is aiming for, because a target of forty
    /// does not want fifty. <b>Deliberately well below the targets:</b> the gap is what the bot is motivated
    /// by, and a bot that begins at its target has nothing left to want. The first version's flat "forty per
    /// cent of every target" gave everybody about twenty-five in everything, which is the number at which no
    /// trade works at all — a smith at twenty-six Mining smelts two piles in a hundred and burns the rest.
    /// They were not novices, they were unemployable.
    /// </para>
    /// </summary>
    private void Learn(BotClass klass)
    {
        var wanted = klass.Skills;

        if (wanted == null || wanted.Count == 0)
        {
            return;
        }

        // Paired with the position each skill was declared at, so that equal targets break towards the one
        // the class names first.
        //
        // <b>A tie here is not a curiosity, it is the shape of a measured defect.</b> The first version put
        // Swords and Tactics both at seventy and let a dictionary's enumeration order decide which got the
        // fifty: half the warriors on the shard spent their best points on Tactics and could not hit
        // anybody. The class files have carried warnings about it ever since — and the sort below was still
        // an unstable one, so the warnings were the only protection. Now the order a class writes its
        // skills in is the tie-break, which is the one thing a class can actually say about it.
        List<(SkillName Skill, double Target, int Declared)> ordered = [];

        for (var i = 0; i < wanted.Count; i++)
        {
            ordered.Add((wanted[i].Skill, wanted[i].Target, i));
        }

        ordered.Sort(
            static (a, b) =>
            {
                var byTarget = b.Target.CompareTo(a.Target);

                return byTarget != 0 ? byTarget : a.Declared.CompareTo(b.Declared);
            }
        );

        for (var i = 0; i < ordered.Count; i++)
        {
            var (skill, target, _) = ordered[i];

            // <b>A seasoned class is given what it declares, and the ladder is skipped rather than raised.</b>
            // Handing the captain a bigger allowance would have been the smaller edit and it would have been
            // the wrong one: the allowance is a shape — most of the points to one skill, a little to a
            // second, a token to a third — and a class that has to hold two weapons at the same standing
            // cannot be described by it at any size. See BotClass.Seasoned for why exactly one class is
            // allowed to say this.
            if (klass.Seasoned)
            {
                // Its share of the target rather than the target itself. See BotClass.Seasoning: born
                // finished leaves a bot nothing to do, and the whole point of a target is that it is ahead.
                Skills[skill].Base = Math.Clamp(target * klass.Seasoning, 0.0, target);

                continue;
            }

            var allowance = i < StartingAllowance.Length ? StartingAllowance[i] : 0.0;

            Skills[skill].Base = Math.Min(target, allowance);
        }
    }

    private static readonly double[] StartingAllowance = [50.0, 30.0, 20.0];

    /// <summary>
    /// What every bot knows how to do, whatever it was born as.
    ///
    /// <para>
    /// <b>Separate from <see cref="Learn"/> on purpose, and it must stay separate.</b> Learn spends the
    /// hundred points of character creation across the class's own skills, in order, and the shape of that
    /// spend — fifty, thirty, twenty — is what makes a smith a smith. A skill everybody has is not part of
    /// that argument: adding it to a class's list would take points away from the trade the class exists to
    /// practise, and adding it to every class's list would be the same defect written thirteen times.
    /// </para>
    ///
    /// <para>
    /// Given outright rather than as a target to grow into, because these are not what a bot is becoming.
    /// <see cref="BotProgress"/> only ever raises a skill it restores, so a value handed over here survives
    /// a restart untouched.
    /// </para>
    /// </summary>
    private void LearnCommon()
    {
        for (var i = 0; i < Common.Length; i++)
        {
            var (skill, value) = Common[i];
            var held = Skills[skill];

            if (held != null && value > held.Base)
            {
                held.Base = Math.Clamp(value, 0.0, 100.0);
            }
        }
    }

    /// <summary>
    /// The skills every bot is born knowing, and what at.
    ///
    /// <para>
    /// <c>ItemID</c> at a hundred by Patrick's order of 08.09.2026: a population that loots chests has to be
    /// able to tell what it has picked up, and this is knowledge rather than a trade — nobody on this shard
    /// is supposed to be *becoming* an appraiser. A hundred also removes the roll: <c>ItemIdentification</c>
    /// asks <c>CheckTargetSkill(ItemID, item, 0, 100)</c>, so anything less would leave a bot holding an
    /// unidentified magic sword it cannot price and cannot sell.
    /// </para>
    /// </summary>
    public static readonly (SkillName Skill, double Value)[] Common =
    [
        (SkillName.ItemID, 100.0)
    ];

    /// <summary>
    /// The skill that swings whatever the roll actually handed this bot, taken from the bond.
    ///
    /// Ten archers in the first version spent their whole lives stabbing skeletons with the daggers they had
    /// been handed first, carrying the bows they had trained for. One fact, asked in one place.
    /// </summary>
    private void LearnWeapon()
    {
        var weapon = Bond?.Weapon;

        if (weapon == null)
        {
            return;
        }

        // The class's own skill list deliberately leaves the weapon skill out — the roll settles it — so
        // there is nothing here to overwrite. Held to the option's own target for the same reason the rest
        // are: a target of forty does not want fifty.
        var chosen = weapon.Value;

        // The same exception, and it has to be made twice because the weapon skill is set from the bond
        // rather than from the class's list. Setting it in one place and not the other is how a captain
        // would have ended up an Expert swordsman carrying a bow it could barely draw — which is the exact
        // shape of the defect this whole method was written to cure.
        // Its seasoning share, exactly as the list above gets it. Left at the bare target, a captain came
        // out of the gate on 02.09.2026 with Archery 100 beside Swordsmanship 78 — grandmaster in the one
        // skill it can never improve again, which is the idling this change was made to end.
        Skills[chosen.Skill].Base = Class is { Seasoned: true } klass
            ? Math.Clamp(chosen.Target * klass.Seasoning, 0.0, chosen.Target)
            : Math.Min(chosen.Target, StartingAllowance[0]);
    }

    /// <summary>
    /// Clothes, bound like everything else the world hands out.
    ///
    /// Bound rather than bought, and not for tidiness: an unbound shirt is lootable, and a population that
    /// dies twice is a population standing about naked. It costs nothing to make the cosmetic layer survive
    /// death, and it saves a whole class of "why does this look broken" question.
    /// </summary>
    /// <summary>
    /// How often a bot looks at what it is carrying against what it is wearing.
    ///
    /// Fifteen seconds. The check walks the pack, which is thirty-odd things, so it is not free — but it is
    /// nothing beside the movement budget, and the alternative is the state this replaces: a piece of armour
    /// bought, paid for, delivered and never worn because the one moment that would have noticed had already
    /// gone by.
    /// </summary>
    public static int DressEveryMs { get; set; } = 15000;

    /// <summary>Dressings that left a blade in hand because the bot was fighting. See <see cref="Unhand"/>.</summary>
    public static long LeftToTheFight { get; private set; }

    private bool _dressed;

    private long _dressedTick;

    /// <summary>Whether this bot's pack has been looked through for anything unidentified yet, and when last. See <see cref="BotTidy"/>.</summary>
    internal bool Appraised;

    internal long AppraisedTick;

    /// <summary>Puts on anything in the pack that belongs on the body, on a clock. See <see cref="Rearm"/>.</summary>
    private void Dress()
    {
        var now = Core.TickCount;

        // Seeded from a real tick rather than compared against a nought this field never held.
        if (!_dressed)
        {
            _dressed = true;
            _dressedTick = now;

            return;
        }

        if (now - _dressedTick < DressEveryMs)
        {
            return;
        }

        _dressedTick = now;

        Rearm();
    }

    private void Clothe()
    {
        Wear(new Shirt(Utility.RandomDyedHue()));
        Wear(new LongPants(Utility.RandomDyedHue()));
        Wear(new Boots());
    }

    /// <summary>
    /// Puts back on whatever is in the pack and belongs in a hand or on a body.
    ///
    /// <para>
    /// <b>Nothing did this, and death made it matter.</b> Bound gear survives being killed, but it survives
    /// into the <em>pack</em> — the engine keeps it with the owner, not on the owner — and going back for your
    /// own corpse fills the pack with the rest. So a bot rose, walked to its body, picked up its sword,
    /// hammer and bandages, and then went hunting with all of it stowed and its fists out. It lost the next
    /// fight, and the one after that, which is precisely what was seen: bots trotting back and forth and
    /// being killed.
    /// </para>
    ///
    /// <para>
    /// Two passes, and the order is the same one birth uses: the engine refuses a two-handed thing while
    /// anything at all is in the other hand, so a bow or a staff goes on before a blade. Getting that
    /// backwards is what once left ten archers stabbing skeletons with knives while carrying the bows they
    /// had trained for.
    /// </para>
    /// </summary>
    public int Rearm()
    {
        var pack = Backpack;

        if (pack == null || Deleted || !Alive)
        {
            return 0;
        }

        // <b>Never mid-cast.</b> Putting anything on while a spell is going up calls
        // Spell.OnCasterEquipping, which disturbs it — the engine says so in one line — so a re-arm on a
        // fixed clock is a mage's spell broken on that same clock. Orin threw ten spells in thirty-six
        // seconds on 26.08.2026 while this fired every five, and a caster interrupted every five seconds is
        // a caster that never lands the slow ones at all. Whatever is in the pack will still be there when
        // the spell is off; the same reasoning, and nearly the same sentence, is already in BotWalk.
        if (Spell != null)
        {
            return 0;
        }

        var worn = 0;
        var refused = 0;

        // <b>Layers already spoken for, so that two things never contend for one place.</b> This is the whole
        // of the fix: the loop below used to try every wearable thing in the pack against the body, and where
        // two of them wanted the same hand the second was simply refused — which is harmless in itself and
        // was not the problem. The problem was that <em>which one got there first depended on the order of
        // the pack</em>, and the pack is reordered constantly. Orin alternated between a SkinningKnife and a
        // WarMace every five seconds for as long as anybody watched, on 26.08.2026, putting one on and being
        // refused the other, then the reverse. From outside it is a bot that buys a weapon and does not keep
        // it on.
        HashSet<Layer> taken = [];

        for (var i = 0; i < Items.Count; i++)
        {
            taken.Add(Items[i].Layer);
        }

        // What was in the hands before this ran. Said in the line below because "put on QuarterStaff" every
        // five seconds is either a bot that keeps losing its staff or a bot that never had it on, and those
        // want opposite fixes — and the line as written could not tell them apart.
        var held = FindItemOnLayer(Layer.TwoHanded) ?? FindItemOnLayer(Layer.OneHanded);

        // <b>What went on, by name, and what would not.</b> The line used to say "put 1 things back on" and
        // nothing else, which is a count where the only useful fact is an identity: a bot that puts one thing
        // on every five seconds for an hour — Joss did it ninety-two times on 26.08.2026 — is either wearing
        // ninety-two different things or losing the same one ninety-two times, and those are opposite
        // problems. A number cannot tell them apart and a name can.
        using var put = ValueStringBuilder.Create();
        using var left = ValueStringBuilder.Create();

        // A snapshot: equipping moves things out of the pack, which mutates the list being read.
        List<Item> carried = [.. pack.Items];

        // <b>A hand busy with something lesser is a two-handed weapon that can never go on.</b> The engine
        // refuses anything two-handed while the other hand holds anything at all, and Upgrade deliberately
        // will not touch a hand — so a bow, a staff or a halberd in the pack simply stayed there, refused on
        // every pass, for as long as whatever was in the hand stayed there. Nothing in this method could
        // ever have got it out.
        Unhand(taken, carried);

        // A better weapon of the bot's own kind, before anything is weighed place by place. See Rewield.
        Rewield(taken, carried);

        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < carried.Count; i++)
            {
                var item = carried[i];

                if (item is not (BaseWeapon or BaseArmor or BaseClothing) || item.Deleted || item.Parent != pack)
                {
                    continue;
                }

                if (item.Layer == Layer.TwoHanded != (pass == 0))
                {
                    continue;
                }

                // <b>Somewhere already covered, which is not the same as somewhere covered well enough.</b>
                // This used to skip outright, so a bot that bought a leather skirt while wearing cloth
                // trousers carried the leather home and never put it on — the slot was busy, and busy was the
                // whole of the question being asked. Joss did exactly that, and it is the reason a population
                // that orders armour, pays for it and collects it can still be walking about in cloth.
                //
                // Asked properly: is what is in the pack better than what is on the body. Better is the
                // engine's own ArmorRating and nothing of ours — see Guards — so leather beats cloth because
                // leather stops blows and cloth does not, rather than because somebody ranked them.
                if (taken.Contains(item.Layer) && !Upgrade(item.Layer, carried, pass))
                {
                    continue;
                }

                // Both hands are busy. A one-handed thing cannot go on over a two-handed one and never could,
                // so trying is not a refusal to report — it is arithmetic, and putting it in the log as
                // "could not wear" would be inventing a problem for the next reader to chase.
                //
                // <b>The hand, and only the hand, and getting that wrong locked half the population out of
                // armour entirely.</b> This read "anything that is not two-handed", which is true of a helm,
                // a gorget, vambraces, gauntlets, a cuirass, greaves and a cloak — so every bot holding a
                // bow, a staff or a halberd skipped every piece of armour in its own pack, for ever, and the
                // only bots ever seen putting anything on were the ones carrying a sword. Fifteen of the
                // thirty-four on this shard fight two-handed, including the Baron in the plate he was issued
                // and every archer the armourer has ever filled an order for. Nothing looked wrong: the
                // goods arrived, the order was paid, the market said so, and the piece sat in the pack.
                //
                // A guard written for one layer and applied to all of them. It cost from the first day the
                // population had any armour to buy.
                //
                // <b>A two-handed weapon, not a shield, and the layer alone cannot tell them apart.</b> A shield is worn on
                // the TwoHanded layer and goes on in the first pass, so this refused every one-handed weapon behind it: Garrow
                // the Warrior, re-arming at 11:50:55 on 24.09.2026 with a war fork and a wooden shield in his pack, put the
                // shield on and fought bare-handed — ten of his flights and mendings in forty minutes were a warrior with no
                // blade — and five bots were reported fighting bare-handed that day. Asked of what is actually in that hand.
                if (item.Layer == Layer.OneHanded && FindItemOnLayer(Layer.TwoHanded) is BaseWeapon)
                {
                    continue;
                }

                // The same arithmetic the other way round: a two-handed weapon cannot go on while the one hand
                // holds anything, and Unhand and Rewield have already decided whether that hand should be
                // emptied for it. Trying anyway was a refusal on every pass for as long as the piece rode in
                // the pack — "Corwin was holding Kryss and put on nothing and could not wear Spear" once a
                // minute from 09:11 to 09:19 on 15.09.2026, the line throttled and the refusal not. A shield is
                // not a weapon and still goes on beside a blade.
                if (item is BaseWeapon && item.Layer == Layer.TwoHanded && taken.Contains(Layer.OneHanded))
                {
                    continue;
                }

                // The best of what wants that place, rather than whichever the pack happened to offer first.
                var choice = Pick(carried, item.Layer, pass);

                if (choice == null)
                {
                    continue;
                }

                if (EquipItem(choice))
                {
                    worn++;
                    taken.Add(choice.Layer);

                    if (put.Length > 0)
                    {
                        put.Append(", ");
                    }

                    put.Append(choice.GetType().Name);

                    continue;
                }

                // Refused, and by far the likeliest reason is that the layer is taken — Mobile.CheckEquip
                // turns any conflict into a bare false. Worth counting: a pack full of armour a bot will
                // never wear because it is already wearing something there is a pack full of merchandise,
                // and nothing anywhere said so.
                refused++;

                if (left.Length > 0)
                {
                    left.Append(", ");
                }

                left.Append(choice.GetType().Name);
            }
        }

        // <b>Said at most once a minute for the whole population.</b> It was one line per re-arm, and once
        // casting turned out to disarm its own caster that came to a hundred lines an hour of a bot picking
        // its staff back up — which is not news, it is the trade working. What the line is for is the case
        // where something keeps coming off and nobody knows what; one a minute still shows that and does not
        // bury the log while it does.
        // Counted before the line is throttled, and that order is the point. See BotArms.Dressing.
        BotArms.Dressing(worn, refused);

        var now = Core.TickCount;

        if ((worn > 0 || refused > 0) && (!_saidWorn || now - _wornTick >= SayEveryMs))
        {
            _saidWorn = true;
            _wornTick = now;

            logger.Information(
                "{Name} was holding {Held} and put on {Put}{Left}",
                Name,
                held?.GetType().Name ?? "nothing",
                worn > 0 ? put.ToString() : "nothing",
                refused > 0 ? $" and could not wear {left.ToString()}" : ""
            );
        }

        return worn;
    }

    /// <summary>
    /// Empties the one hand when the pack holds a two-handed weapon that outranks what is in it.
    ///
    /// <para>
    /// <b>Judged by rank and not by damage, which is the whole of why it is safe.</b> A bot fighting with the
    /// weapon its birth roll settled on is holding rank two and nothing displaces it. A bot holding a tool —
    /// rank minus one — is holding something that is not a weapon at all, and a bot holding somebody else's
    /// loot is at rank one. So this only ever fires for a hand that is occupied by something the bot was
    /// never meant to be fighting with, which is exactly the state it was written for.
    /// </para>
    ///
    /// <para>
    /// The old piece goes into the pack rather than on the ground, and nothing happens at all if the pack
    /// will not take it — the same rule <see cref="Draw"/> and <see cref="Upgrade"/> both keep, and for the
    /// same reason: a bot that dropped its knife in a field has been made permanently poorer by a temporary
    /// problem.
    /// </para>
    /// </summary>
    private void Unhand(HashSet<Layer> taken, List<Item> carried)
    {
        if (!taken.Contains(Layer.OneHanded) || taken.Contains(Layer.TwoHanded))
        {
            return;
        }

        // <b>Not in a fight: there the fight chooses the hand (30.09.2026, build 345).</b> BotArms.Suit draws the blade when
        // something is within reach of a class that closes, and this, on the dressing clock, put the blade away for the bow
        // fifteen seconds later — then Suit drew it again: "Galathil put Dagger away so that both hands were free for Bow"
        // 35 times on build 344, fifteen or sixteen seconds apart for three and a half minutes, Imogen 34, Brokk 28, 198 in
        // all across eleven bots (`night-r22-health.md`), each swap a swing thrown away. With a live combatant the hands are
        // left as the fight set them; the bow goes back on after it.
        if (Combatant is { Deleted: false, Alive: true })
        {
            LeftToTheFight++;

            return;
        }

        var wanted = Pick(carried, Layer.TwoHanded, 0);

        if (wanted == null)
        {
            return;
        }

        var held = FindItemOnLayer(Layer.OneHanded);

        if (held == null || Rank(held) >= Rank(wanted))
        {
            return;
        }

        var pack = Backpack;

        if (pack == null || !pack.TryDropItem(this, held, false))
        {
            return;
        }

        taken.Remove(Layer.OneHanded);

        logger.Information(
            "{Name} put {Held} away so that both hands were free for {Wanted}",
            Name,
            held.GetType().Name,
            wanted.GetType().Name
        );
    }

    /// <summary>
    /// How much better a weapon of the bot's own kind has to be before it is swapped in. See <see cref="Rewield"/>.
    ///
    /// <para>
    /// A margin and not "strictly more", because what a weapon lands moves under the bot's feet: the swing
    /// delay reads the bot's stamina, and a blade loses a little with every point of wear. Two weapons within a
    /// hair of each other would change places on those alone, and every swap costs the swing the engine makes a
    /// newly wielded weapon wait for.
    /// </para>
    /// </summary>
    public static double WeaponMargin { get; set; } = 1.05;

    /// <summary>Weapons put away for a better one of the bot's own kind, across the whole population.</summary>
    public static long Rewielded { get; private set; }

    /// <summary>
    /// How long a weapon <see cref="Rewield"/> has put in a hand is not put there again.
    ///
    /// <para>
    /// Five minutes: longer than any flip seen, and shorter than a death and a walk back to the corpse, after which the
    /// same swap is the right one to make again.
    /// </para>
    /// </summary>
    public static int RewieldRestMs { get; set; } = 300000;

    /// <summary>Swaps not made because <see cref="Rewield"/> had put the same weapon in the same hand within <see cref="RewieldRestMs"/>.</summary>
    public static long Reverted { get; private set; }

    /// <summary>The weapon <see cref="Rewield"/> last put in this bot's hand, and when. Serial.Zero until it has.</summary>
    private Serial _rewielded;

    private long _rewieldedTick;

    /// <summary>
    /// Puts a better weapon of the bot's own kind in its hand, and what it was holding in its pack.
    ///
    /// <para>
    /// <b>Patrick's order of 15.09.2026: a bot wields the better weapon even when it is already holding one.</b>
    /// Nothing did. <see cref="Upgrade"/> never touched a hand and <see cref="Rank"/> put the birth roll's type
    /// above everything else, so a weapon bought or taken off a corpse rode in the pack of a bot swinging the
    /// plainer one it was born with, until an unload sold it.
    /// </para>
    ///
    /// <para>
    /// <b>Within its own kind only</b> — the same skill, the same distance and, for a bow, the same ammunition as
    /// the weapon it was born with; see <see cref="OwnKind"/>. Damage says what a weapon does when it connects and
    /// the skill decides whether it connects, and a swordsman handed a better mace is a swordsman who stops
    /// training. A drawn sidearm, a tool out for a job or anything else a hand was given on purpose is left
    /// alone: this acts only on a hand already holding the bot's own kind.
    /// </para>
    ///
    /// <para>
    /// <b>The old weapon goes into the pack, and a bound one stays there.</b> Bound means it weighs nothing, is
    /// never sold and is kept through death (see <c>BotBinding</c>), so a bot whose better weapon is left on its
    /// corpse in a war rises holding nothing, and <see cref="Rearm"/> puts the bound one back in its hand — the
    /// second half of the same order. Nothing happens at all if the pack will not take the old weapon, and a
    /// two-handed weapon is not put on over a shield or a lamp for a number that does not count them.
    /// </para>
    /// </summary>
    private void Rewield(HashSet<Layer> taken, List<Item> carried)
    {
        var held = FindItemOnLayer(Layer.TwoHanded) as BaseWeapon ?? FindItemOnLayer(Layer.OneHanded) as BaseWeapon;

        if (held == null || Rank(held) != 2)
        {
            return;
        }

        var ranged = held is BaseRanged;
        var was = Worth(held);
        var bar = was * WeaponMargin;
        BaseWeapon better = null;

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is not BaseWeapon weapon || weapon.Deleted || weapon.Parent != Backpack)
            {
                continue;
            }

            if (weapon is BaseRanged != ranged || Rank(weapon) != 2 || !Suits(weapon))
            {
                continue;
            }

            // A weapon being carried to a buyer is the buyer's (build 386; traders put on four katanas bought for orders). See Pick.
            if (BotHandover.Promised(this, weapon.GetType()))
            {
                continue;
            }

            if (weapon.Layer == Layer.TwoHanded && held.Layer == Layer.OneHanded && taken.Contains(Layer.TwoHanded))
            {
                continue;
            }

            var worth = Worth(weapon);

            if (worth <= bar)
            {
                continue;
            }

            better = weapon;
            bar = worth;
        }

        if (better == null)
        {
            return;
        }

        // <b>Not the same weapon again so soon.</b> On build 58 Maeve put her bound bow away for a better one eleven times
        // between 14:20:18 and 14:28:19, every fifteen seconds, the same two numbers on consecutive lines: something else put
        // the bound bow back in her hand in between, silently, and nothing here can see what (see OnItemAdded, which will
        // say). Every swap costs the swing the engine makes a newly wielded weapon wait for. A weapon put in the hand here
        // is not put there again within the rest, and each refusal is counted — a number above nought is that other hand.
        if (better.Serial == _rewielded && Core.TickCount - _rewieldedTick < RewieldRestMs)
        {
            Reverted++;

            return;
        }

        var pack = Backpack;

        if (pack == null || !pack.TryDropItem(this, held, false))
        {
            return;
        }

        if (!EquipItem(better))
        {
            // Put back what was taken off: a bot should not come out of this bare-handed.
            EquipItem(held);

            return;
        }

        taken.Remove(held.Layer);
        taken.Add(better.Layer);
        Rewielded++;
        _rewielded = better.Serial;
        _rewieldedTick = Core.TickCount;

        logger.Information(
            "{Name} put {Old} away for {New}, worth {Worth:F0} to it against {Was:F0} by damage, skill and swing{Kept}",
            Name,
            held.GetType().Name,
            better.GetType().Name,
            bar,
            was,
            BotBinding.IsBound(held, Bond) ? "; the old one is bound and stays in the pack" : ""
        );
    }

    /// <summary>
    /// Whether this is a weapon of the kind the bot was born to fight with: the skill that makes it land, the
    /// distance it is used at and, for a bow, the ammunition it takes, all the same as the birth roll's.
    ///
    /// <para>
    /// <b>Never for a class that draws mana from its staff.</b> A caster's staff is worth what it gives back,
    /// which is a number on the class (<c>StaffManaTrickle</c>) and not on the weapon, so for a mage, a healer or
    /// a sage nothing else is its own kind — a heavier stick would land more and give back nothing.
    /// </para>
    /// </summary>
    private bool OwnKind(Item item)
    {
        if (item is not BaseWeapon weapon || Bond?.Weapon is not { } own || Class is not { StaffManaTrickle: 0 })
        {
            return false;
        }

        if (weapon.Skill != own.Skill)
        {
            return false;
        }

        return weapon is BaseRanged bow ? bow.AmmoType == own.Ammunition : own.Ammunition == null;
    }

    /// <summary>
    /// What a weapon lands a second in this bot's hands, by the engine's own arithmetic for this era.
    ///
    /// <para>
    /// <b>Every term is the engine's, read off the very weapon.</b> <c>BaseWeapon.GetBaseDamage</c>: the middle of
    /// the damage range, and the damage level on top — two a level less one since late T2A.
    /// <c>ScaleDamageOld</c>: tactics, which a weapon's accuracy raises by five a level while it is held and so is
    /// added to the skill's base for every candidate alike; then strength, anatomy, lumberjacking for an axe,
    /// quality and the weapon's own bonus; then wear, which takes up to half. <c>GetDelay</c> for the swing, at
    /// this bot's stamina. <c>CheckHit</c> before AOS: the attack skill plus fifty, against a defender who is the
    /// same whichever weapon is chosen and so drops out.
    /// </para>
    ///
    /// <para>
    /// A ranking and not a forecast: it is only ever compared with itself, for one body at one moment.
    /// </para>
    /// </summary>
    private double Worth(BaseWeapon weapon)
    {
        if (weapon == null)
        {
            return 0.0;
        }

        var delay = weapon.GetDelay(this).TotalSeconds;

        if (delay <= 0.0)
        {
            return 0.0;
        }

        var damage = (weapon.MinDamage + weapon.MaxDamage) / 2.0;
        var level = (int)weapon.DamageLevel;

        if (!Core.AOS && level > 0)
        {
            damage += Core.T2A ? 2 * level - 1 : level;
        }

        var tactics = Skills.Tactics.Base;

        if (weapon.UseSkillMod && weapon.AccuracySkill == SkillName.Tactics)
        {
            tactics += 5 * (int)weapon.AccuracyLevel;
        }

        damage += damage * ((tactics - 50.0) / 100.0);

        var modifiers = Str / 5.0 / 100.0 + Skills.Anatomy.Value / 5.0 / 100.0 + weapon.VirtualDamageBonus / 100.0;

        if (Core.UOR && weapon.Type == WeaponType.Axe)
        {
            modifiers += Skills.Lumberjacking.Value / 5.0 / 100.0;
        }

        if (weapon.Quality != WeaponQuality.Regular)
        {
            modifiers += ((int)weapon.Quality - 1) * 0.2;
        }

        damage += damage * modifiers;

        if (weapon.MaxHitPoints > 0 && weapon.HitPoints < weapon.MaxHitPoints)
        {
            damage *= (50.0 + 50.0 * weapon.HitPoints / weapon.MaxHitPoints) / 100.0;
        }

        var lands = Math.Max(0.1, Skills[weapon.Skill].Value + 50.0);

        return damage * lands / delay;
    }

    /// <summary>
    /// Of everything in the pack that wants one place on the body, the one this bot should actually be
    /// wearing there.
    ///
    /// <para>
    /// <b>The weapon the birth roll settled on outranks everything, and a tool of the trade comes last.</b>
    /// A skinning knife is a <c>BaseWeapon</c> as far as the engine is concerned — every knife is — so a
    /// re-arm that took the first weapon it found would put a butcher's knife in the hand of an archer and
    /// call it armed. The knife is for carving and something else will pick it up when there is something to
    /// carve; what belongs in the hand between times is what the bot fights with.
    /// </para>
    /// </summary>
    private Item Pick(List<Item> carried, Layer layer, int pass)
    {
        Item best = null;
        var bestRank = -1;
        var bestScore = 0.0;

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item is not (BaseWeapon or BaseArmor or BaseClothing) || item.Deleted || item.Parent != Backpack)
            {
                continue;
            }

            if (item.Layer != layer || item.Layer == Layer.TwoHanded != (pass == 0))
            {
                continue;
            }

            // <b>Not what is being carried for somebody else (01.10.2026, build 386).</b> A trader on its way to a buyer with
            // the leather skirt the buyer had ordered put the skirt on here, and the order was struck "nothing left to hand
            // over" at the meeting: 22 of 41 failed hauls on build 386, and 5 failed deliveries by carriers the same way. A
            // promised piece is not a candidate. See BotHandover.Promised.
            if (BotHandover.Promised(this, item.GetType()))
            {
                continue;
            }

            // <b>Whether this body may wear it at all, asked before anything is weighed.</b> Everything else
            // in this method decides which of several things is best; this decides whether a thing is a
            // candidate. Vance the archer looted a pair of bone vambraces off a skeleton on 27.08.2026 and
            // tried them on every fifteen seconds for the rest of the session: they ask forty Strength and
            // an archer is built with thirty-five. Nothing was wrong anywhere — the engine refused, correctly
            // and quietly, and the only trace was a count I had added an hour earlier.
            if (!Suits(item))
            {
                Misfits++;

                continue;
            }

            var rank = Rank(item);

            // A tool of the trade, and not the thing this bot fights with. It is not a candidate at all —
            // see Rank.
            if (rank < 0)
            {
                continue;
            }

            // <b>Of two that rank alike, the better one, and not whichever the pack offered first.</b> Upgrade
            // compares what it is handed here against what is worn, so a first-found piece that stopped less than
            // the one on the body ended the question while a better piece lay in the same pack — and the pack is
            // reordered constantly, so which of them got asked was chance. Guards for what is worn, Worth for what
            // is wielded: the numbers the swaps themselves are decided on.
            var score = Score(item);

            if (rank < bestRank || rank == bestRank && score <= bestScore)
            {
                continue;
            }

            best = item;
            bestRank = rank;
            bestScore = score;
        }

        return best;
    }

    /// <summary>What a candidate is compared on: how much it stops if it is worn, what it lands if it is wielded.</summary>
    private double Score(Item item) => item is BaseWeapon weapon ? Worth(weapon) : Guards(item);

    /// <summary>
    /// Whether this body may wear this thing at all: the right sex for it, and strong, quick and clever
    /// enough to carry it.
    ///
    /// <para>
    /// <b>Read off the object, never from a list of item names.</b> Every number here is the engine's own,
    /// asked of the very item being considered, so a piece that is re-tempered or made of a different metal
    /// answers for itself and nothing here has to be told. The engine's <c>CanEquip</c> would have been the
    /// tidier call and it cannot be used: its base also refuses anything whose layer is occupied, which is
    /// exactly the case <see cref="Upgrade"/> exists to handle — asked there it would refuse every
    /// improvement a bot ever tried to make.
    /// </para>
    ///
    /// <para>
    /// It is a <em>candidacy</em> test and not a preference. A thing that fails it is not a worse choice than
    /// what the bot is wearing; it is not a choice, and treating it as one is how a bot spends its afternoon
    /// being refused.
    /// </para>
    ///
    /// <para>
    /// Public because <c>BotTimber.Tool</c> asks it as well: an axe is a weapon before it is a tool, and a
    /// woodcutting trip offered on one the body cannot lift fails on its first beat.
    /// </para>
    /// </summary>
    public bool Suits(Item item) =>
        item switch
        {
            BaseArmor armour =>
                (Female ? armour.AllowFemaleWearer : armour.AllowMaleWearer)
                && Str >= armour.StrRequirement
                && Dex >= armour.DexRequirement
                && Int >= armour.IntRequirement,
            BaseClothing clothing =>
                (Female ? clothing.AllowFemaleWearer : clothing.AllowMaleWearer)
                && Str >= clothing.StrRequirement,
            BaseWeapon weapon => Str >= weapon.StrRequirement,
            _ => true
        };

    /// <summary>
    /// Things passed over because this body could not wear them, across the whole population.
    ///
    /// A named number, because "the bot has nothing for its arms" and "the bot is carrying vambraces it will
    /// never be strong enough for" are different facts and were the same silence.
    /// </summary>
    public static long Misfits { get; private set; }

    /// <summary>
    /// How much a thing stops, as the engine reckons it. Cloth stops nothing, which is the point.
    ///
    /// Read off the item rather than off a catalogue of kinds: two pieces of the same sort wear differently,
    /// and the one being compared here is a real object with a real remaining life.
    /// </summary>
    private static double Guards(Item item) => item is BaseArmor armour ? armour.ArmorRating : 0.0;

    /// <summary>
    /// Takes off what is covering a place when the pack holds something that covers it better, so the better
    /// thing can go on.
    ///
    /// <para>
    /// <b>Armour and clothing only; a weapon is Rewield's.</b> A piece of armour is simply the best one the bot
    /// happens to own, and there is no reason to prefer the older, so this moves it for anything that stops
    /// strictly more — which also makes it stable, because the piece that came off stops less and will not swap
    /// itself back. Weapons are swapped on a number too since 15.09.2026, but only within the kind the birth roll
    /// settled, and that question lives in one place.
    /// </para>
    ///
    /// <para>
    /// The old piece goes into the pack rather than to the ground, and nothing happens at all if the pack
    /// will not take it: a bot that dropped its cuirass in a field to put on a better one has been made
    /// permanently worse by a temporary problem. The same rule <see cref="Draw"/> keeps about weapons.
    /// </para>
    /// </summary>
    private bool Upgrade(Layer where, List<Item> carried, int pass)
    {
        if (where is Layer.OneHanded or Layer.TwoHanded)
        {
            return false;
        }

        var worn = FindItemOnLayer(where);

        if (worn is not (BaseArmor or BaseClothing))
        {
            return false;
        }

        var choice = Pick(carried, where, pass);

        if (choice == null || Guards(choice) <= Guards(worn))
        {
            return false;
        }

        var pack = Backpack;

        if (pack == null || !pack.TryDropItem(this, worn, false))
        {
            return false;
        }

        logger.Information(
            "{Name} took off {Old} for {New}, which stops {Guard:F0} against {Was:F0}",
            Name,
            worn.GetType().Name,
            choice.GetType().Name,
            Guards(choice),
            Guards(worn)
        );

        return true;
    }

    /// <summary>
    /// What the bot fights with first, then anything else that goes there; a tool of the trade, never.
    ///
    /// <para>
    /// <b>A skinning knife comes out to skin something and at no other time.</b> Every knife is a
    /// <c>BaseWeapon</c> as far as the engine is concerned, so a re-arm that took the first weapon it found
    /// in the pack armed an archer with a butcher's knife and called the job done. Ranking the tool last was
    /// not enough and would have been the wrong rule anyway: a tool is not a poor weapon, it is not a weapon,
    /// and the carving code takes it out of the pack itself when there is a corpse — it never wanted it in a
    /// hand in the first place.
    /// </para>
    ///
    /// <para>
    /// Unless the birth roll handed this bot that very thing to fight with, which is asked first and settles
    /// it: what a bot fights with is a fact about the bot, not about the item's usual job.
    /// </para>
    /// </summary>
    /// <returns>
    /// Two for the bot's own weapon or another of its kind (see <see cref="OwnKind"/>), one for anything else
    /// wearable, minus one for a tool.
    /// </returns>
    private int Rank(Item item)
    {
        var kind = item.GetType();

        if (Bond?.Weapon?.Weapon == kind)
        {
            return 2;
        }

        var tools = BotOutfit.ToolsFor(Class);

        for (var i = 0; i < tools.Count; i++)
        {
            if (tools[i] == kind)
            {
                return -1;
            }
        }

        return OwnKind(item) ? 2 : 1;
    }

    /// <summary>
    /// Swaps between the two weapons a bot is carrying: the one it shoots with and the one it swings.
    ///
    /// <para>
    /// <b>Nothing did this, and the sidearm was decorative because of it.</b> Every ranged class on the
    /// shard is handed a second weapon at birth — <c>BotOutfit</c> puts it straight into the pack — and no
    /// line of code has ever taken it out again. An archer with something chewing on it backs away holding a
    /// bow it cannot fire at that distance, and the dagger it was given for exactly this rides in its pack
    /// until it dies. That was survivable for an archer, whose answer is to be elsewhere. It is not
    /// survivable for a class whose answer is to stand.
    /// </para>
    ///
    /// <para>
    /// The old weapon goes into the pack rather than to the ground, so the swap is reversible and costs
    /// nothing but the moment. If the pack will not take it, nothing happens at all: a bot that has dropped
    /// its bow in a field to draw a sword has been made permanently worse by a temporary problem.
    /// </para>
    /// </summary>
    /// <param name="melee">True to draw the blade, false to bring the bow back up.</param>
    /// <returns>Whether the bot is now holding the sort of weapon asked for.</returns>
    public bool Draw(bool melee)
    {
        var pack = Backpack;

        if (pack == null || Deleted || !Alive)
        {
            return false;
        }

        // Fists answer this too, and they answer it wrongly: they are a BaseMeleeWeapon the bot does not
        // own, so they read as "already holding a blade" and would stop an unarmed bot ever drawing one.
        var held = Weapon as Item;

        if (held != null && held.Parent != this)
        {
            held = null;
        }

        if (held is BaseWeapon inHand && inHand is BaseRanged != melee)
        {
            return true;
        }

        BaseWeapon wanted = null;

        // A snapshot: equipping moves things out of the pack, which mutates the list being read. Same reason
        // as Rearm's.
        List<Item> carried = [.. pack.Items];

        // <b>By rank, and never "the first weapon in the pack".</b> This is the same defect Rearm was fixed
        // for and it was left standing here because the two were written apart: a skinning knife is a
        // BaseWeapon as far as the engine is concerned, and a knife is not ranged, so the first thing this
        // found when a captain closed to arm's length was the butcher's tool. Aldric spent 27.08.2026
        // holding one — and because a two-handed bow cannot go on over anything at all, the knife then
        // locked him out of the weapon he exists to shoot, permanently, three refusals every fifteen
        // seconds. See Rank: a tool is not a poor weapon, it is not a weapon.
        var bestRank = 0;
        var bestWorth = 0.0;

        for (var i = 0; i < carried.Count; i++)
        {
            // Suits as well, now that the better of two is preferred: the heavier weapon usually lands more, and
            // one this body cannot lift would be chosen, refused by the engine and leave the draw failing.
            // <b>The wrong kind is passed over, and until 21.09.2026 this line passed over the right one.</b> It read
            // "weapon is BaseRanged != melee" — the very expression the early return above uses to mean "already
            // holding the right kind" — so asked for a blade this looked only at bows, and asked for the bow only at
            // blades. Nothing threw and nothing was logged: a draw that finds nothing returns false, and its callers
            // count successes. What it cost was read off the engine with Argus's `arms`: Neriah holding a bow with
            // no arrows and a bound dagger in her pack, through a whole 45-second fight of BotArms.Check calls, and
            // "0 found with an empty quiver" on every summary since at least 18.09 while 175 lone hunts in one
            // session ended "100% of it left and not a scratch in 45s", nearly all of them shooters'.
            if (carried[i] is not BaseWeapon weapon || weapon is BaseRanged == melee || !Suits(weapon))
            {
                continue;
            }

            // A bow is only worth bringing up if there is something to put in it. See BotArms.Stocked.
            if (weapon is BaseRanged shooter && (shooter.AmmoType == null || pack.GetAmount(shooter.AmmoType) <= 0))
            {
                continue;
            }

            var rank = Rank(weapon);

            if (rank <= 0)
            {
                continue;
            }

            // Of two that rank alike, the one that lands more — the choice Rewield would make on the next re-arm,
            // so a weapon just drawn is not swapped again the moment the re-arm looks.
            var worth = Worth(weapon);

            if (rank < bestRank || rank == bestRank && worth <= bestWorth)
            {
                continue;
            }

            bestRank = rank;
            bestWorth = worth;
            wanted = weapon;
        }

        if (wanted == null)
        {
            return false;
        }

        if (held != null && !pack.TryDropItem(this, held, false))
        {
            return false;
        }

        // <b>And a shield off the other hand when what is wanted takes both.</b> A shield is worn on the TwoHanded layer
        // (see Rearm), so a bow or a staff was refused for as long as it was on, and the blade just taken off went
        // straight back — the same shape as the axe the woodcutter could not get into its hand. Whatever re-arms next
        // puts the shield back when the two-handed weapon is put away.
        var shield = wanted.Layer == Layer.TwoHanded ? FindItemOnLayer(Layer.TwoHanded) : null;

        if (shield != null && !pack.TryDropItem(this, shield, false))
        {
            if (held != null)
            {
                EquipItem(held);
            }

            return false;
        }

        if (EquipItem(wanted))
        {
            return true;
        }

        // Put back what was taken off. Failing to re-equip leaves the bot bare-handed, which BotArms will
        // notice and mend on its own beat — but it should not be this method that caused it.
        if (shield != null)
        {
            EquipItem(shield);
        }

        if (held != null)
        {
            EquipItem(held);
        }

        return false;
    }

    private void Wear(Item item)
    {
        if (item == null)
        {
            return;
        }

        if (Bond != null)
        {
            BotBinding.Bind(item, Bond);
        }

        if (EquipItem(item))
        {
            return;
        }

        var pack = Backpack;

        if (pack == null)
        {
            item.Delete();

            return;
        }

        pack.DropItem(item);
    }

    public override string ToString() =>
        Class == null ? base.ToString() : $"{Name} the {Class.Name}";

    /// <summary>
    /// What a bot is, over and above what the engine already keeps for any Mobile.
    ///
    /// <para>
    /// <b>Patrick's order of the evening of 18.09.2026: "I am tired of them spinning up again before every test
    /// of a fix."</b> Until this build a bot was thrown away on every world load and raised again from
    /// configuration, so a population that had worked, earned, armed itself and learned where the work is woke up
    /// as strangers standing in a field. The engine was already saving the expensive half all along — where a bot
    /// stands, what is in its pack, what is in its bank, its skills, its stats, its clothes, its guild — and the
    /// purge was throwing that away to avoid the cheap half, which is the four things written here.
    /// </para>
    ///
    /// <para>
    /// So: the class, because it is the one fact that says what this body is for; the bond, because it holds the
    /// weapon roll made once at birth and the serials of the bound gear, and without it the bot would be handed a
    /// second kit; the ledger, because what a bot learned about what pays where is the whole of the "spinning up";
    /// and the work in hand by kind and place, which is written to be read by a person and by a later build.
    /// </para>
    ///
    /// <para>
    /// Nothing here is a reference to a live object. A bot whose class has since been renamed out of the
    /// configuration is deleted on load rather than patched, which is the same rule <see cref="BotProgress"/>
    /// keeps and for the same reason.
    /// </para>
    /// </summary>
    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(1);

        writer.Write(Class?.Name ?? "");

        writer.Write(Bond != null);
        Bond?.Save(writer);

        Resolve.Ledger.Save(writer);

        var deed = Resolve.Deed;

        writer.Write(deed?.Kind ?? "");
        writer.Write(deed?.Map ?? Map.Internal);
        writer.Write(deed?.Where ?? Point3D.Zero);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        if (version < 1)
        {
            // A save written before any of this was kept. There is nothing to reclaim, and the bot is deleted on
            // load exactly as every bot was before this build.
            return;
        }

        var was = reader.ReadString();

        Was = string.IsNullOrWhiteSpace(was) ? null : was;

        if (reader.ReadBool())
        {
            var bond = new BotBond();

            bond.Load(reader);
            Bond = bond;
        }

        Resolve.Ledger.Load(reader);

        var doing = reader.ReadString();

        WasDoing = string.IsNullOrWhiteSpace(doing) ? null : doing;
        WasDoingOn = reader.ReadMap();
        WasDoingAt = reader.ReadPoint3D();
    }

    /// <summary>
    /// Everything a bot back from a save needs, and nothing it already has.
    ///
    /// <para>
    /// The counterpart of <see cref="Become"/>, and the difference between them is the whole point: Become builds a
    /// body, deals out skills, hands over a kit and dresses it. All four of those are already on this bot, restored
    /// by the engine, and doing any of them again is how a reclaimed bot ends up with two of everything. What is
    /// left is the class, the bond and a place in the guild — and the guild only if the save did not bring one.
    /// </para>
    /// </summary>
    /// <param name="away">
    /// A bot that was resting when the shard stopped and is resting still (see <see cref="BotRest"/>): everything is
    /// done but putting it back in the world, which happens when its rest is over.
    /// </param>
    public bool Revive(BotClass klass, bool away = false)
    {
        if (klass == null || Deleted || Backpack == null)
        {
            return false;
        }

        // <b>A bot saved as a ghost is put back in the reviver's hands, where until 21.09.2026 it was deleted.</b>
        // Fallen is not written — it is a flag about a death that is being dealt with, and the machinery dealing
        // with it is gone with the process — so a ghost that came back without it was the motionless bot at the
        // graveyard described in Become, and build 163 chose to delete it and raise a live one. That cost more
        // than it looked: the first restart after guilds could be founded deleted Rowan the Gatherer, who had
        // founded The Hammer six minutes earlier, with his pack, his bank and his ledger, for having been dead
        // at the second of the save. The flag is all the machinery needs: BotPopulation.Revive carries a fallen
        // bot home and raises it after ReviveMs, and BotBinding.Restore hands back whatever of its bond it rose
        // without. The clock starts now, because the tick it fell at belonged to the last process.
        if (!Alive)
        {
            Fallen = true;
            FellTick = Core.TickCount;
            ReviveComplained = false;
            BackAsGhost++;
        }

        // Same line, same reason as in Become: without it the engine deletes this body the first time it dies.
        Player = true;

        // <b>The engine logs every saved player out, and a bot has no client to log back in.</b> Mobile.Deserialize
        // ends with: "if (m_Player && m_Map != Map.Internal) { LogoutLocation = m_Location; LogoutMap = m_Map;
        // m_Map = Map.Internal; }" — a player without a connection is parked on the internal map and put back where
        // it stood only by OnConnected. Nothing ever connects for a bot. Before this line, every one of the eighty
        // bots reclaimed on 18.09.2026 came back on Map.Internal: alive, with its pack, its bank and its ledger, at
        // the right coordinates on the wrong facet — and BotMobile.Beat returns on "Map == Map.Internal" without a
        // word, so they took their turns and did nothing at all for a quarter of an hour. This is what the purge had
        // been hiding: a bot was deleted before anybody could notice where the engine had put it.
        if (!away && (Map == null || Map == Map.Internal) && LogoutMap != null && LogoutMap != Map.Internal)
        {
            MoveToWorld(LogoutLocation, LogoutMap);
        }

        if (away ? LogoutMap == null || LogoutMap == Map.Internal : Map == null || Map == Map.Internal)
        {
            return false;
        }

        Class = klass;

        Bond ??= BotOutfit.Give(this, klass);

        if (Guild == null)
        {
            BotGuilds.Enrol(this);
        }

        return true;
    }
}
