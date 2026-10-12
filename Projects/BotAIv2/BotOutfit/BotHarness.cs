using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What a bot ought to be wearing, worked out from what this shard can actually make and what it costs.
///
/// <para>
/// <b>The catalogue is asked of the engine, never written down here.</b> A table of armour types with their
/// protection and their price would be right on the day it was typed and wrong the first time anybody edits
/// an armour definition or a recipe — and wrong <em>silently</em>, which is the expensive kind. So this
/// walks the craft systems the shard already runs, keeps every recipe whose product is a piece of armour,
/// builds exactly one of each, reads its numbers off the object, and throws it away. Everything below is a
/// fact the engine gave up: protection, the agility it costs, the strength it wants, whether a caster can
/// meditate in it, and how much material one takes.
/// </para>
///
/// <para>
/// <b>Only what can be made, because a want nobody can fill is worse than no want.</b> Coming from the
/// recipes rather than from the item types means a piece is on this list if and only if some bot on this
/// island could in principle forge or sew it — and the skill it needs comes along with it, so "nobody is
/// good enough yet" is a fact the board can be told rather than a mystery on it.
/// </para>
///
/// <para>
/// <b>And the answer is harm stopped per gold — over the piece's whole life, not per blow.</b> Ranked on
/// armour rating alone every bot wants plate, and twenty bots saving for plate is twenty bots not buying
/// arrows. Ranked on rating per gold, which is what this did first, everybody buys leather — and that is
/// wrong for precisely the bots that need armour most, because a piece is not bought by the point, it is
/// bought by the afternoon. Plate carries more rating <em>and</em> more durability, so it stops nearly three
/// times the harm a leather tunic does before either is scrap. Both numbers are the engine's own.
/// </para>
/// </summary>
public static class BotHarness
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHarness));

    /// <summary>
    /// What one point of a bot's agility is worth against one point of protection.
    ///
    /// <para>
    /// Heavy armour costs dexterity on this era — the engine says so through <c>OldDexBonus</c>, and a plate
    /// hauberk is eight points where a ring one is two. For somebody who swings a blade that is a fair
    /// trade; for an archer it is the difference between shooting and not, because everything a bow does is
    /// paid for in dexterity. Weighed rather than forbidden: a class that hates the trade gets a heavy
    /// multiplier, not a veto, so a very cheap piece of plate would still be considered on its merits.
    /// </para>
    /// </summary>
    public static double AgilityPerArmour { get; set; } = 1.0;

    /// <summary>How much more an agile class minds losing dexterity than a heavy one does.</summary>
    public static double NimbleCare { get; set; } = 3.0;

    /// <summary>
    /// What one point of armour rating takes off a blow, on average.
    ///
    /// Three quarters, and it is not a choice: <c>BaseArmor.OnHit</c> absorbs <c>AR/2 + AR/2 × random</c>,
    /// whose mean is exactly this. Written down here so that the arithmetic below can be read without
    /// opening the engine, and named so that it is obvious where to look when the engine changes.
    /// </summary>
    public const double AbsorbedPerPoint = 0.75;

    /// <summary>
    /// How many blows a piece survives per point of its durability.
    ///
    /// A quarter of blows wear the piece, and a wearing blow costs it about half a point on average for a
    /// blade or a spear — so eight blows to the point. Bashing weapons are far crueller, wearing by half of
    /// what was absorbed, which means heavy armour wears fastest against maces; that is real and it is not
    /// modelled here, because what a bot will be hit with is not knowable when it is buying.
    /// </summary>
    public const double BlowsPerPoint = 8.0;

    /// <summary>What a piece is assumed to cost when the market has never seen one.</summary>
    public static int GoldPerHide { get; set; } = 4;

    /// <summary>Least a piece may be reckoned to cost, so nothing divides by nothing.</summary>
    public static int LeastCost { get; set; } = 10;

    /// <summary>One kind of armour, as the engine describes it.</summary>
    public sealed class Piece
    {
        public Type Kind { get; init; }

        public Layer Where { get; init; }

        /// <summary>Protection, as the engine computes it for a plain iron or leather example.</summary>
        public double Rating { get; init; }

        /// <summary>Agility it costs. Negative in the engine's own sign; kept as a positive cost here.</summary>
        public int Agility { get; init; }

        public int Strength { get; init; }

        /// <summary>How much punishment the piece takes before it is scrap, as the engine rolls it.</summary>
        public int Lasts { get; init; }

        /// <summary>
        /// Damage this piece will absorb over the whole of its life, at the engine's own arithmetic.
        ///
        /// <para>
        /// <b>This is the number that answers "leather or plate", and it is entirely the engine's.</b>
        /// <c>BaseArmor.OnHit</c> absorbs <c>AR/2 + AR/2 × random</c> from every blow that lands on the
        /// piece — three quarters of its rating on average, linear, with no diminishing return. And every
        /// blow has a quarter chance of costing it a point or two of durability, so a piece survives roughly
        /// eight blows per point it has. Multiply the two and a piece has a <em>total</em> quantity of harm
        /// it will ever stop, which is the only honest thing to divide a price by.
        /// </para>
        /// </summary>
        public double Absorbs => Rating * AbsorbedPerPoint * Lasts * BlowsPerPoint;

        public ArmorMeditationAllowance Meditation { get; init; }

        /// <summary>
        /// Whether a man may wear it, and whether a woman may.
        ///
        /// <para>
        /// <b>Read off the sample with everything else, because the engine has always known and nothing here
        /// ever asked.</b> This catalogue is built by walking the craft recipes — a piece is on it if some
        /// bot could in principle make one — and "could somebody make it" is not "could this bot wear it".
        /// Two different questions, and a leather bustier answers yes to the first for everybody and no to
        /// the second for half the population.
        /// </para>
        ///
        /// <para>
        /// On 27.08.2026 Godric, who is a man, ordered three pairs of <c>LeatherBustierArms</c>, paid
        /// forty-eight gold for each, had all three delivered, and was refused by the engine every time —
        /// then found his arms still bare and ordered a fourth. A want nobody could fill is a want that
        /// eventually gives up; a want that is filled perfectly and cannot be used is a bot buying the same
        /// thing for ever.
        /// </para>
        /// </summary>
        public bool Male { get; init; }

        public bool Female { get; init; }

        /// <summary>Whether this piece will go on that body at all. The one question the catalogue owed.</summary>
        public bool Fits(Mobile body) => body == null || (body.Female ? Female : Male);

        /// <summary>Which trade makes it, and how good that trade has to be.</summary>
        public SkillName Craft { get; init; }

        public double Needs { get; init; }

        /// <summary>What the material for one comes to, at the shard's own prices.</summary>
        public int Cost { get; init; }

        /// <summary>The material it is made of, how many units of it one takes, and the stand-in price of a unit (03.10.2026, build next). See <see cref="Making"/>.</summary>
        public Type Material { get; init; }

        public int Units { get; init; }

        public int PerUnit { get; init; }

        /// <summary>
        /// What one costs to make now, twice over — the material at what it goes for on the island (<see cref="BotMarket.Worth"/>,
        /// never under the stand-in <see cref="PerUnit"/>), and as much again for the crafter's work. The armourer's opening
        /// (03.10.2026, build next).
        ///
        /// <para>
        /// <b>The stand-in alone is no longer enough, because nothing lifts it now.</b> <see cref="Cost"/> prices leather at
        /// <see cref="GoldPerHide"/> (4) while the tanner sells it at 6 and bots handed it over at 7.8 on build 396: twice that is a
        /// leather chest at 96gp for 94gp of leather. Every armour want opened at ×4 of it then, read off another buyer's raised
        /// offer (<see cref="BotMarket.Worth"/> no longer does), so the hole never showed.
        /// </para>
        /// </summary>
        public int Making()
        {
            if (Material == null || Units <= 0)
            {
                return Cost * 2;
            }

            var stand = Math.Max(1, PerUnit);
            var unit = Math.Max(stand, BotMarket.Worth(Material, Math.Max(stand, BotShops.Lowest(Material))));

            return (int)Math.Min(int.MaxValue, Math.Max((long)Cost, (long)Units * unit) * 2);
        }

        public override string ToString() =>
            $"{Kind.Name} ({Rating:F0} armour × {Lasts} wear = {Absorbs:F0} damage stopped for {Cost}gp, {Craft} {Needs:F0})";
    }

    private static readonly List<Piece> _pieces = [];

    public static IReadOnlyList<Piece> Pieces => _pieces;

    /// <summary>The layers this shard will ever try to cover, in the order a bot cares about them.</summary>
    public static IReadOnlyList<Layer> Layers { get; } =
    [
        Layer.InnerTorso,
        Layer.Pants,
        Layer.Arms,
        Layer.Gloves,
        Layer.Helm,
        Layer.Neck
    ];

    /// <summary>Whether this is a place a bot bothers to armour at all.</summary>
    private static bool Covers(Layer where)
    {
        for (var i = 0; i < Layers.Count; i++)
        {
            if (Layers[i] == where)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>How long before an empty survey is worth trying again.</summary>
    public static int RetryMs { get; set; } = 30000;

    private static long _surveyedTick;

    private static bool _surveyed;

    /// <summary>
    /// Reads the craft systems, if they exist yet, and works out what armour this shard can make.
    ///
    /// <para>
    /// <b>Asked when it is first needed rather than at startup, and the first version got that wrong.</b>
    /// <c>DefBlacksmithy.CraftSystem</c> is built by a static <c>Initialize</c> that the engine runs during
    /// content initialisation; a bot module starts on world load, and the order between the two is not ours
    /// to decide. Surveyed at startup it read <em>nought pieces</em> and said so in the boot line — an
    /// answer that is indistinguishable from "this shard cannot make armour", which is what it looked like
    /// for a whole restart. Lazily, with a retry, it simply comes right on its own the first time anybody
    /// asks after the recipes exist. This is the same idiom the shops and the ore already use.
    /// </para>
    /// </summary>
    public static void Survey()
    {
        _pieces.Clear();

        Read(DefBlacksmithy.CraftSystem, SkillName.Blacksmith, typeof(IronIngot), BotDig.GoldPerIngot);
        Read(DefTailoring.CraftSystem, SkillName.Tailoring, typeof(Leather), GoldPerHide);

        _pieces.Sort(static (a, b) => a.Cost.CompareTo(b.Cost));

        _surveyedTick = Core.TickCount;
        _surveyed = _pieces.Count > 0;

        if (!_surveyed)
        {
            // Said once as a warning rather than as a fact, because at this point it is far more likely that
            // the craft systems are not built yet than that this shard genuinely has no armour in it.
            logger.Warning("No armour could be surveyed yet: the craft systems may not be built. Trying again in {Wait}ms", RetryMs);

            return;
        }

        logger.Information(
            "Armour surveyed: {Count} pieces this shard could make, from {Cheap} up to {Dear}",
            _pieces.Count,
            _pieces.Count == 0 ? "nothing" : _pieces[0].ToString(),
            _pieces.Count == 0 ? "nothing" : _pieces[^1].ToString()
        );
    }

    private static void Read(CraftSystem system, SkillName craft, Type material, int perUnit)
    {
        if (system == null)
        {
            return;
        }

        var recipes = system.CraftItems;

        for (var i = 0; i < recipes.Count; i++)
        {
            var recipe = recipes[i];

            // One material and nothing else, which is the same test the crafters themselves apply before
            // they will take a recipe on. A thing needing three ingredients is a thing this shard's supply
            // chain cannot yet promise.
            if (!BotCraftwork.Simple(recipe, material) || recipe.ItemType == null)
            {
                continue;
            }

            var made = Sample(recipe.ItemType);

            if (made == null)
            {
                continue;
            }

            try
            {
                if (made is not BaseArmor armour || !Covers(armour.Layer))
                {
                    continue;
                }

                _pieces.Add(
                    new Piece
                    {
                        Kind = recipe.ItemType,
                        Where = armour.Layer,
                        Rating = armour.ArmorRating,

                        // The engine states the penalty as a negative bonus. Kept positive here so that
                        // everything in the arithmetic below is a cost, and nothing has to be read twice.
                        Agility = Math.Max(0, -armour.DexBonus),
                        Strength = armour.StrRequirement,

                        // Averaged rather than taken from the sample: the engine rolls a piece's durability
                        // between two bounds at birth, so one example is one throw of a die and would rank a
                        // whole family by whichever way that die fell.
                        Lasts = Math.Max(1, (armour.InitMinHits + armour.InitMaxHits) / 2),
                        Meditation = armour.MeditationAllowance,
                        Male = armour.AllowMaleWearer,
                        Female = armour.AllowFemaleWearer,
                        Craft = craft,
                        Needs = BotCraftwork.Requirement(recipe, craft),
                        Cost = Math.Max(LeastCost, BotCraftwork.Cost(recipe) * perUnit),
                        Material = material,
                        Units = BotCraftwork.Cost(recipe),
                        PerUnit = perUnit
                    }
                );
            }
            finally
            {
                // <b>Built only to be asked, and it must not survive the question.</b> Constructing one puts
                // a real item into the world; leaving it there would be a piece of armour lying in limbo per
                // recipe, for ever, and a world save full of them.
                made.Delete();
            }
        }
    }

    /// <summary>
    /// One of a kind, built to be measured. Null for anything that will not be built at all.
    ///
    /// <para>
    /// <b>A constructor with an optional argument is not a parameterless constructor.</b>
    /// <c>Activator.CreateInstance(Type)</c> looks for a genuinely empty signature, and every boot, shoe and
    /// sandal in this fork is declared <c>Boots(int hue = 0)</c> — which C# lets a caller write as
    /// <c>new Boots()</c> and reflection does not. So the survey threw four times at every single boot and
    /// said so in four warnings that named the wrong cause.
    /// </para>
    ///
    /// <para>
    /// <b>And the four it was throwing on would have been excluded anyway, which is worth writing down
    /// rather than quietly deleting.</b> Footwear on this fork is <c>BaseShoes : BaseClothing</c>, so it has
    /// no armour rating and the survey drops it a line later; the six slots the harness dresses are
    /// InnerTorso, Pants, Arms, Gloves, Helm and Neck, and Shoes is not among them. So this fixes the survey
    /// and not the wardrobe: what it buys is that the next piece declared with a default argument is
    /// measured instead of skipped, and that the boot log stops naming a cause that was never the cause.
    /// </para>
    ///
    /// <para>
    /// The defaults are taken from the signature rather than guessed, so a piece is measured as the engine
    /// would build it when nobody says otherwise.
    /// </para>
    /// </summary>
    private static Item Sample(Type kind)
    {
        try
        {
            return Activator.CreateInstance(kind) as Item;
        }
        catch (MissingMethodException)
        {
            var made = Defaulted(kind);

            if (made != null)
            {
                return made;
            }

            logger.Warning(
                "{Kind} has no constructor that can be called without arguments, so it is left out of the armoury",
                kind.Name
            );

            return null;
        }
        catch (Exception e)
        {
            logger.Warning("{Kind} could not be built to be measured, so it is left out of the armoury: {Why}", kind.Name, e.Message);

            return null;
        }
    }

    /// <summary>The shortest public constructor whose every argument has a default, built with those defaults.</summary>
    private static Item Defaulted(Type kind)
    {
        var ctors = kind.GetConstructors();

        for (var i = 0; i < ctors.Length; i++)
        {
            var wants = ctors[i].GetParameters();

            if (wants.Length == 0)
            {
                continue;
            }

            var args = new object[wants.Length];
            var all = true;

            for (var j = 0; j < wants.Length; j++)
            {
                if (!wants[j].HasDefaultValue)
                {
                    all = false;

                    break;
                }

                args[j] = wants[j].DefaultValue;
            }

            if (!all)
            {
                continue;
            }

            try
            {
                if (ctors[i].Invoke(args) is Item made)
                {
                    return made;
                }
            }
            catch (Exception e)
            {
                logger.Warning("{Kind} threw while being built for the armoury: {Why}", kind.Name, e.Message);

                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// The best piece for this bot on this layer, or null when nothing is worth wearing there.
    /// </summary>
    /// <param name="skill">
    /// The best any crafter on the shard is at the trade that makes it. A piece nobody can make is not the
    /// best piece, it is a want that will sit on the board holding escrow until somebody gives up on it.
    /// </param>
    /// <summary>
    /// What this bot may spend on one piece, from how rough a life it has actually been having.
    ///
    /// <para>
    /// <b>The second half of the answer, and without it "plate" is the answer for everybody.</b> Ranked on
    /// harm stopped per gold, plate wins outright — and it should, for anybody who is going to be hit. A
    /// gatherer is not. Buying it plate is not a small waste: it is a bot's whole savings spent against a
    /// danger it does not face, and this shard has already learned once what happens when bots are poor.
    /// </para>
    ///
    /// <para>
    /// So the class is never asked. What is asked is how many blows a minute have actually been landing on
    /// this bot lately — a fact it has been collecting all day without knowing it — and the purse follows
    /// from that. A warrior in a graveyard finds it can afford plate. The same warrior after a fortnight of
    /// standing in a market cannot, and neither can the miner who has never been touched.
    /// </para>
    /// </summary>
    public static int Purse(BotMobile bot)
    {
        var resolve = bot?.Resolve;

        if (resolve == null)
        {
            return 0;
        }

        // <b>A floor, because a factor with no floor is a veto wearing a multiplier's clothes.</b> Read
        // straight, this said "a bot nothing has touched lately may spend nothing" — and nothing is not a
        // small budget, it is a refusal: the piece is never offered, the auction never sees it, and the bot
        // cannot buy so much as a leather cap however much gold it has. 1843 refusals of 3172 on the
        // afternoon of 27.08.2026, well over half, and the population read as one where only the captain
        // ever ordered armour — which is exactly what Patrick saw from the client and asked about.
        //
        // The floor is the price of the cheapest thing on the catalogue, so the meaning of the rule is kept
        // whole: a quiet life still buys leather and nothing better, and it takes real blows to reach mail
        // or plate. What changes is only that a quiet life is no longer worth nothing at all. The miner who
        // has never been touched is precisely the bot that gets killed by the first thing that touches it.
        var earned = (int)(resolve.Beaten(Core.TickCount) * GoldPerBlow);

        return Math.Max(LeastPurse, earned);
    }

    /// <summary>
    /// The least any bot may spend on one piece, however quiet its life has been.
    ///
    /// <para>
    /// Read off the catalogue rather than chosen, and read off the <em>dearest</em> thing a tailor makes
    /// rather than the cheapest thing anybody makes. The first version of this floor took the cheapest piece
    /// on the whole catalogue — a leather cap at ten gold — which bought a bot a cap and nothing else, since
    /// every other place on the body costs more than that. The rule this floor exists to express is "a quiet
    /// life is worth leather", and leather means a suit of it, so the floor is what the priciest leather
    /// costs. Metal stays out of reach: it is the smith's trade, not the tailor's, and it is not in this sum.
    /// </para>
    ///
    /// <para>
    /// Doubled the way an offer is — see the note in <c>BotArmourer</c> on why a commission is twice the
    /// material — so this is a price a crafter would actually take, not a materials cost nobody would work
    /// for. Recomputed with the catalogue rather than cached: the survey can run again, and a floor that
    /// remembered an empty catalogue would be a floor of nothing for the life of the shard.
    /// </para>
    /// </summary>
    public static int LeastPurse
    {
        get
        {
            var dearest = 0;

            for (var i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];

                if (piece.Craft == SkillName.Tailoring && piece.Cost > dearest)
                {
                    dearest = piece.Cost;
                }
            }

            // A hundred and twenty until the craft systems are built, which is about what a leather tunic
            // comes to on this shard. See Survey: a start-up answer cannot be trusted and is asked again.
            return dearest > 0 ? dearest * 2 : 120;
        }
    }

    /// <summary>
    /// What a bot will spend on one piece for each blow a minute it has been taking.
    ///
    /// The one dial on the whole arrangement, and it is a statement about how much a quiet life is worth. At
    /// two hundred, a bot taking a blow a minute will buy leather, one taking two or three can afford ring or
    /// chain, and only a bot really living in a fight ever gets plate.
    /// </summary>
    public static int GoldPerBlow { get; set; } = 200;

    /// <summary>
    /// Pieces passed over because the bot's body could not wear them.
    ///
    /// A named number rather than a silent skip: "this bot wants nothing on its arms" and "everything that
    /// goes on arms is cut for the other sex" are different facts about the shard, and they were the same
    /// silence.
    /// </summary>
    public static long Misfits { get; private set; }

    public static Piece Best(BotMobile bot, Layer where, Func<SkillName, double> skill) =>
        Best(bot, where, skill, int.MaxValue);

    public static Piece Best(BotMobile bot, Layer where, Func<SkillName, double> skill, int purse)
    {
        // Nothing yet, and it is worth asking again: see Survey for why a startup answer cannot be trusted.
        if (!_surveyed && Core.TickCount - _surveyedTick >= RetryMs)
        {
            Survey();
        }

        Piece best = null;
        var bestWorth = 0.0;

        for (var i = 0; i < _pieces.Count; i++)
        {
            var piece = _pieces[i];

            if (piece.Where != where)
            {
                continue;
            }

            // <b>Before price, before skill, before protection: will it go on.</b> Everything below this
            // line weighs how good a piece would be, and a piece the engine will refuse is not a poor
            // choice — it is not a choice. See Piece.Fits for what three leather bustiers cost.
            if (!piece.Fits(bot))
            {
                Misfits++;

                continue;
            }

            if (skill != null && piece.Needs > skill(piece.Craft))
            {
                continue;
            }

            if (piece.Cost > purse)
            {
                continue;
            }

            var worth = Worth(bot, piece);

            if (worth > bestWorth)
            {
                bestWorth = worth;
                best = piece;
            }
        }

        return best;
    }

    /// <summary>
    /// What one piece is worth to one bot: protection per thousand gold, bent by what it costs this bot to
    /// wear.
    ///
    /// <para>
    /// Two vetoes and one weighing. A caster that cannot meditate in it may not have it at any price —
    /// everything a mage does is rationed by mana, and armour that stops the mana coming back is armour that
    /// stops the mage. A piece the bot is not strong enough for is refused for the same reason the engine
    /// would punish it. Everything else is a trade between protection and agility, and how hard that trade
    /// bites depends on what the bot does for a living.
    /// </para>
    /// </summary>
    public static double Worth(BotMobile bot, Piece piece)
    {
        var klass = bot?.Class;

        if (klass == null || piece == null || piece.Rating <= 0.0)
        {
            return 0.0;
        }

        // The flag was written for this and had nothing to read until today. Asked of the engine's own
        // answer for the piece rather than of a list of plate, chain and ring kept here.
        if (klass.NeedsMeditation && piece.Meditation != ArmorMeditationAllowance.All)
        {
            return 0.0;
        }

        if (piece.Strength > bot.RawStr)
        {
            return 0.0;
        }

        // What losing that much dexterity costs this bot, as a share of what it has. An archer minds three
        // times as much as a swordsman, because everything a bow does is paid for in dexterity.
        var cares = klass.Role == BotRole.Ranged ? NimbleCare : 1.0;
        var agility = Math.Clamp(
            1.0 - piece.Agility * AgilityPerArmour * cares / Math.Max(1, bot.RawDex),
            0.0,
            1.0
        );

        // <b>Harm stopped over the piece's whole life, per gold — not protection per gold.</b> The first
        // version divided the rating by the price and answered "leather" for everybody, which is wrong in a
        // way that matters most to exactly the bots who need armour most. A leather tunic is thirteen points
        // of armour on thirty-five of durability; a plate one is forty on fifty-seven. Per point of rating
        // leather looks the better bargain, and it is — but a warrior does not buy points of rating, it buys
        // an afternoon of not being hurt, and the plate stops nearly three times as much harm before it
        // falls apart. Both facts are the engine's; the old formula simply threw one of them away.
        return piece.Absorbs * agility / piece.Cost;
    }

    /// <summary>
    /// The best anybody alive on this island is at a trade.
    ///
    /// <para>
    /// <b>The population is asked, not the bot doing the wanting.</b> A warrior ordering a hauberk is not
    /// going to forge it, so its own blacksmithy is beside the point entirely; what decides whether the want
    /// can ever be filled is the best smith on the shard. Asked live rather than remembered, because the
    /// whole design of this population is that its crafters get better — a piece that is out of reach this
    /// afternoon should come within reach on its own, with nobody clearing a cache.
    /// </para>
    /// </summary>
    public static double Ablest(SkillName craft)
    {
        var bots = BotPopulation.Bots;
        var best = 0.0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Alive: true })
            {
                continue;
            }

            // <b>Only a craftsman holding its tool, and at the margin it works to.</b> Faron's 78 in Tailoring kept the
            // armourer ordering leather armour all night on 26.09.2026 while he had no sewing kit and nobody else could
            // sew it, and the orders escalated to 1505gp with thirty thousand gold frozen under them. A crafter takes an
            // order only at the recipe's skill plus BotCraftwork.Margin.
            if (craft == SkillName.Tailoring && BotThread.Kit(bot) == null)
            {
                continue;
            }

            var able = bot.Skills[craft].Base - BotCraftwork.Margin;

            if (able > best)
            {
                best = able;
            }
        }

        return best;
    }

    public static string Describe() =>
        _pieces.Count == 0
            ? "no armour has been surveyed yet"
            : $"{_pieces.Count} kinds of armour this shard can make, {Covering()} of the {Layers.Count} places a bot can cover, {Misfits} passed over as cut for the other sex";

    private static int Covering()
    {
        var covered = 0;

        for (var i = 0; i < Layers.Count; i++)
        {
            for (var j = 0; j < _pieces.Count; j++)
            {
                if (_pieces[j].Where == Layers[i])
                {
                    covered++;

                    break;
                }
            }
        }

        return covered;
    }
}
