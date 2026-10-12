using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Targeting;

namespace Server.BotAI.V2;

/// <summary>
/// Cutting wood: walk to the nearest tree and swing until the pack has enough or the tree has nothing left.
///
/// <para>
/// <b>Deliberately the smallest harvest on the shard.</b> The miner has veins worth different money, a lode
/// to walk to, a forge to smelt at and a bank to leave metal in; a woodcutter has trees, and every tree is
/// the same tree. So there is no survey, no ledger of good ground and no second leg — the errand is "cut
/// until the axe stops paying", and the walk that brought the bot into the woods is the walk the auction
/// already priced.
/// </para>
/// </summary>
public sealed class BotChop : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotChop));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "chop";

    /// <summary>What a chopping trip is reckoned at before the ledger knows better.</summary>
    public static double Prior { get; set; } = 60.0;

    /// <summary>How long one is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>How often the axe comes round. The engine holds its own delay; this only stops us asking faster.</summary>
    public static int SwingMs { get; set; } = 2000;

    /// <summary>
    /// How long the axe may go without producing a log before the tree is given up on.
    ///
    /// <para>
    /// Half a minute, which is a dozen swings. <b>A backstop now rather than the measure, and the note that
    /// stood here said exactly why it had to be the measure:</b> "the engine says which only by silence — the
    /// message it would send goes to a client this bot has not got". That is no longer true. The engine says
    /// "there's not enough wood here to harvest" in as many words, and since 09.09.2026 there is a seam for a
    /// bot to hear it through — see <c>BotHeard</c> and <c>engine-patches/HarvestDefinition-said.patch</c>.
    /// </para>
    ///
    /// <para>
    /// What silence cost, measured the evening the ear was opened: the harvest system said <b>there is
    /// nothing left here 1,105 times in twenty minutes</b>, the great majority of them to woodcutters, while
    /// this clock made every one of them wait out half a minute of swinging at a stump first.
    /// </para>
    /// </summary>
    public static int StallMs { get; set; } = 30000;

    /// <summary>Trees given up on because the engine said they were cut out. See <c>BotHeard</c>.</summary>
    public static long Spoken { get; private set; }

    /// <summary>And trees given up on by the clock alone, which is now the unusual case.</summary>
    public static long Silent { get; private set; }

    /// <summary>Trees given up on because the engine called every swing out of range from where the bot stood.</summary>
    public static long Unreached { get; private set; }

    /// <summary>How many out-of-range swings in a row at one tree before it is given up on.</summary>
    public static int AdriftMost { get; set; } = 4;

    /// <summary>
    /// How many swings in a row at one tree may bring no wood before it is given up, whatever the clock says.
    ///
    /// <para>
    /// <b>The clock could be kept young for ever, and it was.</b> <see cref="StallMs"/> counts from the last log,
    /// but an out-of-range swing sets it back on purpose, and one of those every half minute kept a tree from
    /// being given up while the swings between them brought nothing: Sable, 293 swings and no log in 10.7
    /// minutes, and Ulla, 199 in 7.6, both on 14.09.2026, both ended only by the stall watch — after which Ulla
    /// took a fresh trip at the same spot and cut twenty logs in seven swings. Swings are what the tree is being
    /// asked, so swings are what this counts, and nothing resets it but wood or a new tree.
    /// </para>
    /// </summary>
    public static int QuietSwings { get; set; } = 20;

    /// <summary>
    /// How many swings a whole trip may take with nothing at all to show before it is given up as a failure. The
    /// backstop under <see cref="QuietSwings"/>: a trip that keeps finding trees that give nothing ends itself and
    /// says so, instead of waiting for a watch to notice.
    /// </summary>
    public static int GiveUpSwings { get; set; } = 80;

    /// <summary>Trips given up after <see cref="GiveUpSwings"/> swings without a single log.</summary>
    public static long Fruitless { get; private set; }

    /// <summary>The swing count when wood last arrived or a tree was last given up. See <see cref="QuietSwings"/>.</summary>
    private int _grewSwings;

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly int _want;

    private IPoint3D _tree;

    /// <summary>Trees given up on this trip, by tile, so the nearest-tree search does not hand them back.</summary>
    private readonly HashSet<(int X, int Y)> _shunned = [];

    /// <summary>Swings in a row the engine has called out of range at the tree in hand.</summary>
    private int _adrift;

    private int _cut;

    private int _swings;

    private long _swungTick;

    private long _grewTick;

    /// <summary>Trees one trip swaps for the next when the walk to one is refused, before the trip is given up.</summary>
    public static int MostBends { get; set; } = 3;

    /// <summary>Trees swapped for the next when the walk to one was refused. See <see cref="Bend"/>.</summary>
    public static long Bent { get; private set; }

    private int _bends;

    public BotChop(Map map, Point3D where, int want)
    {
        _map = map;
        _where = where;
        _want = want;
    }

    public override string Kind => Trade;

    /// <summary>A trip to the woods is seen through. See <see cref="BotDeed.Steadfast"/>.</summary>
    public override bool Steadfast => true;

    /// <summary>
    /// <b>One tree without a road is not the woods without one (30.09.2026, build 366).</b> The walk to a trunk refused, the
    /// whole trip failed: 20 of 363's 160 failures were "chop: no way through" in 0.2 minutes, in runs of three a woodsman
    /// at a time — Sigra at (660, 918) and (660, 909), Balgrim at three trunks by The Crown's hall twice, Vistra twice at
    /// (2616, 486) and then 20 logs from the next tree along. The trunk is written off for everybody
    /// (<see cref="BotTimber.Unreachable"/>) and the trip goes on to the next, <see cref="MostBends"/> times.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_tree != null)
        {
            BotTimber.Unreachable(_map, _tree);
            _shunned.Add((_tree.X, _tree.Y));
        }

        _tree = null;

        if (++_bends > MostBends)
        {
            return false;
        }

        Bent++;

        return true;
    }

    /// <summary>The next tree along is the trip's own business, not trouble that lifts its hold. See <see cref="BotDeed.BendIsTrouble"/>.</summary>
    public override bool BendIsTrouble => false;

    /// <summary>
    /// Back at the trees after an interruption: the swing clock and the growth clock are both from before it,
    /// and the pack may have been sold out of in the meantime, so all three are started again.
    /// </summary>
    public override void Resumed(IBotWilful bot)
    {
        _swungTick = 0;
        _grewTick = 0;
        _grewSwings = _swings;
        _adrift = 0;
        _counting = false;
    }

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => SkillName.Lumberjacking;

    public override int Outlay => 0;

    /// <summary>Nothing here is coin. Wood is goods, and what it is worth is what the shard pays for a log.</summary>
    public override double Coin => 0.0;

    /// <summary>Since 01.10.2026 at the island's price only for the logs somebody asks for; the rest at the surplus price. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(typeof(Log), _cut, BotTimber.Worth);

    public override string Stage =>
        _tree == null
            ? $"out to the woods near ({_where.X}, {_where.Y})"
            : $"cutting wood ({_cut} logs in {_swings} swings)";

    /// <summary>Logs in the pack when the trip began, so wood it was already carrying is not counted as cut.</summary>
    private int _had;

    /// <summary>Seeded on the first swing, because the pack is not empty when the trip starts.</summary>
    private bool _counting;

    /// <summary>
    /// Puts the axe in the hand, freeing it first if something else is in it.
    ///
    /// <para>
    /// <c>Mobile.EquipItem</c> refuses a busy layer outright rather than swapping, so whatever the bot is
    /// holding goes into the pack first. It is put back by <see cref="Sheathe"/> when the trip ends, and by
    /// <c>BotMobile.Rearm</c> on its own clock if the trip never does.
    /// </para>
    /// </summary>
    private static bool Wield(Mobile body, Item tool)
    {
        if (tool.Parent == body)
        {
            return true;
        }

        // <b>Both hands, not the first one found.</b> A shield is worn on the TwoHanded layer and a blade on the OneHanded
        // one, and since build 211 a warrior carries both (BotMobile.Rearm). Taking only the first put the shield away and
        // left the blade, so the axe was refused, the next try took the blade, the chop went ahead, and the re-arm after it
        // put both back: Sable failed every other chop, ten times between 14:28 and 14:43 on 24.09.2026, "it cannot get the
        // axe into its hand: Katana on the FirstValid layer is in the way".
        var twoHanded = body.FindItemOnLayer(Layer.TwoHanded);

        if (twoHanded != null && twoHanded != tool)
        {
            body.AddToBackpack(twoHanded);
        }

        var oneHanded = body.FindItemOnLayer(Layer.OneHanded);

        if (oneHanded != null && oneHanded != tool)
        {
            body.AddToBackpack(oneHanded);
        }

        return body.EquipItem(tool);
    }

    /// <summary>The axe back into the pack and the bot's own weapon back into its hand.</summary>
    private static void Sheathe(Mobile body)
    {
        var tool = body?.FindItemOnLayer(Layer.OneHanded) ?? body?.FindItemOnLayer(Layer.TwoHanded);

        if (tool is not Hatchet and not BaseAxe)
        {
            return;
        }

        body.AddToBackpack(tool);

        (body as BotMobile)?.Rearm();
    }

    /// <summary>
    /// Why the engine would not put this axe in the hand, in the engine's own terms, or null when none of them holds.
    /// Asked only after <see cref="Wield"/> has failed, of the body and the tool as they stand then.
    ///
    /// <para>
    /// <b>Build 54 closed the reason that was found and left the sentence covering the rest.</b> A double axe too heavy
    /// for a healer was Marek's five failures at 09:46:43 on 15.09.2026, and <c>BotTimber.Tool</c> stopped offering such
    /// an axe. Then at 11:33:21 Nessa — strong enough for her hatchet, and holding a kryss with nothing two-handed by her
    /// last re-arm lines — failed the same way, and the line could not say why. The engine tells a player in a message to
    /// a client a bot does not have, so this reads the same conditions off the body and the tool: a spell going up
    /// (<c>Mobile.EquipItem</c>), the three requirements and the weapon hold (<c>BaseWeapon.CanEquip</c>), the layer
    /// (<c>Item.CanEquip</c>) and whatever else is in the way (<c>Mobile.CheckEquip</c>).
    /// </para>
    /// </summary>
    private static string Refusal(Mobile body, Item tool)
    {
        if (body.Spell != null)
        {
            return "a spell is going up";
        }

        if (tool is BaseWeapon weapon)
        {
            if (body.Str < weapon.StrRequirement)
            {
                return $"strength {body.Str} against {weapon.StrRequirement}";
            }

            if (body.Dex < weapon.DexRequirement)
            {
                return $"dexterity {body.Dex} against {weapon.DexRequirement}";
            }

            if (body.Int < weapon.IntRequirement)
            {
                return $"intelligence {body.Int} against {weapon.IntRequirement}";
            }

            if (!body.CanBeginAction<BaseWeapon>())
            {
                return "the engine is holding weapons out of its hands for a moment";
            }
        }

        var onLayer = body.FindItemOnLayer(tool.Layer);

        if (onLayer != null && onLayer != tool)
        {
            return $"{onLayer.GetType().Name} is still on the {tool.Layer} layer";
        }

        var worn = body.Items;

        for (var i = 0; i < worn.Count; i++)
        {
            var other = worn[i];

            if (other != tool && (other.CheckConflictingLayer(body, tool, tool.Layer) || tool.CheckConflictingLayer(body, other, other.Layer)))
            {
                return $"{other.GetType().Name} on the {other.Layer} layer is in the way";
            }
        }

        return null;
    }

    /// <summary>
    /// Given up on. The axe goes back in the pack — a woodcutter that walks off to a fight holding a hatchet
    /// is a bot that has quietly swapped its own weapon for a tool, and the shopper will then buy it another
    /// of the weapon it is standing on.
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        base.Drop(bot);

        Sheathe(bot?.Self);
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        var tool = BotTimber.Tool(body);

        if (tool == null)
        {
            return BotDoing.Failed("nothing to cut with");
        }

        // <b>The axe has to be in the hand, and this one line is why not one log has ever been cut on this
        // shard.</b> Lumberjacking is the only harvest with this rule and it enforces it twice:
        // <c>Lumberjacking.CheckHarvest</c> refuses outright when <c>tool.Parent != from</c> and sends
        // "The axe must be equipped for any serious wood chopping" — to a client a bot does not have. Mining
        // has no such rule, which is exactly why the pickaxe worked from the pack and the hatchet never did.
        // Thirty honest swings at a real tree, nought logs, and the engine explaining itself to nobody:
        // 0 logs in 30 swings at 14:01 on 04.09.2026, and the same reading every half hour before it.
        if (!Wield(body, tool))
        {
            var why = Refusal(body, tool);

            return BotDoing.Failed(why == null ? "it cannot get the axe into its hand" : $"it cannot get the axe into its hand: {why}");
        }

        // Enough. Said as done rather than pressed on with: wood is worth the same by the log, so a
        // woodcutter that keeps going past what it came for is a woodcutter carrying its own weight limit.
        if (_cut >= _want)
        {
            Sheathe(body);

            var (ordered, listed) = BotTimber.Store(bot);

            return BotDoing.Done($"{_cut} logs in {_swings} swings, {ordered} to order and {listed} put out to sell");
        }

        // Looked for again every time, because a tree that has been cut out stops being a tree to the
        // engine — and the next one along is usually one tile away.
        _tree ??= BotTimber.Find(body, _shunned);

        if (_tree == null)
        {
            Sheathe(body);

            if (_cut <= 0)
            {
                return BotDoing.Failed("no tree within reach");
            }

            var (ordered, listed) = BotTimber.Store(bot);

            return BotDoing.Done(
                $"{_cut} logs in {_swings} swings, {ordered} to order and {listed} put out to sell — no tree left within reach"
            );
        }

        var trunk = new Point3D(_tree.X, _tree.Y, _tree.Z);

        if (!body.InRange(trunk, BotTimber.SwingReach))
        {
            return BotDoing.Walk(_map, trunk, BotArrival.Within(BotTimber.SwingReach), "to a tree");
        }

        BotTimber.Reached(_map, _tree);

        var now = Core.TickCount;

        if (_swungTick != 0 && now - _swungTick < SwingMs)
        {
            return BotDoing.Work("cutting wood");
        }

        // <b>Counted before the next swing, not after the last one.</b> Harvesting is asynchronous — the
        // engine starts a timer and the wood appears a moment later — so reading the pack in the same breath
        // as the swing reads it too early, every time. It showed as "dropped chop: cutting wood (0 logs in
        // 29 swings)" at 13:31 on 04.09.2026: twenty-nine honest swings at a real tree, nought recorded, the
        // errand never able to reach its own finish, and the whole arrow chain waiting behind it. The same
        // fault as BotBrew and BotFletch had, on the harvest side of the house.
        if (!_counting)
        {
            _counting = true;
            _had = BotTimber.Logs(body);
        }

        var have = BotTimber.Logs(body);

        if (have > _had)
        {
            _cut += have - _had;

            // Into the balance's half-hour window, the same count at the same moment. See BotGlut.
            BotGlut.Made(typeof(Log), have - _had);
            _had = have;
            _grewTick = now;
            _grewSwings = _swings;
        }
        else if (have < _had)
        {
            // Wood left the pack by some other door - sold off a stall, handed to an order - and the count has to
            // follow it down, or every log cut from here on is hidden under the ones that went.
            _had = have;
        }

        if (_cut <= 0 && _swings >= GiveUpSwings)
        {
            Fruitless++;
            Sheathe(body);

            return BotDoing.Failed($"{_swings} swings and not one log");
        }

        // <b>What the engine said about the last swing, before any clock is consulted.</b> Patrick's order of
        // 09.09.2026, the same one the miners got: a tree is cut out when the bot sees the message saying so.
        // Everything below this was inference from silence, and half a minute of swinging at a stump was the
        // price of it.
        var word = BotHeard.Last(body, Server.Engines.Harvest.Lumberjacking.System?.GetDefinition(), out _);

        if (word != BotHeard.Word.Adrift)
        {
            _adrift = 0;
        }

        switch (word)
        {
            case BotHeard.Word.Empty:
            case BotHeard.Word.Taken:
                {
                    BotHeard.Clear(body);
                    Spoken++;

                    // Shunned for the trip like every other tree given up: a cut-out trunk is still a tree to the
                    // nearest-tree search, which would hand it straight back.
                    if (_tree != null)
                    {
                        _shunned.Add((_tree.X, _tree.Y));
                    }

                    _tree = null;
                    _grewTick = 0;
                    _grewSwings = _swings;

                    return BotDoing.Work(
                        _cut > 0 ? $"moving to the next tree, {_cut} logs so far" : "looking for another tree"
                    );
                }

            case BotHeard.Word.Broken:
                {
                    BotHeard.Clear(body);

                    // Logs in the pack are logs cut: the trip is over, and it was not for nothing. See BotForge.
                    return _cut > 0
                        ? BotDoing.Done($"the axe wore out after {_cut} logs")
                        : BotDoing.Failed("the axe wore out");
                }

            case BotHeard.Word.Full:
                {
                    // The log came off the tree and was destroyed for want of room. Carrying on here costs the
                    // wood and pays nobody.
                    BotHeard.Clear(body);
                    Sheathe(body);

                    var (ordered, listed) = BotTimber.Store(bot);

                    return BotDoing.Done(
                        $"{_cut} logs in {_swings} swings, {ordered} to order and {listed} put out to sell — the pack would hold no more"
                    );
                }

            case BotHeard.Word.Adrift:
                {
                    // The swing was cancelled rather than swung, because the bot moved while it resolved. The
                    // tree is not to blame, so the clock below is not allowed to hold it against it.
                    //
                    // <b>Unless it is every swing.</b> "Out of range" from a bot that has not left its tile
                    // is a tree the engine will not let it cut from where it stands, and resetting the clock
                    // for it made the loop below endless: Sable, 220 swings and no log in 8.4 minutes at one
                    // tree on 14.09.2026, Doran 192, nine such trips in twenty minutes, each ended only by
                    // the stall watch — and the nearest-tree search handed the same tree straight back. Four
                    // in a row and the tree is shunned for the rest of the trip.
                    BotHeard.Clear(body);
                    _grewTick = now;

                    if (++_adrift >= AdriftMost && _tree != null)
                    {
                        _shunned.Add((_tree.X, _tree.Y));
                        Unreached++;
                        _tree = null;
                        _adrift = 0;
                        _grewSwings = _swings;

                        return BotDoing.Work(
                            _cut > 0 ? $"the tree was out of reach from here, {_cut} logs so far; looking for another" : "the tree was out of reach from here; looking for another"
                        );
                    }

                    break;
                }
        }

        _swungTick = now;
        _swings++;

        BotTimber.Swing(body, tool, _tree);

        // From here the original fence stands unchanged, and now it means what it says: _grewTick moves
        // whenever wood actually arrives, so "this tree has stopped giving" is a fact rather than a
        // certainty produced by reading the pack a second too soon.
        if (_grewTick == 0)
        {
            _grewTick = now;
            _grewSwings = _swings;

            return BotDoing.Work("cutting wood");
        }

        if (now - _grewTick < StallMs && _swings - _grewSwings < QuietSwings)
        {
            return BotDoing.Work("cutting wood");
        }

        // This one is finished, by the clock rather than by anything the engine said. That is now the unusual
        // ending and it is counted apart, because a clock that keeps firing while the ear stays quiet means
        // the ear has stopped working.
        Silent++;

        // <b>Shunned, or the nearest-tree search hands it straight back.</b> The out-of-range case was
        // shunned in build 17; the silent one was not, and Hale stood at one trunk for 250 swings and ten
        // minutes on 14.09.2026 — thirty seconds of nothing, "trying another", the same tree, again.
        if (_tree != null)
        {
            _shunned.Add((_tree.X, _tree.Y));
        }

        _tree = null;
        _grewTick = 0;
        _grewSwings = _swings;

        if (_cut > 0)
        {
            return BotDoing.Work($"moving to the next tree, {_cut} logs so far");
        }

        logger.Information(
            "{Name} cut at a tree {Swings} times and got nothing; trying another",
            body.Name,
            _swings
        );

        return BotDoing.Work("looking for another tree");
    }
}
