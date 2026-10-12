using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// What the smith's trade knows that no other trade does: its system, its skill, its hammer, its metal, and
/// the one thing about blacksmithing that is genuinely different — it cannot be done just anywhere.
///
/// <para>
/// <b>A smith needs a forge and an anvil, both, within reach.</b> That is the engine's rule and it is
/// checked by the engine — <c>DefBlacksmithy.CheckAnvilAndForge</c> — so a bot standing next to an anvil in a
/// field can swing a hammer all day and produce a message about needing a forge that nobody reads. The good
/// news is that this shard already solved the location half for another reason entirely: <c>BotGround</c>
/// only writes a forge into its list if there is an anvil within a few tiles of it, because the miner needs
/// the same pair to smelt ore. So "where can I smith" is a question that was already being answered, by the
/// people who needed it for something else.
/// </para>
/// </summary>
public static class BotAnvil
{
    /// <summary>The blacksmithing system, or null before content initialisation has built it.</summary>
    public static CraftSystem System => DefBlacksmithy.CraftSystem;

    /// <summary>The skill this trade is measured in.</summary>
    public const SkillName Skill = SkillName.Blacksmith;

    /// <summary>
    /// The metal the engine's recipes are written in, and the last resort when nothing better is known.
    ///
    /// <para>
    /// <b>Everything that looks a recipe up must use this and not <see cref="Metal"/>.</b>
    /// <c>BotCraftwork.Simple</c> matches a recipe by <c>Resources[0].ItemType == material</c>, and every
    /// blacksmithing recipe on this shard names <c>IronIngot</c> whatever it is going to be made of - the
    /// sub-resource is chosen at the swing, not at the lookup. Handing the island's own metal to Choose or
    /// Recipe therefore matches no recipe at all and stops the trade dead, silently, with a smith standing
    /// at a hot forge holding forty bars. Caught here on 08.09.2026 before it ever ran.
    /// </para>
    /// </summary>
    public static Type Base => typeof(IronIngot);

    /// <summary>
    /// The metal to fall back on when this bot is carrying none: whatever the island actually has, not iron.
    ///
    /// <para>
    /// <b>An empty smith ordered a metal this island does not produce, and did it 578 times in a night.</b>
    /// Felucca's veins here give dull copper, bronze, shadow iron, copper and verite - and over a whole
    /// session, not one iron ore. <see cref="Best"/> answers with the dearest metal already in the pack, so
    /// for an empty pack it answered with the constant, the constant was iron, and <c>BotBullion</c> put a
    /// standing order on the board for iron. Miners came back with coloured bars, looked for an order
    /// matching what they held, found none, and banked them. Both halves worked perfectly and never met -
    /// the same shape as the market that never crossed a stall with a want.
    /// </para>
    ///
    /// <para>
    /// The market is the honest source: since 01.10.2026 it is what the shops by the bank show — a gatherer's shift sells its bars
    /// there — so this is not a guess about the island, it is a reading of what the island's own miners have brought back and are
    /// holding out for sale (<see cref="BotShopkeep.Showing"/>; the auction's stalls until then).
    /// </para>
    ///
    /// <para>
    /// Iron when the board holds no metal at all, which is a young shard rather than a wrong one.
    /// </para>
    /// </summary>
    public static Type Metal => Plentiful(double.MaxValue) ?? Base;

    /// <summary>How long one reading of the board stands before it is taken again.</summary>
    public static int PlentifulMs { get; set; } = 60000;

    /// <summary>How many bars of a metal have to be on the board before it counts as one the island has.</summary>
    public static int LeastPlentiful { get; set; } = 10;

    /// <summary>What the board last said the island's metal was, for the report.</summary>
    public static string Reading { get; private set; } = "not read yet";

    private static readonly Dictionary<Type, int> _onBoard = [];

    private static long _readAt;

    /// <summary>
    /// The commonest metal on the market that a bot of this skill could work, or null when there is none.
    ///
    /// Read at most once a minute for the whole population: the board is up to a thousand stalls and this is
    /// asked on every smith's beat.
    /// </summary>
    public static Type Plentiful(double able)
    {
        var system = System;

        if (system == null)
        {
            return null;
        }

        var now = Core.TickCount;

        if (_onBoard.Count == 0 || now - _readAt >= PlentifulMs)
        {
            Read(system);
            _readAt = now;
        }

        var metals = system.CraftSubRes;

        Type best = null;
        var most = LeastPlentiful - 1;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType == null || metal.RequiredSkill > able)
            {
                continue;
            }

            if (!_onBoard.TryGetValue(metal.ItemType, out var held) || held <= most)
            {
                continue;
            }

            best = metal.ItemType;
            most = held;
        }

        return best;
    }

    /// <summary>One pass over the shops by the bank, totalling every metal on show.</summary>
    private static void Read(CraftSystem system)
    {
        _onBoard.Clear();

        var metals = system.CraftSubRes;

        for (var i = 0; i < metals.Count; i++)
        {
            var kind = metals.GetAt(i)?.ItemType;
            var shown = kind == null ? 0 : BotShopkeep.Showing(kind);

            if (shown > 0)
            {
                _onBoard[kind] = shown;
            }
        }

        var say = ValueStringBuilder.Create(128);

        try
        {
            for (var i = 0; i < metals.Count; i++)
            {
                var metal = metals.GetAt(i);

                if (metal?.ItemType == null || !_onBoard.TryGetValue(metal.ItemType, out var held))
                {
                    continue;
                }

                if (say.Length > 0)
                {
                    say.Append(", ");
                }

                say.Append(metal.ItemType.Name);
                say.Append(' ');
                say.Append(held);
            }

            Reading = say.Length == 0 ? "no metal on show at the shops at all, so iron stands" : say.ToString();
        }
        finally
        {
            say.Dispose();
        }
    }

    /// <summary>The hammer this bot forges with, if it is carrying one.</summary>
    public static SmithHammer Kit(Mobile bot) => BotOutfit.Oldest<SmithHammer>(bot?.Backpack);

    /// <summary>How much of a given metal is in the pack. The island's own metal when nothing else is named.</summary>
    public static int Ingots(Mobile bot, Type metal = null) => bot?.Backpack?.GetAmount(metal ?? Metal) ?? 0;

    /// <summary>
    /// The skill a metal's floor is held against: the base, not the value, because that is what the engine reads.
    ///
    /// <para>
    /// <b>Two readings of one skill, and the gap between them is a smith swinging at metal it may not touch.</b>
    /// <c>CraftItem</c> refuses a sub-resource when <c>Skills[MainSkill].Base</c> is under its floor (line 647, message
    /// 1044268, "you cannot work this strange and unusual metal"), and this class asked <c>Value</c>, which a hammer's
    /// bonus or a stat lifts above the base. Calla on build 202 (24.09.2026) was given a metal her base would not take
    /// and swung at a short spear twenty-four times, sixteen stints in 38 minutes (08:13–08:36), "24 attempts, 0 made"
    /// each, until the breaker rested her.
    /// </para>
    /// </summary>
    public static double Able(Mobile bot) => bot?.Skills[Skill].Base ?? 0.0;

    /// <summary>
    /// The Blacksmithy base a metal asks for, off the engine's own sub-resources: nought for iron, sixty-five for dull copper,
    /// ninety-nine for valorite. Below nought for anything that is not a smith's metal.
    /// </summary>
    public static double Rung(Type metal)
    {
        var metals = System?.CraftSubRes;

        if (metal == null || metals == null)
        {
            return -1.0;
        }

        for (var i = 0; i < metals.Count; i++)
        {
            var known = metals.GetAt(i);

            if (known?.ItemType == metal)
            {
                return known.RequiredSkill;
            }
        }

        return -1.0;
    }

    /// <summary>
    /// What a bar of a coloured metal is reckoned at, over iron's price, for each hundred points of Blacksmithy its floor asks.
    /// One: dull copper at 1.65 iron, bronze at 1.8, valorite at 1.99. Nought prices a coloured bar as iron.
    /// </summary>
    public static double ColouredPremium { get; set; } = 1.0;

    /// <summary>
    /// What one bar of a coloured metal is worth before the island has traded any: iron's price — what the island pays for iron,
    /// else the smith's own guess at it (<see cref="BotBullion.Guess"/>) — raised by the metal's floor. Nought for iron and for
    /// anything that is not a smith's metal, so every other kind is priced exactly as before.
    ///
    /// <para>
    /// <b>No shopkeeper in this era buys or sells a coloured ingot, and since 01.10.2026 that kept every one of them off the
    /// market.</b> A bot's shop opens a kind at the shopkeeper's shelf, else at what a shopkeeper pays for one, else at a price the
    /// island has bid or traded (<c>BotShopkeep.Opening</c>), and a coloured bar has none of the three until a smith has asked for
    /// it — so it was never shown; <see cref="Plentiful"/>, which reads the shops, never saw it; and a smith with an empty pack
    /// ordered iron (<see cref="Best"/>) on an island whose miners dig coloured ore in 93 stints of a hundred (build 384). The
    /// auction this market replaced listed them at a guess, which is how the board of 384 came to show 622. This is that guess,
    /// made once, for both sides: the shop's opening and the smith's offer (<c>BotBullion</c>) read the same number, so a smith that
    /// can work the bar meets a shop that shows it.
    /// </para>
    /// </summary>
    public static int Reckon(Type metal)
    {
        var rung = Rung(metal);

        if (rung <= 0.0)
        {
            return 0;
        }

        var iron = BotMarket.Worth(Base, BotBullion.Guess);

        return (int)Math.Max(1.0, Math.Round(iron * (1.0 + Math.Max(0.0, ColouredPremium) * rung / 100.0)));
    }

    /// <summary>A metal's name for a person: "Dull Copper" for <c>DullCopperIngot</c>, "Iron" for iron and for nothing named.</summary>
    public static string Named(Type metal)
    {
        var name = metal?.Name ?? Base.Name;

        if (name.EndsWith("Ingot", StringComparison.Ordinal))
        {
            name = name[..^5];
        }

        return BotMarket.Spaced(name);
    }

    /// <summary>
    /// Every bot that works an anvil, by the best metal its Blacksmithy base opens now: "Iron 3, Dull Copper 2, Bronze 1". Counted
    /// over the population on the five-minute line, because the question "can anybody work what the miners dig" is a fact about
    /// the bots and not about any one stint.
    /// </summary>
    public static string Hands()
    {
        var metals = System?.CraftSubRes;

        if (metals == null)
        {
            return "no smithing system yet";
        }

        _hands.Clear();

        Tally(BotPopulation.Bots, metals);
        Tally(BotPopulation.Away, metals);

        if (_hands.Count == 0)
        {
            return "nobody works an anvil";
        }

        using var said = ValueStringBuilder.Create(128);

        for (var i = 0; i < metals.Count; i++)
        {
            var kind = metals.GetAt(i)?.ItemType;

            if (kind == null || !_hands.TryGetValue(kind, out var many))
            {
                continue;
            }

            if (said.Length > 0)
            {
                said.Append(", ");
            }

            said.Append(Named(kind));
            said.Append(' ');
            said.Append(many);
        }

        return said.ToString();
    }

    private static readonly Dictionary<Type, int> _hands = [];

    private static void Tally(IReadOnlyList<BotMobile> bots, CraftSubResCol metals)
    {
        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot == null || bot.Deleted || bot.Class is not { } klass || !klass.Wants(Skill))
            {
                continue;
            }

            var able = Able(bot);
            Type best = null;
            var bestNeeds = -1.0;

            for (var j = 0; j < metals.Count; j++)
            {
                var metal = metals.GetAt(j);

                if (metal?.ItemType != null && metal.RequiredSkill <= able && metal.RequiredSkill > bestNeeds)
                {
                    best = metal.ItemType;
                    bestNeeds = metal.RequiredSkill;
                }
            }

            if (best != null)
            {
                _hands[best] = _hands.GetValueOrDefault(best) + 1;
            }
        }
    }

    /// <summary>
    /// The dearest metal this bot carries enough of and its skill will take, or iron.
    ///
    /// <para>
    /// <b>Every recipe on this shard says "iron ingot" and none of them means it.</b> The engine's smithy
    /// takes eight metals and they make the same items in better material — the recipe names iron because
    /// iron is the <em>base</em> resource, and which metal actually goes in is chosen at the moment of the
    /// swing. Nothing here ever chose: <see cref="Metal"/> was a constant, so a population that dug forty
    /// bronze to five iron in a session could use one ore in nine and piled the rest up.
    /// </para>
    ///
    /// <para>
    /// <b>The skill floor is the engine's, read rather than copied.</b> Every metal carries its own
    /// requirement — sixty-five for dull copper, eighty for bronze, ninety-nine for valorite — and those
    /// numbers already exist in <c>DefBlacksmithy</c>. A second table of them here is the defect this project
    /// has paid for more than any other, so this walks the system's own sub-resources instead.
    /// </para>
    ///
    /// <para>
    /// Dearest first, because a better metal is a better piece for the same swing and the same walk to the
    /// mine. Falling back to iron rather than to nothing: a smith that will not work because it has no
    /// valorite is a smith standing still.
    /// </para>
    /// </summary>
    /// <param name="need">How many the recipe eats. A metal the bot cannot fill a recipe with is no use.</param>
    public static Type Best(Mobile bot, int need)
    {
        var system = System;
        var pack = bot?.Backpack;

        if (system == null || pack == null)
        {
            return Metal;
        }

        var able = Able(bot);
        var metals = system.CraftSubRes;

        Type best = null;
        var bestNeeds = -1.0;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType == null || metal.RequiredSkill > able || metal.RequiredSkill <= bestNeeds)
            {
                continue;
            }

            if (pack.GetAmount(metal.ItemType) < need)
            {
                continue;
            }

            best = metal.ItemType;
            bestNeeds = metal.RequiredSkill;
        }

        // <b>Nothing in the pack, so this is the answer that becomes an order.</b> Falling back to the
        // constant here is what put 578 standing orders for iron on a board no miner could fill; falling back
        // to the island's own metal, at a skill this bot can actually work, is an order somebody comes back
        // with. See Plentiful.
        return best ?? Plentiful(able) ?? Base;
    }

    /// <summary>
    /// How many bars of a coloured metal must lie in the island's bank boxes before an empty smith orders that metal rather than
    /// iron (06.10.2026, box16). Two hundred: ten batches; the census of 05:35 read thousands of each of four metals.
    /// </summary>
    public static int BoxedLeast { get; set; } = 200;

    /// <summary>Whether an empty smith's order reads the metal in the bank boxes at all. See <see cref="Order"/>.</summary>
    public static bool OrdersBoxed { get; set; } = true;

    private static readonly Dictionary<Type, int> _boxed = [];

    /// <summary>The bars in every bot's bank box by metal, from the pack census (<c>BotUnload.Weighed</c>, every five minutes).</summary>
    public static void Boxed(Dictionary<Type, int> metal)
    {
        _boxed.Clear();

        if (metal == null)
        {
            return;
        }

        foreach (var (kind, units) in metal)
        {
            _boxed[kind] = units;
        }
    }

    /// <summary>
    /// The metal a smith short of metal puts its order in (<see cref="BotBullion"/>): what <see cref="Best"/> finds in its pack;
    /// when the pack fills no batch, the coloured metal its skill can work that the island's bank boxes hold most of, past
    /// <see cref="BoxedLeast"/>; else <see cref="Best"/>'s own fallback (the shops' metal, else iron). <paramref name="boxed"/> says
    /// the boxes chose it.
    ///
    /// <para>
    /// <b>Ten thousand coloured bars lay in bank boxes with no way out (06.10.2026, box16; money15's census).</b> No shopkeeper of
    /// this era buys a coloured bar, and a smith with an empty pack ordered iron, because the shops by the bank show no metal at
    /// all ("the board's metal reads no metal on show at the shops at all, so iron stands", every summary of 06.10.2026), so the
    /// one buyer the bars have never asked for them. The few coloured wants that were said were met: CopperIngot 4, DullCopper 3,
    /// Bronze 2: all 9 lines met in their hour on build 405 (02:05–05:36), carried by <c>BotCourier</c>, which takes a carrier's bars out
    /// of its own box when it stands at a bank. The
    /// player's loop is the honest exit: bars to a smith, the smith's daggers and plate to the smith's shopkeeper at the item's
    /// price (<c>GenericSellInfo</c> prices a bronze dagger as a dagger). Only a metal the smith can work, so the order is never
    /// for bars it would then carry about unused; gold, agapite, verite and valorite wait for a smith of 85 to 99.
    /// </para>
    /// </summary>
    public static Type Order(Mobile bot, int need, out bool boxed)
    {
        boxed = false;

        var system = System;
        var pack = bot?.Backpack;

        if (system == null || pack == null)
        {
            return Best(bot, need);
        }

        var able = Able(bot);
        var metals = system.CraftSubRes;

        Type held = null;
        var heldNeeds = -1.0;
        Type most = null;
        var mostUnits = BoxedLeast - 1;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType == null || metal.RequiredSkill > able)
            {
                continue;
            }

            if (metal.RequiredSkill > heldNeeds && pack.GetAmount(metal.ItemType) >= need)
            {
                held = metal.ItemType;
                heldNeeds = metal.RequiredSkill;
            }

            if (metal.ItemType != Base && _boxed.TryGetValue(metal.ItemType, out var units) && units > mostUnits)
            {
                most = metal.ItemType;
                mostUnits = units;
            }
        }

        if (held != null)
        {
            return held;
        }

        if (OrdersBoxed && most != null)
        {
            boxed = true;

            return most;
        }

        return Plentiful(able) ?? Base;
    }

    /// <summary>
    /// Takes this bot's own metal back off its own stall, whatever metal it is, and says how much came back.
    ///
    /// <para>
    /// <b>Three gates in a ring, and the smith starved inside it.</b> <c>BotBullion</c> refuses to order iron
    /// from a bot that is selling iron, and says in as many words that the metal is "fetched back at the
    /// anvil, see BotForge" — which is true, <see cref="BotForge"/> reclaims when it runs short mid-work. But
    /// <c>BotSmith</c> will not offer the anvil at all below <c>LeastMetal</c>, so a smith whose ingots were
    /// on its own stall could never reach the errand that would fetch them. The two halves of it printed side
    /// by side in the same sentence every five minutes and never met: at 21:42 on 04.09.2026,
    /// "241 short of metal" beside "160 have their own out on a stall", with nought forged all session.
    /// </para>
    ///
    /// <para>
    /// So the fetch happens where the shortage is noticed, which is the fletcher's rule about its own
    /// feathers and the tailor's about its own leather. Nothing is bought and nothing walks: the market holds
    /// a stall's goods out of the world, so a bot takes its own back from wherever it is standing.
    /// </para>
    /// </summary>
    /// <summary>
    /// Caps every metal this bot could work at <paramref name="amount"/> in a keep-list, rather than iron
    /// alone.
    ///
    /// <para>
    /// <b>Two lists of what counts as metal, and they were about to be set against each other.</b>
    /// <c>BotUnload.Needed</c> named <see cref="Metal"/> — iron and nothing else — so a smith's bronze was
    /// merchandise and went out on a stall, while <see cref="Best"/> and <see cref="Fetch"/> know every
    /// sub-resource the bot's skill allows. Left as it was, the porter would list the bronze and the smith
    /// would fetch it straight back, for ever, on the beat. The same reading, in one place, is the only way
    /// two subsystems can agree about a thing.
    /// </para>
    /// </summary>
    public static void Keep(Mobile body, Dictionary<Type, int> keep, int amount)
    {
        var system = System;

        if (system == null || keep == null)
        {
            keep?.TryAdd(Base, amount);

            return;
        }

        keep[Base] = amount;

        if (body == null)
        {
            return;
        }

        var able = Able(body);
        var metals = system.CraftSubRes;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType != null && metal.RequiredSkill <= able)
            {
                keep[metal.ItemType] = amount;
            }
        }
    }

    public static int Fetch(IBotWilful bot, Mobile body, int need)
    {
        var system = System;
        var pack = body?.Backpack;

        if (system == null || pack == null || bot == null)
        {
            return 0;
        }

        var able = Able(body);
        var metals = system.CraftSubRes;
        var back = 0;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            // Only metal this bot could actually work: what it cannot smith waits in the box for a shift or a carrier.
            if (metal?.ItemType == null || metal.RequiredSkill > able)
            {
                continue;
            }

            // Out of its own bank box, standing at a bank (01.10.2026: the box is reached only there, the stall this took its metal
            // back off was reached from anywhere).
            back += BotMarket.FromBank(bot, metal.ItemType, need);

            if (Ingots(body, metal.ItemType) >= need)
            {
                break;
            }
        }

        return back;
    }

    /// <summary>
    /// How near an anvil and a forge a body has to be for the engine to let it smith.
    ///
    /// <para>
    /// <b>Two, because <c>DefBlacksmithy.CanCraft</c> says two.</b> It is written here rather than borrowed
    /// because the engine writes it as a literal and there is nothing to borrow — so what this constant owes
    /// the reader is the line it mirrors, and that line is the only reason it may ever change.
    /// </para>
    /// </summary>
    public static int Reach { get; set; } = 2;

    /// <summary>
    /// Whether this bot is standing where a hammer is any use. Asked of the engine, not of a list.
    ///
    /// <para>
    /// <b>Asking the right authority the wrong question is not asking it.</b> This called the engine's own
    /// test — and handed it <c>BotGround.AnvilReach</c>, three, which is a number about how far an anvil may
    /// sit from a forge for the pair to be worth remembering. The engine judges by two. So the gate was
    /// looser than the authority behind it, and a gate looser than its authority passes exactly the cases
    /// that go on to fail: the bot stood two tiles the far side of the forge, was told it was at a smithy,
    /// swung twenty-four times, and the engine refused every one — silently, because a refusal is a message
    /// to a client and a bot has none. "24 attempts, 0 made", over and over, at an eighty-seven per cent
    /// failure rate for as long as anybody has been watching, and not one line in the log to say why.
    /// </para>
    ///
    /// <para>
    /// The engine also wants line of sight and the same floor, which no list of coordinates could ever have
    /// promised. That is the whole argument for asking it rather than modelling it — and the argument only
    /// works if it is asked its own question.
    /// </para>
    /// </summary>
    public static bool AtASmithy(Mobile bot)
    {
        if (bot == null)
        {
            return false;
        }

        DefBlacksmithy.CheckAnvilAndForge(bot, Reach, out var anvil, out var forge);

        return anvil && forge;
    }

    /// <summary>
    /// The floor nearest <paramref name="from"/> that is within <see cref="Reach"/> of this forge and of an anvil on the
    /// forge's own storey, with nobody standing on it — or nought when no anvil stands near enough for any tile to be both.
    ///
    /// <para>
    /// <b>Beside the forge is not beside the anvil.</b> Britain's upper-storey smithy has its forge at (1424, 1558, 30) and
    /// its anvil at (1423, 1556, 30), two tiles north; a smith arriving beside the forge from the south stands three tiles
    /// from the anvil and the engine refuses the hammer. Ten smiths failed there on the night of 24.09.2026 ("no anvil the
    /// engine will accept within 2 of the forge"), one after every restart, because the hour's rest the failure writes
    /// (<c>BotGround.UnfitMs</c>) lives in memory. The tile a step or two round the forge is the one the engine says yes at.
    /// </para>
    /// </summary>
    public static Point3D Between(Map map, Point3D forge, Point3D from)
    {
        var best = Point3D.Zero;

        if (map == null || map == Map.Internal)
        {
            return best;
        }

        var bestOff = int.MaxValue;
        var span = Reach * 2;

        // Anvils as items — the decoration pass lays Felucca's smithies down as addons — and as map tiles, no further from
        // the forge than two reaches, beyond which no tile is within one reach of both.
        foreach (var item in map.GetItemsInRange(forge, span))
        {
            if (BotGround.IsAnvilId(item.ItemID) && Math.Abs(item.Z - forge.Z) <= BotArrival.PersonHeight)
            {
                Stand(map, forge, item.Location, from, ref best, ref bestOff);
            }
        }

        for (var dx = -span; dx <= span; dx++)
        {
            for (var dy = -span; dy <= span; dy++)
            {
                foreach (var tile in map.Tiles.GetStaticAndMultiTiles(forge.X + dx, forge.Y + dy))
                {
                    if (BotGround.IsAnvilId(tile.ID) && Math.Abs(tile.Z - forge.Z) <= BotArrival.PersonHeight)
                    {
                        Stand(map, forge, new Point3D(forge.X + dx, forge.Y + dy, tile.Z), from, ref best, ref bestOff);
                    }
                }
            }
        }

        return best;
    }

    private static void Stand(Map map, Point3D forge, Point3D anvil, Point3D from, ref Point3D best, ref int bestOff)
    {
        for (var x = forge.X - Reach; x <= forge.X + Reach; x++)
        {
            for (var y = forge.Y - Reach; y <= forge.Y + Reach; y++)
            {
                if (Math.Abs(x - anvil.X) > Reach || Math.Abs(y - anvil.Y) > Reach)
                {
                    continue;
                }

                // A floor on the forge's storey — the engine wants both within a person's height of the smith — that a body
                // fits on and nobody is standing on now.
                if (!BotStep.Ground(map, x, y, forge.Z, BotArrival.PersonHeight / 2, out var z)
                    || !map.CanFit(x, y, z, BotArrival.PersonHeight, false, true))
                {
                    continue;
                }

                var off = Math.Max(Math.Abs(x - from.X), Math.Abs(y - from.Y));

                if (off < bestOff)
                {
                    best = new Point3D(x, y, z);
                    bestOff = off;
                }
            }
        }
    }

    /// <summary>The hardest thing this bot can still reliably beat out of iron, or null.</summary>
    /// <summary>
    /// The most of any one metal this smith could actually swing with.
    ///
    /// <para>
    /// A recipe is paid for out of a single metal — <see cref="Best"/> picks which — so what decides whether
    /// a piece can be made is the largest single pile, not the total. A bot holding eight iron and eight
    /// bronze can make an eight-ingot piece and not a sixteen-ingot one, and adding them up would say
    /// otherwise.
    /// </para>
    /// </summary>
    public static int Stock(Mobile bot)
    {
        var system = System;
        var pack = bot?.Backpack;

        if (system == null || pack == null)
        {
            return 0;
        }

        var able = Able(bot);
        var metals = system.CraftSubRes;
        var most = pack.GetAmount(Base);

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType == null || metal.RequiredSkill > able)
            {
                continue;
            }

            var held = pack.GetAmount(metal.ItemType);

            if (held > most)
            {
                most = held;
            }
        }

        return most;
    }

    /// <summary>
    /// How many attempts a stint at the anvil is set up to survive. Three.
    ///
    /// <para>
    /// <b>A failed swing is not a free swing: the engine eats half the metal for it.</b>
    /// <c>CraftItem.ConsumeRes</c> takes <c>amounts[i] - amounts[i] / 2</c> on a failure and
    /// <c>CraftSystem.ConsumeOnFailure</c> is true for smithing, so a broadsword that does not come off costs
    /// five of its ten ingots. Every number in this trade was written as though a swing that failed cost
    /// nothing, and the whole of the waste follows from that one assumption: a smith set out holding exactly
    /// what one piece needs, missed twice, and walked home. On 05.09.2026 that was thirty-eight of the
    /// fifty-two stints that ended in nothing, and the log wrote the same sentence every time —
    /// <em>2 attempts, 0 made — out of metal</em>.
    /// </para>
    ///
    /// <para>
    /// <b>What three actually buys, said properly, because the first version of this note said two.</b> A
    /// miss costs half a piece, so <c>3c</c> carries four misses and then the piece — not two, which is
    /// <c>2c</c>. Three is kept because it is what was measured: stints producing something went from three
    /// in ten to eight in ten. Two was never tried, and the note claiming three was the smallest number that
    /// would do was arithmetic that did not match its own number.
    ///
    /// <para>
    /// <b>It is not free.</b> Dividing the pile by it turned 82 smiths in one window into "with metal but not
    /// enough for any recipe they can work", where that bucket used to read nought. The trade is ahead —
    /// more pieces out of fewer stints — but this is a dial that has been chosen once and never tuned, and
    /// the number to watch when tuning it is that bucket against the count of stints that produced something.
    /// </para>
    ///
    /// <para>
    /// A multiplier and not a floor, because what a round eats depends on what is being made, and a number
    /// that did not depend on the recipe is how these two thresholds came to sit on one shelf in the first
    /// place.
    /// </para>
    /// </para>
    /// </summary>
    public static int Tries { get; set; } = 3;

    /// <summary>
    /// The best thing this bot could make and see through, or null.
    ///
    /// The stock is divided by <see cref="Tries"/> so that what is chosen is what the pack can afford to
    /// attempt, rather than what it can afford to attempt once. Choosing on the undivided pile is choosing
    /// the dearest recipe the metal will just barely cover, which is the recipe most likely to end the round
    /// with nothing.
    /// </summary>
    public static CraftItem Choose(Mobile bot) =>
        BotCraftwork.Choose(bot, System, Skill, Base, Stock(bot) / Math.Max(1, Tries));

    /// <summary>The recipe for exactly this thing, if this bot could make one. For orders off the board.</summary>
    public static CraftItem Recipe(Mobile bot, Type wanted) =>
        BotCraftwork.Recipe(bot, System, Skill, Base, wanted);

    /// <summary>
    /// One attempt at the anvil, in the metal named. The metal is what the engine calls the sub-resource, and
    /// passing anything but iron is the whole of how a bronze helm comes to exist. See <see cref="Best"/>.
    /// </summary>
    public static bool Swing(Mobile bot, CraftItem recipe, BaseTool tool, Type metal = null) =>
        BotCraftwork.Swing(bot, System, recipe, metal ?? Base, tool);

    /// <summary>How many of that thing are in the pack.</summary>
    public static int Made(Mobile bot, Type kind) => BotCraftwork.Made(bot, kind);

    /// <summary>Everything of that kind in the pack.</summary>
    public static List<Item> Gather(Mobile bot, Type kind) => BotCraftwork.Gather(bot, kind);
}
