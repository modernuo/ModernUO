using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Going through something this bot killed without meaning to.
///
/// <para>
/// <b>It exists because self-defence became a reflex and left bodies nobody owned.</b> Until then the only
/// way a creature died was that some bot had taken on a fight, and the last leg of that undertaking is
/// emptying the corpse — so every kill had somebody whose errand ended over the body. Making a bot hit back
/// in its damage hook fixed a mage standing still for twenty seconds and quietly created this: a bot now
/// kills whatever walks up to it while doing something else entirely, and what it leaves has no errand
/// attached. A sage killed a harpy and walked away from it, which is what sent anybody looking.
/// </para>
///
/// <para>
/// <b>Only its own kills, and only what the engine agrees it may take.</b> A corpse names its killer, so
/// there is no judgement here about whose it is — the engine already knows, and going through somebody
/// else's is a criminal act it will refuse anyway. This is not a scavenger that follows the population
/// around picking up after it; it is the missing end of a fight the bot did not choose.
/// </para>
///
/// <para>
/// It reuses <c>BotSlay.Rifle</c> and <c>BotSlay.Skin</c> rather than repeating them. What goes in a pack,
/// what stays on the corpse when the pack is full, what is listed and at what price — all of that is one
/// rule, and a second copy of it would disagree with the first inside a week.
/// </para>
/// </summary>
public sealed class BotPickings : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPickings));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "pickings";

    /// <summary>
    /// What going through a body is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// High, and it costs the shard nothing to be: the walk is a few tiles and the work is one gesture, so a
    /// minute of it is several corpses. It has to outrank walking about — the alternative is a bot strolling
    /// past the thing it just killed — and it cannot outrank a fight, which is priced in the hundreds. What
    /// it actually pays is measured like everything else now; see <c>BotCommons.Corrected</c>.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 90.0;

    public static double WorkMinutes { get; set; } = 0.5;

    /// <summary>How far off a body a bot will notice it. The hunt's own reach, so the two agree.</summary>
    public static int Reach => BotQuarry.LootReach;

    /// <summary>How far a bot will walk to one it has left behind.</summary>
    public static int Range { get; set; } = 30;

    /// <summary>
    /// Bodies walked to that turned out to hold nothing the bot would carry.
    ///
    /// Counted apart from the ones that paid, because "the corpse was empty" and "the corpse held three
    /// things worth less than the walk" are different facts about the island and were the same silence.
    /// </summary>
    public static long Barren { get; private set; }

    /// <summary>The deed's own tally back to nothing, called by the proposer's Forget with the rest.</summary>
    public static void Forget() => Barren = 0;

    private readonly Map _map;

    private readonly Corpse _corpse;

    private int _taken;

    private int _coins;

    private int _hides;

    public BotPickings(Map map, Corpse corpse)
    {
        _map = map;
        _corpse = corpse;
    }

    public override string Kind => Trade;

    /// <summary>Goes where the fighting was, on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    public override Map Map => _map;

    public override Point3D Where => _corpse?.GetWorldLocation() ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Bending over a body teaches a bot no more than bending over anything else.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    public override double Coin => 1.0;

    /// <summary>
    /// The road was refused, so the place is written down — and this is the half that was missing.
    ///
    /// <para>
    /// <b>Asking the reach ledger was not enough and the log said so within the hour.</b>
    /// <c>BotReach.Ask</c> answers out of pockets that enclosure searches have proved closed: it is about
    /// the ground a bot is <em>standing in</em>. A corpse fifteen tiles below the floor is not a pocket
    /// anybody has surveyed, so the answer is Unknown and the offer goes through. Corwin took this errand
    /// 533 times in five minutes on 10.09.2026 at one such corpse and took the whole shard's band to 33%,
    /// an hour after the reach guard went in and four hours after the identical loop on a spent arrow.
    /// </para>
    ///
    /// <para>
    /// So the failure marks the place, the way <c>BotUnload.Bend</c> has always marked a counter it could
    /// not reach, and the proposer reads the mark. Both halves are needed: a mark nobody reads is this
    /// project's oldest and most repeated defect, and the appraisal's own caution factor is not a substitute
    /// — a fifth root turns 0.15 into 0.68, which does not stop anything.
    /// </para>
    ///
    /// <para>
    /// False, always: there is no second place to try. The errand ends and the mark stops it coming back.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        var where = _corpse?.GetWorldLocation() ?? Point3D.Zero;

        if (where != Point3D.Zero)
        {
            bot?.Resolve?.Ledger?.Beware(Trade, _map, where);
        }

        return false;
    }

    public override int Made => _made;

    private int _made;

    public override string Stage =>
        _taken > 0 || _coins > 0
            ? $"took {_taken} things and {_coins}gp off what it killed"
            : $"going through {_corpse?.Owner?.Name ?? "what it killed"}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        // Somebody else got there, or it decayed. Finished rather than failed: nothing about the ground was
        // proved bad and the bot has lost nothing but a few steps.
        if (_corpse is not { Deleted: false })
        {
            return BotDoing.Done("it was gone by the time it got there");
        }

        var where = _corpse.GetWorldLocation();

        if (!body.InRange(where, Reach))
        {
            return BotDoing.Walk(_map, where, BotArrival.Within(Reach), "back to what it killed");
        }

        // Before anything is lifted, so the hide travels home with the rest instead of needing its own trip.
        // Exactly the ordering the hunt uses, and for the same reason.
        _hides = BotSlay.Skin(body, _corpse);

        var (taken, coins, made) = BotSlay.Rifle(bot, body, _corpse);

        _taken = taken;
        _coins = coins;
        _made = made;

        // <b>Plunder off a bot of a guild at war with ours counts towards the war.</b> The coin as coin and
        // the goods at what they were listed for, which is the shard's own reckoning of what they are worth.
        // See BotWar.Loot: ten thousand gold's worth wins a war as surely as fifty dead.
        if (_corpse.Owner is BotMobile { Guild: Guilds.Guild theirs } && body.Guild is Guilds.Guild ours &&
            ours != theirs)
        {
            BotWar.Looted(ours.Name, theirs.Name, coins + made);
        }

        if (taken == 0 && coins == 0 && _hides == 0)
        {
            // <b>Been through it, and that has to be written down even when nothing came out.</b> The
            // engine's Looters list is only touched when something is actually lifted, so a corpse holding
            // three things the bot will not carry is a corpse it has never been to as far as any record
            // goes — offered again on the next beat, and the next, for as long as it lies there. 69,868 of
            // these in eight hours on 27.08.2026. Looters means "who has been in this corpse", which is
            // exactly and only what is being claimed here.
            Barren++;

            _corpse.Looters?.Add(body);

            return BotDoing.Done("there was nothing on it");
        }

        logger.Information(
            "{Name} went back for what it killed and took {Things} things, {Coins}gp and {Hides} leather",
            body.Name,
            taken,
            coins,
            _hides
        );

        return BotDoing.Done($"{taken} things, {coins}gp and {_hides} leather off {_corpse.Owner?.Name ?? "it"}");
    }
}

/// <summary>
/// Offers a bot the body of something it killed and has not been through.
///
/// <para>
/// Refuses far more often than it offers and every refusal is named, for the reason the patrol's proposer
/// states at length: an unnamed nought is the failure this shard has paid for more than any other.
/// </para>
/// </summary>
public sealed class BotPicker : IBotProposer
{
    public static long Asked { get; private set; }

    /// <summary>Answers that went to a class which never goes through a corpse at all.</summary>
    public static long Sworn { get; private set; }

    /// <summary>Bots with nothing of theirs lying about. Nearly every answer is this.</summary>
    public static long NothingDead { get; private set; }

    /// <summary>Bodies already gone through, by this bot or by anybody.</summary>
    public static long Picked { get; private set; }

    public static long Offered { get; private set; }

    /// <summary>Corpses passed over because the ground they lie on has already been proved unreachable.</summary>
    public static long Sealed { get; private set; }

    /// <summary>Corpses passed over because this bot's own road to them was refused before. See BotPickings.Bend.</summary>
    public static long Baulked { get; private set; }

    public string Name => "Picker";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        // A class that does not scavenge never stoops, whatever is lying about. See BotClass.Scavenges: for a
        // company whose whole duty is to walk somewhere dangerous together, staying in formation is worth more
        // than what is in the corpse. Counted before Asked so "nobody picks things up" and "this class never
        // does" stay different numbers.
        if (body is BotMobile { Class.Scavenges: false })
        {
            Sworn++;

            return null;
        }

        Asked++;

        Corpse found = null;

        foreach (var item in map.GetItemsInRange(body.Location, BotPickings.Range))
        {
            if (item is not Corpse corpse || corpse.Deleted)
            {
                continue;
            }

            // The engine names the killer, so whose it is needs no judgement of ours — and going through
            // somebody else's is a criminal act the engine would refuse in any case.
            if (corpse.Killer != body)
            {
                continue;
            }

            // <b>Nothing left to come for, which is a different question from who has been here.</b> The
            // first version of this asked only whose kill it was, and a corpse the bot's own hunt had already
            // emptied still answered yes — so it was offered again, and again, five times in two minutes,
            // every one of them finishing "there was nothing on it". Looters would have caught it if anything
            // had been lifted, but an empty corpse is never looted by anybody and so never recorded.
            //
            // <b>And "or a hide still on it" was the second version of the same mistake.</b> Carved reads
            // false on anything that never had a hide — every skeleton, wraith and spectre on the island —
            // so a test of "empty and already carved" was a test that undead could never pass, and 106 of
            // 107 trips still found nothing. A flag that cannot become true is not a gate.
            //
            // Asked of the body alone: is there anything on it. The hide is carved by the hunt at the moment
            // of the kill, which is where it has always been done and where the blade is already out.
            if (corpse.Items.Count == 0)
            {
                Picked++;

                continue;
            }

            // Already been through it. Looters is the engine's own record: a bot that emptied a corpse and
            // had to leave the heavy half is not owed a second trip for it.
            if (corpse.Looters?.Contains(body) == true)
            {
                Picked++;

                continue;
            }

            found = corpse;

            break;
        }

        if (found == null)
        {
            NothingDead++;

            return null;
        }

        var lies = found.GetWorldLocation();

        // Two questions, because one of them is not enough. The reach ledger knows about ground that
        // enclosure searches have proved closed; this bot's own ledger knows about roads that were actually
        // refused, which is the only thing that knows about a corpse under the floor. See BotPickings.Bend.
        if (BotReach.Ask(map, body.Location, lies, BotArrival.Within(BotPickings.Reach))
            == BotReachVerdict.Sealed)
        {
            Sealed++;

            return null;
        }

        if (bot.Resolve?.Ledger?.Cautious(BotPickings.Trade, map, lies) == true)
        {
            Baulked++;

            return null;
        }

        Offered++;

        return new BotPickings(map, found);
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered anything they killed"
            : $"{Asked} looks for its own kills ({Sworn} answers went to classes that never stoop): {Offered} bodies offered, {Picked} already been through, {NothingDead} had nothing of theirs lying about; {BotPickings.Barren} held nothing worth carrying, {Sealed} lay where nothing can walk, {Baulked} had refused this bot a road before, and {BotGleaner.Sealed} spent arrows lay the same way ({BotGleaner.Baulked} of them already refused)";

    public static void Forget()
    {
        Asked = 0;
        Sworn = 0;
        NothingDead = 0;
        Picked = 0;
        Offered = 0;
        BotPickings.Forget();
    }
}
