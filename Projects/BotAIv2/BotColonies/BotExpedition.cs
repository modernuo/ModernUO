using System;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a guild's captain an expedition: the member holding the guild's ship, or its head when the guild has none and can
/// adopt or buy one; to found a colony on the first free island, or to take the ship and fresh hands back to the colony it has.
/// One guild weighed every <see cref="BotColonies.ConsiderMs"/>, one expedition at a time (<see cref="BotOffice"/>).
/// </summary>
public sealed class BotExpeditioner : IBotProposer
{
    public const string Office = "expedition";

    public string Name => Office;

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotColonies.Running || !BotSea.Running || !BotSea.Ready || bot?.Self is not BotMobile { Deleted: false, Alive: true } body)
        {
            return null;
        }

        if (body.Guild is not Guild guild || BotUnderworld.Band(guild) || body.Squad != null || body.Map != BotSeaChart.Map)
        {
            return null;
        }

        if (BotOffice.Busy(Office, guild) || BotColonies.Passage(guild, BotSailing.Way.Out) != null || BotColonies.OnLand(body) != null)
        {
            return null;
        }

        // Asked of the guild's clock before any pack is searched: a guild is weighed once every ConsiderMs.
        if (!BotColonies.Ripe(guild))
        {
            return null;
        }

        // One member of the guild is asked: whoever holds its ship, else its head.
        var holder = BotColonies.ShipHolder(guild, body);

        if (holder != null ? holder != body : !BotGuilds.IsHead(body))
        {
            return null;
        }

        if (!BotColonies.Due(guild))
        {
            return null;
        }

        var deed = Fit(body, guild, false, out _);

        if (deed == null)
        {
            return null;
        }

        BotOffice.Offering(Office, guild);
        BotColonies.Count("offered");

        return deed;
    }

    /// <summary>
    /// The expedition this captain can fit out now, or null with why. Founding when the guild has no colony; a relief when it has
    /// one and the ship is here on the mainland with the captain (the ship belongs at the colony: the run home is sailed from
    /// there). The money is asked here and paid later: the stores out of the chest when the ship puts to sea, a ship at the
    /// shipwright's counter.
    /// </summary>
    public static BotExpedition Fit(BotMobile captain, Guild guild, bool byHand, out string why)
    {
        why = null;

        if (captain == null || guild == null || captain.Map == null)
        {
            why = "no captain";

            return null;
        }

        if (BotTrading.Refuses(captain))
        {
            why = $"{captain.Name} is a trader, neutral, and leads no party";

            return null;
        }

        var colony = BotColonies.Of(guild.Name);
        var founding = colony == null;
        BotDock from;
        BotDock island;
        BotSeaLane lane;
        Point3D site;

        if (founding)
        {
            if (!BotColonies.Island(captain, out island, out site, out from, out lane))
            {
                BotColonies.Count("noisland");
                why = "no island named has a free site with rock and a lane from a dock the captain can walk to";

                return null;
            }
        }
        else
        {
            if (BotColonies.OnLand(captain) != null)
            {
                why = $"{captain.Name} is on the colony's island already";

                return null;
            }

            // A relief sails the ship back. With the ship elsewhere, or a colony manned and the ship already there, nothing to do.
            if (BotHelm.ShipOf(captain) == null && BotColonies.ShipHolder(guild) != null)
            {
                why = "the guild's ship is in another member's hands";

                return null;
            }

            if (!BotColonies.OutWay(captain, colony, out from, out island, out lane))
            {
                why = $"no lane from a dock {captain.Name} can walk to out to {colony.Island}";

                return null;
            }

            site = colony.Site;
        }

        var holds = BotHelm.ShipOf(captain) != null;
        var adopt = !holds && BotColonies.Salvage && BotColonies.Adoptable(captain);
        var price = 0;

        if (!holds && !adopt)
        {
            if (!BotColonies.BuyShip)
            {
                BotColonies.Count("noship");
                why = "no ship of its own, none at anchor to adopt, and buying is off";

                return null;
            }

            var vendor = BotShops.Nearest((Mobile)captain, typeof(SmallBoatDeed));

            price = vendor != null ? BotShops.Price(vendor, typeof(SmallBoatDeed)) : BotSea.ListPrice;

            if (price <= 0)
            {
                price = BotSea.ListPrice;
            }
        }

        var stores = founding ? BotColonies.Cost : BotColonies.RelieveCost;

        if (BotChest.Holds(guild.Name) < stores)
        {
            BotColonies.Count("poor");
            why = $"the chest holds {BotChest.Holds(guild.Name)}gp of the {stores}gp the stores cost";

            return null;
        }

        // A ship comes out of the captain's purse and what the guild can stand after the stores: the chest's spare and the members'.
        if (price > 0 && BotYield.Spendable(captain) + BotEstate.Fund(guild) - stores < price)
        {
            BotColonies.Count("poor");
            why = $"a ship costs {price}gp and the captain and the guild could find {BotYield.Spendable(captain) + BotEstate.Fund(guild) - stores}gp after the stores";

            return null;
        }

        var wanted = founding
            ? Math.Max(0, BotColonies.PartySize - 1)
            : Math.Max(0, Math.Min(BotColonies.PartySize - 1, BotColonies.PartySize - 1 - colony.Colonists));
        var least = founding ? Math.Max(0, BotColonies.LeastParty - 1) : 0;

        return new BotExpedition(guild, captain, colony, founding, from, island, lane, site, stores, price > 0, adopt, wanted, least, byHand);
    }
}

/// <summary>
/// The captain's expedition: the ship in the pack (held, adopted at anchor, or bought at the shipwright), the walk to the dock,
/// the muster of the party at its side, the stores paid out of the chest as the ship puts to sea, the voyage with the party
/// aboard as the helm's company, and on the island the colony set down and its square claimed — or, for a relief, the ship
/// and its hands brought back to the colony.
/// </summary>
public sealed class BotExpedition : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotExpedition));

    public const string Trade = "expedition";

    public const string ReliefTrade = "relief";

    /// <summary>What an expedition is reckoned at a minute: high, because it is the guild's errand and pays nothing to the captain.</summary>
    public static double Prior { get; set; } = 150.0;

    private enum Step
    {
        Ship,
        Buy,
        ToDock,
        Muster,
        Sail,
        Ashore
    }

    private readonly Guild _guild;

    private readonly BotMobile _captain;

    private readonly bool _founding;

    private readonly BotDock _from;

    private readonly BotDock _to;

    private readonly BotSeaLane _lane;

    private readonly Point3D _site;

    private readonly int _stores;

    private readonly bool _buy;

    private readonly bool _adopt;

    private readonly bool _byHand;

    private readonly BotSailing _passage;

    private readonly double _minutes;

    private BotColony _colony;

    private Step _step;

    private BaseVendor _vendor;

    private BotVoyage _voyage;

    private bool _paid;

    private bool _said;

    public BotExpedition(
        Guild guild, BotMobile captain, BotColony colony, bool founding, BotDock from, BotDock to, BotSeaLane lane, Point3D site, int stores,
        bool buy, bool adopt, int wanted, int least, bool byHand
    )
    {
        _guild = guild;
        _captain = captain;
        _colony = colony;
        _founding = founding;
        _from = from;
        _to = to;
        _lane = lane;
        _site = site;
        _stores = stores;
        _buy = buy;
        _adopt = adopt;
        _byHand = byHand;

        _passage = new BotSailing
        {
            Guild = guild,
            Captain = captain,
            Heading = BotSailing.Way.Out,
            Founding = founding,
            Map = captain?.Map,
            From = from,
            To = to,
            Goal = site,
            Colony = colony,
            Wanted = wanted,
            Least = least,
            State = BotSailing.Tide.Mustering,
            Began = Core.TickCount
        };

        var sail = (lane?.Tiles ?? 0) * (double)BotHelm.StrokeMs / Math.Max(1, BotHelm.Speed) / 60000.0;

        _minutes = Math.Max(5.0, BotColonies.MusterMs / 60000.0 + sail + 3.0);
    }

    public BotSailing Passage => _passage;

    public override string Kind => _founding ? Trade : ReliefTrade;

    public override Map Map => _passage.Map;

    public override Point3D Where =>
        _step switch
        {
            Step.Buy when _vendor != null => _vendor.Location,
            Step.Sail when _voyage != null => _voyage.Where,
            Step.Ashore => _site,
            _ => _from?.First?.Shore ?? _from?.Shore ?? Point3D.Zero
        };

    public override double Expects => Prior;

    public override double Minutes => _minutes;

    public override double Coin => 0.0;

    /// <summary>The guild's errand: what it is worth is the colony, not the captain's purse.</summary>
    public override bool Unpaid => true;

    public override bool Steadfast => true;

    public override bool Resumes => true;

    public override double HoldsFor => _minutes;

    /// <summary>From the first step to the dock: the party is called to it, and a captain taken off for a bolt of cloth strands it.</summary>
    public override bool Committed => _step >= Step.ToDock;

    /// <summary>Waiting at the berth and at sea the labour clock is not asked: the muster's and the helm's clocks keep it.</summary>
    public override bool Still => _step == Step.Muster || _voyage is { Still: true };

    public override string Stage =>
        _step switch
        {
            Step.Ship => "finding a ship",
            Step.Buy => _vendor != null ? $"buying a ship from {_vendor.Name}" : "looking for a shipwright",
            Step.ToDock => $"walking to the dock at {_from?.Name} to muster {_guild?.Name}'s party for {_to?.Name}",
            Step.Muster => $"mustering at {_from?.Name}: {_passage.Present()} of {_passage.Wanted} at its side, {_passage.Roster.Count} signed",
            Step.Sail => _voyage?.Stage ?? $"at sea for {_to?.Name}",
            _ => _founding ? $"setting down {_guild?.Name}'s colony at ({_site.X}, {_site.Y})" : $"bringing the ship back to the colony at ({_site.X}, {_site.Y})"
        };

    /// <summary>For the door's answer.</summary>
    public string Describe() =>
        $"{(_founding ? "an expedition to found a colony on" : "a relief to")} {_to?.Name} at ({_site.X}, {_site.Y}) from {_from?.Name}, up to {_passage.Wanted} hands"
        + $"{(_buy ? ", a ship to be bought" : _adopt ? ", a ship to be adopted from anchor" : "")}, {_stores}gp of stores";

    public override void Taken(IBotWilful bot)
    {
        BotColonies.Open(_passage);

        logger.Information(
            "{Name} of {Guild} takes on {What}{ByHand}",
            _captain?.Name,
            _guild?.Name,
            Describe(),
            _byHand ? " (ordered by hand)" : ""
        );
    }

    public override void Paused(IBotWilful bot) => _voyage?.Paused(bot);

    public override void Resumed(IBotWilful bot) => _voyage?.Resumed(bot);

    public override void Drop(IBotWilful bot)
    {
        BotOffice.Release(BotExpeditioner.Office, _guild);

        if (_passage.State != BotSailing.Tide.Over)
        {
            BotColonies.Close(_passage, _passage.State == BotSailing.Tide.Landed ? "landed" : "the captain put it down");
        }
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile { Deleted: false, Alive: true } body || _guild == null || _guild.Disbanded || body.Guild != _guild)
        {
            return BotDoing.Failed("no captain, or no guild");
        }

        if (_from == null || _to == null || _lane == null)
        {
            return BotDoing.Failed("nowhere to sail");
        }

        BotOffice.Hold(BotExpeditioner.Office, _guild, 180000);

        for (var guard = 0; guard < 8; guard++)
        {
            var doing = _step switch
            {
                Step.Ship => Ship(body),
                Step.Buy => Buy(bot, body),
                Step.ToDock => ToDock(body),
                Step.Muster => Muster(bot, body),
                Step.Sail => Sail(bot, body),
                _ => Ashore(body)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    private BotDoing Ship(BotMobile body)
    {
        if (BotHelm.ShipOf(body) != null)
        {
            _step = Step.ToDock;

            return default;
        }

        if (_adopt && BotColonies.Adopt(body, out var refused))
        {
            _step = Step.ToDock;

            return default;
        }

        if (_buy || _adopt && BotColonies.BuyShip)
        {
            _step = Step.Buy;

            return default;
        }

        return BotDoing.Failed("no ship of its own and none to adopt, and buying is off");
    }

    /// <summary>
    /// A small ship from the nearest shopkeeper selling one, paid from the captain's purse and, short of it, the guild's
    /// (<see cref="BotProvision.Fund"/>) — the shipwright's counter, as every ship on this shard is bought (<see cref="BotVoyage"/>).
    /// Booked aside: the ship sails every run after this one, and one expedition is not its cost.
    /// </summary>
    private BotDoing Buy(IBotWilful bot, BotMobile body)
    {
        var wanted = typeof(SmallBoatDeed);

        if (_vendor == null || _vendor.Deleted)
        {
            _vendor = BotShops.Nearest(bot, wanted);
        }

        if (_vendor == null)
        {
            return BotDoing.Failed("nobody within reach sells a ship");
        }

        if (!body.InRange(_vendor.Location, BotShops.CounterReach))
        {
            return BotDoing.Walk(body.Map, _vendor, BotArrival.Within(BotShops.CounterReach), $"to {_vendor.Name} for a ship");
        }

        var price = BotShops.Price(_vendor, wanted);

        if (price <= 0)
        {
            return BotDoing.Failed($"{_vendor.Name} has no ship to sell");
        }

        if (BotYield.Spendable(body) < price)
        {
            BotProvision.Fund(body, price);
        }

        if (BotYield.Spendable(body) < price)
        {
            BotColonies.Count("poor");

            return BotDoing.Failed($"{BotYield.Spendable(body)}gp free will not buy a ship at {price}gp, the guild standing what it could");
        }

        var bought = BotShops.Buy(bot, _vendor, wanted, 1, out var refused);

        if (bought <= 0 || BotHelm.ShipOf(body) == null)
        {
            return BotDoing.Failed($"no ship bought: {refused}");
        }

        BotYield.Aside(body, price);
        BotColonies.Bought(price);
        BotHelm.Keep(body, BotHelm.ShipOf(body));

        logger.Information(
            "{Name} of {Guild} bought a ship from {Vendor} for {Price}gp for its guild's {What}",
            body.Name,
            _guild.Name,
            _vendor.Name,
            price,
            _founding ? "expedition" : "relief"
        );

        _step = Step.ToDock;

        return default;
    }

    private BotDoing ToDock(BotMobile body)
    {
        if (BotHelm.ShipOf(body) == null)
        {
            return BotDoing.Failed("the ship is gone from its pack");
        }

        var berth = _from.First;

        if (berth == null)
        {
            return BotDoing.Failed($"{_from.Name} has no berth");
        }

        if (!_said)
        {
            _said = true;

            BotVoice.Say(
                body,
                "guild",
                _founding
                    ? $"Our ship sails from {_from.Name} for {_to.Name} to found a colony. I want {_passage.Wanted} hands at the dock, picks if you have them."
                    : $"The ship goes back to our colony on {_to.Name} from {_from.Name}. Whoever will relieve the hands there, to the dock.",
                true
            );
        }

        if (!body.InRange(berth.Shore, 2) || Math.Abs(body.Z - berth.Shore.Z) > 16)
        {
            return BotDoing.Walk(body.Map, berth.Shore, BotArrival.Within(1), $"to the dock at {_from.Name} to muster the party");
        }

        _passage.AtDock = Core.TickCount;
        _step = Step.Muster;

        return default;
    }

    private BotDoing Muster(IBotWilful bot, BotMobile body)
    {
        var berth = _from.First;

        if (!body.InRange(berth.Shore, 3))
        {
            return BotDoing.Walk(body.Map, berth.Shore, BotArrival.Within(1), $"back to the dock at {_from.Name}");
        }

        var present = _passage.Present();
        var waited = Core.TickCount - _passage.AtDock;

        if (present < _passage.Wanted && waited < BotColonies.MusterMs)
        {
            return BotDoing.Work($"mustering at {_from.Name}: {present} of {_passage.Wanted} at its side");
        }

        if (present < _passage.Least)
        {
            BotColonies.Count("unmustered");

            BotVoice.Say(body, "guild", $"Only {present} came to the dock. The {(_founding ? "expedition" : "relief")} to {_to.Name} is off.", true);

            return BotDoing.Failed($"only {present} of the party came to the dock at {_from.Name} in {waited / 60000.0:F1} minutes; {_passage.Least} wanted");
        }

        _passage.Aboard.Clear();

        for (var i = 0; i < _passage.Roster.Count; i++)
        {
            if (_passage.Roster[i] != body && _passage.AtHand(_passage.Roster[i]))
            {
                _passage.Aboard.Add(_passage.Roster[i]);
            }
        }

        var town = _to.Town;

        _voyage = new BotVoyage(body.Map, _from, _to, _lane, town, _site, $"{_guild.Name}'s {(_founding ? "expedition" : "relief")}", false, _passage.Aboard);
        _voyage.Taken(bot);
        _step = Step.Sail;

        logger.Information(
            "{Name} of {Guild} puts the ship in at {From} for {To} with {Party} of the party at its side ({Signed} signed, {Wanted} wanted): {Why}",
            body.Name,
            _guild.Name,
            _from.Name,
            _to.Name,
            _passage.Aboard.Count,
            _passage.Roster.Count,
            _passage.Wanted,
            _founding ? "to found a colony" : "to relieve the colony"
        );

        return default;
    }

    private BotDoing Sail(IBotWilful bot, BotMobile body)
    {
        var doing = _voyage.Advance(bot);

        // The stores are paid the moment the ship is in the water with the party aboard: out of the chest, into the city's purse.
        // Until then the muster stands, so a member who walked after the captain to another berth is still taken aboard.
        if (!_paid && BotHelm.Of(body) != null)
        {
            _paid = true;
            _passage.State = BotSailing.Tide.Sailing;
            BotColonies.Count("sailed");
            PayStores();
        }

        switch (doing.Kind)
        {
            case BotDoingKind.Done:
                {
                    _passage.State = BotSailing.Tide.Landed;
                    _step = Step.Ashore;
                    BotColonies.Count("landed");

                    return default;
                }
            case BotDoingKind.Failed:
                {
                    BotColonies.Count("lostatsea");

                    return BotDoing.Failed($"the {(_founding ? "expedition" : "relief")}'s voyage failed: {doing.Note}");
                }
            default:
                {
                    return doing;
                }
        }
    }

    private void PayStores()
    {
        if (_stores <= 0)
        {
            return;
        }

        var drawn = BotChest.Draw(_guild.Name, _stores);

        if (drawn <= 0)
        {
            return;
        }

        BotCity.Tax(drawn);
        BotColonies.PaidStores(drawn);

        logger.Information(
            "{Guild}'s chest paid {Gold}gp for the {What}'s stores, into the city's purse",
            _guild.Name,
            drawn,
            _founding ? "expedition" : "relief"
        );
    }

    private BotDoing Ashore(BotMobile body)
    {
        if (!_founding)
        {
            BotColonies.Count("relieved");

            BotVoice.Say(body, "guild", $"The ship is back at our colony on {_to.Name} with {_passage.Aboard.Count} fresh hands.", true);

            return BotDoing.Done($"brought the ship and {_passage.Aboard.Count} hands back to the colony on {_to.Name}");
        }

        if (BotColonies.Of(_guild.Name) is { } already)
        {
            return BotDoing.Done($"{_guild.Name} has a colony already, on {already.Island}");
        }

        if (!body.InRange(_site, 3))
        {
            return BotDoing.Walk(body.Map, _site, BotArrival.Within(2), $"to the site of {_guild.Name}'s colony");
        }

        _colony = BotColonies.Found(_guild, _to, body.Map, _site, body.Name);

        if (_colony == null)
        {
            return BotDoing.Failed("the colony could not be set down");
        }

        _passage.Colony = _colony;

        var claimed = BotColonies.Claim(_colony, _guild);

        BotVoice.Say(
            body,
            "local",
            claimed
                ? $"Here we stand. This square is {_guild.Name}'s from today: hold it with me while the claim is made."
                : $"Here we set our store down. The square will be claimed for {_guild.Name} as soon as the guild can.",
            true
        );

        return BotDoing.Done($"founded {_guild.Name}'s colony on {_to.Name} with {_passage.Aboard.Count} hands, {(claimed ? "its square claimed" : "its claim to come")}");
    }
}
