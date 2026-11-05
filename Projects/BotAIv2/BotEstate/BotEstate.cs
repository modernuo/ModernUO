using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The halls the guilds own: where they stand, who paid for them, and what they hold.
///
/// <para>
/// <b>Ordered by Patrick on 08.09.2026, and the shape of the order is the interesting part.</b> The plan
/// said "a bot buys a house". The arithmetic said otherwise: the cheapest classic house in this engine costs
/// 35,250gp (<c>HousePlacementEntry.ClassicHouses</c>), the fattest purse on this shard was 843gp when the
/// question was asked, the middle one 355gp, and the whole population of forty-nine held under 25,000gp
/// between them — with <c>BotProgress.Savings</c> off by his own order of the same day, so none of it
/// survives a restart. A house per bot at the engine's price is a threshold nothing on the shard can reach:
/// the exact defect this project keeps meeting, two numbers on one shelf that never meet.
/// </para>
///
/// <para>
/// So the guild buys it. Four halls rather than forty-nine huts, paid for out of a levy on the members, and
/// the guild tag stops being a word over a head: it is the thing that opens a door, holds a chest, and owns
/// the forge inside. That is what makes <see cref="BotGuilds"/> worth having.
/// </para>
///
/// <para>
/// <b>A hall outlives the population that raised it, and that is what this class is really for.</b> Bots are
/// rebuilt from nothing every start and their owner mobile is deleted with them; the house is a world object
/// and stays. An unowned house in this engine decays. So every start this reads the world back — houses are
/// found by the name on their sign, which is the guild's — and hands each one to whoever leads that guild
/// now. The sign is the record. Nothing else is written down, because anything else could disagree with the
/// world and this cannot.
/// </para>
/// </summary>
public static class BotEstate
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEstate));

    /// <summary>Whether the population buys halls at all.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>
    /// What a hall costs the guild.
    ///
    /// <para>
    /// <b>Not the engine's 35,250, and the reason is measured rather than felt.</b> The population earns on
    /// the order of 500gp a minute between all of it, most of which is spent again on materials, lessons and
    /// kit; at the engine's price a single hall is over an hour of every bot's entire net income and there
    /// would never be a second one. At five thousand a large guild raises one within the hour and a small one
    /// takes an evening, which is what a milestone should feel like on a shard somebody watches.
    /// </para>
    ///
    /// <para>
    /// It is a dial because it is a judgement, and the number to watch beside it is in <see cref="Describe"/>:
    /// what each guild has, and how far short of this it is. A guild that is short by the same amount for an
    /// hour is this number set too high, and the line says so rather than reporting a silent zero.
    /// </para>
    /// </summary>
    public static int Price { get; set; } = 5000;

    /// <summary>
    /// What a member keeps back when the levy is called.
    ///
    /// <para>
    /// A levy that empties its own guild is a guild that cannot buy bandages the next minute. This is the
    /// floor under the contribution — see <c>factor-without-a-floor-is-a-veto</c>, which is the same lesson
    /// upside down: a rule that takes everything is as harmful as a rule that allows nothing.
    /// </para>
    ///
    /// <para>
    /// <b>Eight hundred since 02.10.2026 (build 389; Patrick's choice of option 1 in poverty-388.md).</b> At three hundred the
    /// keep was a ceiling: <c>BotGuilds.Stand</c> drew every member down to it for anybody's supplies — 58,229gp in the nine
    /// hours of build 388, 113,213 times it could not — and not one guild member of any class held more than 300gp while the
    /// guildless traders held 500 to 6,000. Every price that needs savings sat above it: the armourer's reserve 400, a horse
    /// 600, the gear purse 800. The keep now stands at the gear purse, so what a member earns past the horse and the armour is
    /// its own; the stand takes a share of what lies above it (<c>BotGuilds.StandShare</c>) and the dues are taken only above
    /// it (<c>BotDues.Pay</c>).
    /// </para>
    /// </summary>
    public static int Keep { get; set; } = 800;

    /// <summary>How many halls the island may hold. A ceiling on a permanent change to the world.</summary>
    public static int MaxHalls { get; set; } = 4;

    /// <summary>Whether guilds may hire merchants for their halls at all.</summary>
    public static bool Merchanting { get; set; } = true;

    /// <summary>
    /// How many merchants one hall may hold.
    ///
    /// One to begin with. A merchant is a mobile that stands in the world for ever, charges wages and has to
    /// be looked at to be worth anything; five of them in a room is a market stall with extra steps.
    /// </summary>
    public static int MostMerchants { get; set; } = 1;

    /// <summary>Merchants hired, and what their contracts came to.</summary>
    public static long Merchanted { get; private set; }

    public static long Contracts { get; private set; }

    /// <summary>Halls standing this moment, by the guild that owns them.</summary>
    private static readonly Dictionary<string, BaseHouse> _halls = new();

    /// <summary>Halls bought by this population, since the shard started.</summary>
    public static long Raised { get; private set; }

    /// <summary>Halls found in the world at boot and handed to the guild that owns them now.</summary>
    public static long Adopted { get; private set; }

    /// <summary>Gold taken from members to pay for halls.</summary>
    public static long Levied { get; private set; }

    /// <summary>Members who paid into a levy.</summary>
    public static long Payers { get; private set; }

    /// <summary>Purchases that fell over after the ground was found. Each one has a word in the log.</summary>
    public static long Failed { get; private set; }

    /// <summary>Fittings — forges, looms, ovens — set up inside halls.</summary>
    public static long Fitted { get; private set; }

    /// <summary>Halls razed by hand. See <see cref="Raze"/>.</summary>
    public static long Razed { get; private set; }

    /// <summary>Times a workshop was passed over because it stands in somebody else's hall.</summary>
    public static long Barred { get; private set; }

    /// <summary>
    /// Whether this bot may use a thing standing at this spot.
    ///
    /// <para>
    /// <b>Patrick's order of 08.09.2026: a guild's tools are the guild's.</b> The engine will not do this for
    /// us — a forge, a loom and an oven have no access check of any kind, and <c>CraftItem.NearHeatSource</c>
    /// asks only whether a fire is within reach. So the rule has to be applied where a workshop is chosen,
    /// which is <c>BotGround.Pick</c>, and this is the test it asks.
    /// </para>
    ///
    /// <para>
    /// <b>Asked of the engine's own idea of a friend</b>, so it cannot drift from what a door would decide:
    /// owner, co-owner, friend. Our halls make every member of the guild a co-owner, so a guildmate passes
    /// and a stranger does not.
    /// </para>
    ///
    /// <para>
    /// It is a hard rule rather than a preference, and that is safe for one reason worth writing down: it
    /// only ever refuses a place that is <em>inside a house</em>. Britain's own forges, fires and counters
    /// stand in the street and are refused to nobody, so a population locked out of every guild hall on the
    /// island still has everything it had the day before halls existed.
    /// </para>
    /// </summary>
    public static bool MayUse(Mobile bot, Map map, Point3D where)
    {
        if (!Running || bot == null || map == null || map == Map.Internal)
        {
            return true;
        }

        var house = BaseHouse.FindHouseAt(where, map, 16);

        if (house is not { Deleted: false })
        {
            return true;
        }

        return house.IsOwner(bot) || house.IsCoOwner(bot) || house.IsFriend(bot);
    }

    /// <summary>Counts one such refusal. Called by the chooser, once per choice rather than once per pass.</summary>
    public static void Bar() => Barred++;

    /// <summary>The hall this guild owns, or null.</summary>
    public static BaseHouse Hall(Guild guild)
    {
        if (guild == null || !_halls.TryGetValue(guild.Name, out var house))
        {
            return null;
        }

        if (house is { Deleted: false })
        {
            return house;
        }

        _halls.Remove(guild.Name);

        return null;
    }

    /// <summary>Whether this bot's guild has a hall, and where it is.</summary>
    public static BaseHouse Hall(Mobile bot) => Hall(bot?.Guild as Guild);

    /// <summary>Halls standing, for anything that wants to walk to one.</summary>
    public static IEnumerable<BaseHouse> Halls => _halls.Values;

    /// <summary>
    /// Whose hall this is.
    ///
    /// <para>
    /// The register is kept by guild name rather than by object, because a guild object is replaced by a
    /// world reload and a name is not. So the way back is through the engine's own roll of guilds, which is
    /// one dictionary lookup and cannot disagree with what a member's own <c>Guild</c> property says. See
    /// <see cref="BotLand"/>, which asks this of every hall a few thousand times a second.
    /// </para>
    /// </summary>
    public static Guild Whose(BaseHouse hall) => BaseGuild.FindByName(WhoseName(hall)) as Guild;

    /// <summary>The name of the guild whose hall this is, which is what the register is actually keyed by.</summary>
    public static string WhoseName(BaseHouse hall)
    {
        if (hall is not { Deleted: false })
        {
            return null;
        }

        foreach (var (name, house) in _halls)
        {
            if (house == hall)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Every hall with the name of the guild that holds it.
    ///
    /// For anything that has to walk the whole register rather than ask about one hall — the land claim
    /// does, on every piece of work weighed, so it gets the pairs in one pass instead of a lookup each.
    /// </summary>
    /// <para>
    /// Typed as the dictionary itself rather than as an interface, and that is a hot-path decision: a
    /// <c>foreach</c> over <c>IEnumerable</c> boxes the enumerator on every call, and the land claim asks
    /// this on every piece of work every bot weighs.
    /// </para>
    public static Dictionary<string, BaseHouse> Held => _halls;

    /// <summary>How many stand.</summary>
    public static int Standing => _halls.Count;

    /// <summary>
    /// What the guild could raise between it: what its chest holds, and what its members could spare keeping
    /// <see cref="Keep"/> back for each.
    ///
    /// <para>
    /// <b>The chest is counted, since 29.09.2026, because the levy takes it first.</b> <see cref="Take"/> has drawn the
    /// chest before any member's pack since 16.09.2026, while this — the number every purchase's gate asks, the
    /// steward's first of all — added up the members alone: a chest of ten thousand read as a guild with nothing, and
    /// four of the callers had learnt to add the chest themselves. The members' half is still asked of their actual
    /// purses every time rather than kept as a running total: a bot spends its own money on its own business every
    /// minute, and a total that only ever went up would be promising coin that was eaten hours ago. The half that does
    /// keep is the chest, which dues fill (<see cref="BotDues"/>).
    /// </para>
    /// </summary>
    public static int Fund(Guild guild) => guild == null ? 0 : BotChest.Holds(guild.Name) + Spare(guild);

    /// <summary>
    /// What the guild's members could spare between them, keeping <see cref="Keep"/> back for each: the members' half of
    /// <see cref="Fund"/>, without the chest. What the crown's tax is reckoned on — a tax on members, never on the guild's
    /// savings.
    /// </summary>
    public static int Spare(Guild guild)
    {
        if (guild?.Members == null)
        {
            return 0;
        }

        var total = 0;

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is not BotMobile { Deleted: false } member)
            {
                continue;
            }

            var spare = BotYield.Spendable(member) - Keep;

            if (spare > 0)
            {
                total += spare;
            }
        }

        return total;
    }

    /// <summary>What one member paid, kept so it can be handed back if the purchase falls over.</summary>
    public readonly struct Contribution
    {
        public Contribution(BotMobile who, int paid)
        {
            Who = who;
            Paid = paid;
        }

        public BotMobile Who { get; }

        public int Paid { get; }
    }

    /// <summary>
    /// Takes <paramref name="want"/> from the guild's members, richest first.
    ///
    /// <para>
    /// <b>The pocket before the account, and every payment on this shard does it in that order</b> — see
    /// <c>BotStable.Buy</c>, where the reasoning lives. This returns what it managed to take and the caller
    /// hands it straight back if the purchase then fails: a levy that half-succeeds is a guild poorer with
    /// nothing to show for it, and any other ordering is a way to lose a bot's money.
    /// </para>
    /// </summary>
    public static int Levy(Guild guild, int want, List<Contribution> paid, Mobile except = null)
    {
        // Every member's share is booked aside except the buyer's, which stays in the buyer's errand and is credited
        // there as made; a levy nobody's errand pays for, a claim's, passes nobody. See BotYield.Aside.
        var got = Take(guild, want, paid, except);

        Levied += got;

        return got;
    }

    /// <summary>
    /// The crown's tax: the same collection, counted separately and never refunded.
    ///
    /// <para>
    /// <b>Where the watcher's prize money comes from, and it mints nothing.</b> A treasury that is topped up
    /// out of nowhere is a faucet, and this shard has paid for one of those before — 110,900gp into the world
    /// in a night through shopkeepers' shelves. A tax moves coin from the guilds to the crown and the crown
    /// hands it back as prizes, so the same money goes round rather than more of it arriving.
    /// </para>
    /// </summary>
    public static int Tax(Guild guild, int want)
    {
        var got = Take(guild, want, null, null);

        Taxed += got;

        return got;
    }

    /// <summary>Coin the crown has taken in tax. See <see cref="Tax"/>.</summary>
    public static long Taxed { get; private set; }

    /// <summary>Members who have paid the crown's tax. Its own bucket: the levy has one already.</summary>
    public static long Taxpayers { get; private set; }

    /// <summary>The collection itself: richest first, each keeping <see cref="Keep"/> back.</summary>
    private static int Take(Guild guild, int want, List<Contribution> paid, Mobile except)
    {
        if (guild?.Members == null || want <= 0)
        {
            return 0;
        }

        var members = new List<BotMobile>();

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false } member)
            {
                members.Add(member);
            }
        }

        members.Sort((a, b) => BotYield.Wealth(b).CompareTo(BotYield.Wealth(a)));

        var got = 0;

        // <b>The guild's chest pays first.</b> What the guild's ground earned in tithes goes to the guild's next claim
        // or hall before any member's pack is asked. See BotChest: Patrick's fourth Quad idea of 16.09.2026, the
        // money half. <b>A levy only, never the crown's tax</b> (paid is null for the tax): the tax is on what members
        // can spare, and from 29.09.2026 the chest is where a guild saves its members' dues for a hall — six per cent
        // of it every quarter of an hour would be a fifth of a guild's savings an hour gone to the crown. See BotDues.
        var chest = 0;

        if (paid != null)
        {
            chest = BotChest.Draw(guild.Name, want);
            got += chest;
        }

        for (var i = 0; i < members.Count && got < want; i++)
        {
            var member = members[i];
            var pack = member.Backpack;

            if (pack == null)
            {
                continue;
            }

            var spare = Math.Min(BotYield.Spendable(member) - Keep, want - got);

            if (spare <= 0)
            {
                continue;
            }

            var carried = pack.GetAmount(typeof(Gold));
            var fromPack = Math.Min(carried, spare);
            var fromBank = spare - fromPack;

            if (fromPack > 0 && !pack.ConsumeTotal(typeof(Gold), fromPack))
            {
                continue;
            }

            if (fromBank > 0 && !Banker.Withdraw(member, fromBank))
            {
                // Put back what was already taken from this one before moving on. The rest of the levy
                // stands: this member simply could not pay what its account claimed.
                if (fromPack > 0)
                {
                    pack.DropItem(new Gold(fromPack));
                }

                continue;
            }

            got += spare;
            paid?.Add(new Contribution(member, spare));

            if (!ReferenceEquals(member, except))
            {
                BotYield.Aside(member, spare);
            }

            // Counted under whichever collection this is. One bucket for two collectors would report "0gp
            // levied off 5 members" the first time the crown taxed anybody — which it did, within five
            // minutes of the tax existing. See a-new-gate-needs-a-new-bucket.
            if (paid == null)
            {
                Taxpayers++;
            }
            else
            {
                Payers++;
            }
        }

        // <b>A levy that falls short puts the chest's share back.</b> Every caller treats a short levy as no purchase and
        // hands the members' shares back (Refund), which knows nothing of the chest: until 29.09.2026 the chest's share of
        // a short levy was simply gone. With the chest holding a guild's dues for a hall — most of the price, by the time a
        // steward walks to the plot and finds the members have spent their part — that is a guild's day of saving lost to
        // a walk. Put back here, where the draw is known.
        if (chest > 0 && got < want)
        {
            BotChest.Return(guild.Name, chest);
            got -= chest;
        }

        return got;
    }

    /// <summary>
    /// Hands a levy back, into the account rather than the pack: a thousand coins in a backpack is twenty
    /// stones of carrying weight, and this is money the bot was not expecting to hold.
    /// </summary>
    public static void Refund(List<Contribution> paid, Mobile except = null)
    {
        if (paid == null)
        {
            return;
        }

        for (var i = 0; i < paid.Count; i++)
        {
            var back = paid[i];

            if (back.Who is { Deleted: false } && back.Paid > 0)
            {
                Banker.Deposit(back.Who, back.Paid);
                Levied -= back.Paid;
                Payers--;

                // Undone exactly as it was booked by Take: pass the same buyer. See BotYield.Aside.
                if (!ReferenceEquals(back.Who, except))
                {
                    BotYield.Aside(back.Who, -back.Paid);
                }
            }
        }
    }

    /// <summary>A hall has been raised. Records it and counts it.</summary>
    public static void Register(Guild guild, BaseHouse house, bool bought)
    {
        if (guild == null || house is not { Deleted: false })
        {
            return;
        }

        _halls[guild.Name] = house;

        // The square it stands in is the guild's own. See BotClaim.Seat.
        BotClaim.Seat(guild.Name, house.Map, house.Location);

        if (bought)
        {
            Raised++;
        }
        else
        {
            Adopted++;
        }
    }

    /// <summary>Counts a purchase that got as far as the ground and no further.</summary>
    public static void Lost() => Failed++;

    /// <summary>Counts fittings put into a hall.</summary>
    public static void Fit(int many) => Fitted += Math.Max(0, many);

    /// <summary>How many merchants stand in this hall.</summary>
    public static int Merchants(BaseHouse hall)
    {
        var many = 0;

        if (hall?.PlayerVendors == null)
        {
            return 0;
        }

        for (var i = 0; i < hall.PlayerVendors.Count; i++)
        {
            if (hall.PlayerVendors[i] is { Deleted: false })
            {
                many++;
            }
        }

        return many;
    }

    /// <summary>One has been hired. Counted, and said in the log because it is a permanent addition.</summary>
    public static void Hired(BaseHouse hall, Mobile merchant, int paid)
    {
        Merchanted++;
        Contracts += paid;

        logger.Information(
            "A merchant now stands in {Guild} at {X},{Y}; the contract cost {Paid}gp",
            hall?.Sign?.Name ?? "a hall",
            merchant?.X ?? 0,
            merchant?.Y ?? 0,
            paid
        );
    }

    /// <summary>
    /// Reads the world back: every house whose sign carries a guild's name is that guild's hall again.
    ///
    /// <para>
    /// Three things are done to each one, and each is a fact about this engine rather than a preference.
    /// <b>The owner is set to whoever leads the guild now</b>, because the mobile that bought it was deleted
    /// with the last population and a house with no owner is condemned (<c>BaseHouse.DecayType</c>).
    /// <b>Decay is refreshed</b>, because a house whose owner has no account is on manual refresh in this
    /// era and nothing else would ever refresh it. <b>The doors are unlocked</b>, because a locked door in
    /// pre-AOS wants a key in the pack (<c>BaseDoor.Use</c>) and this population's keys died with it.
    /// </para>
    /// </summary>
    public static void Adopt()
    {
        if (!Running)
        {
            return;
        }

        var houses = BaseHouse.AllHouses;

        for (var i = 0; i < houses.Count; i++)
        {
            var house = houses[i];

            if (house is not { Deleted: false })
            {
                continue;
            }

            var named = house.Sign?.Name;

            if (string.IsNullOrEmpty(named))
            {
                continue;
            }

            var guild = BotGuilds.Find(named);

            if (guild == null || _halls.ContainsKey(guild.Name))
            {
                continue;
            }

            Take(guild, house);

            // A hall furnished by an older version of the fittings may have its chest on the porch, in the
            // doorway everybody walks through. Repaired here rather than left for somebody to notice.
            var moved = BotFittings.Tidy(house);

            if (moved > 0)
            {
                logger.Information(
                    "{Moved} things standing outside the hall of {Guild} were moved into the room",
                    moved,
                    guild.Name
                );
            }

            Register(guild, house, false);
        }

        HallsAdopted = true;
    }

    /// <summary>
    /// Whether the halls standing in the world have been found again since the boot. The claims are read back before the
    /// halls, and a guild whose hall has not been adopted yet would otherwise lose every square it holds. See
    /// <see cref="BotClaim.Connect"/>.
    /// </summary>
    public static bool HallsAdopted { get; private set; }

    /// <summary>Halls razed because the square they stood in was taken.</summary>
    public static long RazedByLoss { get; private set; }

    /// <summary>
    /// Takes one guild's hall off the island: its merchant and everything in it go with it. Patrick's order of 26.09.2026:
    /// the square with the hall lost, the hall is razed, and the guild may raise it again on another square — which its
    /// steward does as for any guild with no hall, from its seat, on ground no other guild holds. A move it owed after a
    /// war is forgiven: there is nothing left to move.
    /// </summary>
    public static bool RazeHall(string guild, string why)
    {
        if (guild == null || !_halls.TryGetValue(guild, out var house) || house is not { Deleted: false })
        {
            return false;
        }

        var where = house.Location;
        var merchant = BotShelf.Of(house);

        if (merchant is { Deleted: false })
        {
            merchant.House = null;
            merchant.Delete();
        }

        house.Delete();
        _halls.Remove(guild);
        BotPlot.Spend();
        RazedByLoss++;

        if (BaseGuild.FindByName(guild) is Guild lost)
        {
            BotExile.Paid(lost);
        }

        logger.Warning("The hall of {Guild} at {X},{Y} is razed: {Why}", guild, where.X, where.Y, why);

        return true;
    }

    /// <summary>
    /// Makes a house the guild's: owner, co-owners, open doors, and the sign that says whose it is.
    ///
    /// <para>
    /// Co-owners rather than friends, because co-ownership is what the engine's own lockdown check asks
    /// about (<c>BaseHouse.LockDown</c> begins with <c>IsCoOwner</c>). The list is rebuilt from the guild
    /// every time this is called, so a member enrolled after the hall went up is not a stranger to it.
    /// </para>
    /// </summary>
    public static void Take(Guild guild, BaseHouse house)
    {
        if (guild == null || house is not { Deleted: false })
        {
            return;
        }

        if (guild.Leader is BotMobile { Deleted: false } leader)
        {
            house.Owner = leader;
        }

        house.CoOwners ??= new List<Mobile>();
        house.CoOwners.Clear();

        if (guild.Members != null)
        {
            for (var i = 0; i < guild.Members.Count; i++)
            {
                if (guild.Members[i] is BotMobile { Deleted: false } member && member != house.Owner)
                {
                    house.CoOwners.Add(member);
                }
            }
        }

        // Anybody may walk in. The population is the only thing on this island with legs, and a hall nobody
        // can enter is a very expensive wall.
        house.Public = true;

        if (house.Sign != null)
        {
            house.Sign.Name = guild.Name;
        }

        Unlock(house);
        house.RefreshDecay();

        // <b>And whatever stands inside it working.</b> A merchant is owned by the one bot that set it up,
        // and that bot was deleted with the last population — the same problem the hall itself has, with the
        // same cure. Whether the engine lets one survive a restart at all is a thing to watch rather than to
        // assume: <c>PlayerVendor</c>'s own deserialisation dismisses a vendor whose owner has gone.
        // And out of the doorway, where every merchant hired before 21.09.2026 was set down. See BotFittings.Unblock.
        var cleared = BotFittings.Unblock(house);

        if (cleared > 0)
        {
            logger.Information(
                "{Count} merchant in the hall of {Guild} stood in its doorway and was moved to the far end of the room",
                cleared,
                guild.Name
            );
        }

        for (var i = 0; i < house.PlayerVendors.Count; i++)
        {
            if (house.PlayerVendors[i] is { Deleted: false } merchant && guild.Leader is BotMobile keeper)
            {
                merchant.Owner = keeper;
            }
        }
    }

    /// <summary>
    /// Opens the locks on a house's doors, permanently.
    ///
    /// <para>
    /// <b>This is the whole reason bots can use a hall at all.</b> In this era <c>BaseHouseDoor.UseLocks</c>
    /// is true and <c>BaseDoor.Use</c> demands the key be in the pack; a bot without one is told "that is
    /// locked" and stands outside for ever. Keys could be handed out — and would be lost at the next restart
    /// with the packs that held them. An unlocked door survives that, and a shard whose only inhabitants are
    /// this population loses nothing by it.
    /// </para>
    /// </summary>
    public static void Unlock(BaseHouse house)
    {
        if (house?.Doors == null)
        {
            return;
        }

        for (var i = 0; i < house.Doors.Count; i++)
        {
            if (house.Doors[i] is BaseDoor door)
            {
                door.Locked = false;
            }
        }
    }

    /// <summary>
    /// Takes every hall off the island.
    ///
    /// <para>
    /// A house is the first thing this project has built that outlives the experiment that made it, so the
    /// experiment has to be undoable in one word — <c>do raze</c> at Argus. Without it the island silently
    /// accumulates the debris of every evening's work.
    /// </para>
    /// </summary>
    public static int Raze()
    {
        var gone = 0;

        foreach (var house in new List<BaseHouse>(_halls.Values))
        {
            if (house is { Deleted: false })
            {
                house.Delete();
                gone++;
            }
        }

        _halls.Clear();
        Razed += gone;

        return gone;
    }

    /// <summary>One line for the shard's own summary.</summary>
    public static string Describe()
    {
        if (!Running)
        {
            return "the population buys no halls";
        }

        var say = Server.Text.ValueStringBuilder.Create(384);
        var wanting = Server.Text.ValueStringBuilder.Create(256);

        try
        {
            var standing = 0;

            foreach (var (name, house) in _halls)
            {
                if (house is not { Deleted: false })
                {
                    continue;
                }

                if (standing++ > 0)
                {
                    say.Append(", ");
                }

                say.Append(name);
                say.Append(" at ");
                say.Append(house.X.ToString());
                say.Append(',');
                say.Append(house.Y.ToString());
            }

            // What every guild without a hall has, and how far short of the price it is. This is the line
            // that has to exist: a shard where nothing ever happens must say which of the two numbers is
            // wrong, rather than reporting a zero that could mean anything at all.
            var many = 0;

            foreach (var guild in BotGuilds.Standing)
            {
                if (Hall(guild) != null)
                {
                    continue;
                }

                var fund = Fund(guild);

                if (many++ > 0)
                {
                    wanting.Append(", ");
                }

                wanting.Append(guild.Name);
                wanting.Append(" has ");
                wanting.Append(fund.ToString());
                wanting.Append(" of ");
                wanting.Append(Price.ToString());
                if (fund < Price)
                {
                    wanting.Append(" and is ");
                    wanting.Append((Price - fund).ToString());
                    wanting.Append(" short");
                }
                else
                {
                    wanting.Append(" and can pay");
                }
            }

            var halls = standing == 0 ? "no halls stand" : $"{standing} halls stand: {say.ToString()}";

            var ledger =
                $"{Raised} raised and {Adopted} taken back from the save, {Failed} purchases lost, {Fitted} fittings set up, {Levied}gp levied off {Payers} members and {Taxed}gp taken off {Taxpayers} in tax";

            var owed = wanting.Length == 0 ? "every guild has one" : wanting.ToString();

            return
                $"{halls}; {ledger}; {owed}; {BotSteward.Describe()}; {BotFitter.Describe()}; {BotHirer.Describe()}; {Merchanted} merchants hired for {Contracts}gp; {BotSupplier.Describe()}; {BotOffice.Describe()}; {BotShelf.Describe()}; {BotLand.Describe()}; {BotRegard.Describe()}; {BotBailiff.Describe()}; {BotFeuder.Describe()}; {BotExile.Describe()}; {BotRemover.Describe()}; {BotHolder.Describe()}; {BotReeve.Describe()}; {BotChest.Describe()}; {BotToll.Describe()}; {BotTollman.Describe()}; {BotEnlarger.Describe()}; {BotAbode.Describe()}; {BotAbodeBuyer.Describe()}; {BotReposer.Describe()}; {BotFurnish.Describe()}; {BotOutposter.Describe()}; {Barred} times a workshop was passed over as somebody else's; {BotPlot.Describe()}";
        }
        finally
        {
            wanting.Dispose();
            say.Dispose();
        }
    }

    /// <summary>
    /// A world reload is a different population, and the halls are about to be re-adopted by it.
    ///
    /// The houses themselves are left standing: they are world objects, older than any bot in them, and
    /// deleting them here would throw away the only thing this subsystem exists to keep.
    /// </summary>
    public static void Forget()
    {
        _halls.Clear();
        Raised = 0;
        Adopted = 0;
        Levied = 0;
        Payers = 0;
        Failed = 0;
        Fitted = 0;
        Razed = 0;
        Barred = 0;
        Taxed = 0;
        Taxpayers = 0;
        Merchanted = 0;
        Contracts = 0;
    }
}
