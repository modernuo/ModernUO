using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-estate.json</c> is allowed to say. Everything optional.
///
/// <para>
/// What is not here: where a house may stand. That is the engine's — <c>HousePlacement.Check</c> — and a
/// file able to disagree with it would be a file able to promise a guild a hall that cannot be built.
/// </para>
/// </summary>
public sealed class BotEstateSettings
{
    /// <summary>Whether the guilds buy halls at all.</summary>
    public bool? Running { get; set; }

    /// <summary>What a hall costs the guild that raises it.</summary>
    public int? Price { get; set; }

    /// <summary>What a member keeps back when the levy is called.</summary>
    public int? Keep { get; set; }

    /// <summary>Whether members pay dues into their guild's chest. See BotDues.Running.</summary>
    public bool? DuesRunning { get; set; }

    /// <summary>The share of the coin a member's work brings in that goes to its guild's chest. See BotDues.Share.</summary>
    public double? Dues { get; set; }

    /// <summary>A chest at or above this takes no dues. See BotDues.Ceiling.</summary>
    public int? DuesCeiling { get; set; }

    /// <summary>Whether a chest keeps what its guild saves for back from everyday draws. See BotDues.Saving.</summary>
    public bool? DuesSaving { get; set; }

    /// <summary>How many halls the island may hold.</summary>
    public int? MaxHalls { get; set; }

    /// <summary>Which house goes up, by multi id.</summary>
    public int? MultiID { get; set; }

    /// <summary>No nearer to the population's home than this.</summary>
    public int? PlotNear { get; set; }

    /// <summary>And no further.</summary>
    public int? PlotFar { get; set; }

    /// <summary>How far apart two halls must stand.</summary>
    public int? PlotApart { get; set; }

    /// <summary>How wide a belt round a graveyard, or any no-housing ground, is kept clear.</summary>
    public int? PlotClearance { get; set; }

    /// <summary>How many candidates one search may put through the engine's check.</summary>
    public int? PlotBudget { get; set; }

    /// <summary>How often anybody may go looking for ground, in milliseconds.</summary>
    public int? LookMs { get; set; }

    /// <summary>The share of the price at which a guild starts looking for somewhere to build.</summary>
    public double? LookAt { get; set; }

    /// <summary>What raising a hall is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>Whether a new hall is furnished with a chest and its guild's tools.</summary>
    public bool? Fittings { get; set; }

    /// <summary>Whether the population's own houses are furnished over time. See BotFurnish.Running.</summary>
    public bool? AbodeFurnish { get; set; }

    /// <summary>What a bed costs a house's owner, in gold. See BotFurnish.BedPrice.</summary>
    public int? FurnishBedPrice { get; set; }

    /// <summary>Hours before a house is looked at again. See BotFurnish.EveryHours.</summary>
    public double? FurnishEveryHours { get; set; }

    /// <summary>How many of a kind a guild counter holds before it stops being restocked.</summary>
    public int? ShelfDepth { get; set; }

    /// <summary>The most units in one lot put on a guild counter.</summary>
    public int? ShelfBatch { get; set; }

    /// <summary>About what one supply run may spend of the guild's money.</summary>
    public int? ShelfSpend { get; set; }

    /// <summary>What a guild adds to what it paid, as a multiplier. One is at cost.</summary>
    public double? ShelfMarkup { get; set; }

    /// <summary>What a guild tops its shopkeeper's purse up to, so the engine does not destroy it.</summary>
    public int? ShelfFloat { get; set; }

    /// <summary>Whether a hall claims the ground round it at all.</summary>
    public bool? Lands { get; set; }

    /// <summary>How far that claim reaches, in tiles.</summary>
    public int? LandReach { get; set; }

    /// <summary>What work on your own guild's ground is worth, as a multiplier.</summary>
    public double? LandHome { get; set; }

    /// <summary>And on somebody else's. Never nought: a claim that refuses is a claim that stops the shard.</summary>
    public double? LandAbroad { get; set; }

    /// <summary>Whether guilds form opinions of each other at all.</summary>
    public bool? Standing { get; set; }

    /// <summary>
    /// Whether a bad enough opinion may become a war. Off by Patrick's own reasoning: it is the only part of
    /// the plan that can make the population smaller, so it waits for somebody to be watching.
    /// </summary>
    public bool? War { get; set; }

    /// <summary>Below this regard, one guild declares war on another.</summary>
    public double? WarBelow { get; set; }

    /// <summary>And above this, the war ends.</summary>
    public double? PeaceAbove { get; set; }

    /// <summary>What five minutes of nothing happening is worth, towards nought, on every opinion.</summary>
    public double? Mend { get; set; }

    /// <summary>Enemies one side has to bring down to win a war.</summary>
    public int? WarKills { get; set; }

    /// <summary>Or gold's worth of plunder taken off the enemy's corpses.</summary>
    public int? WarLoot { get; set; }

    /// <summary>How long a war stands before a mended opinion may end it, in minutes.</summary>
    public int? WarLeastMinutes { get; set; }

    /// <summary>And how long one may run before it is judged on whatever blood there was, in minutes.</summary>
    public int? WarLongestMinutes { get; set; }

    /// <summary>How long two guilds that have just fought are kept from fighting again, in minutes.</summary>
    public int? TruceMinutes { get; set; }

    /// <summary>How often one guild may declare a war, in minutes.</summary>
    public int? DeclareEveryMinutes { get; set; }

    /// <summary>How many wars one guild may stand in at once.</summary>
    public int? MostWars { get; set; }

    /// <summary>How far a member will come to defend the guild's own ground, in tiles.</summary>
    public int? Defend { get; set; }

    /// <summary>Where each guild lives, by name: <c>[x, y]</c>. See <c>BotSeat</c>.</summary>
    public Dictionary<string, int[]> Seats { get; set; }

    /// <summary>How far a hall may stand from its seat and still be at home.</summary>
    public int? SeatSettled { get; set; }

    /// <summary>The share of newly founded guilds seated by another town. See BotSeat.AbroadShare.</summary>
    public double? SeatAbroadShare { get; set; }

    /// <summary>What a corpse shared between two guilds' members is worth to each opinion of the other.</summary>
    public double? Comradeship { get; set; }

    /// <summary>What a rescue across guilds is worth.</summary>
    public double? Aid { get; set; }

    /// <summary>At or above this, both ways, two guilds are allies.</summary>
    public double? Alliance { get; set; }

    /// <summary>And below this, either way, an alliance ends.</summary>
    public double? Estrangement { get; set; }

    /// <summary>How many allies one guild may have.</summary>
    public int? MostAllies { get; set; }
}

public static class BotEstateConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEstateConfig));

    private const string ConfigPath = "Configuration/bot-estate.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotEstateSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotEstateSettings());

            logger.Information(
                "Wrote a starter estate file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotEstate.Running = settings.Running ?? BotEstate.Running;
        BotEstate.Price = settings.Price ?? BotEstate.Price;
        BotEstate.Keep = settings.Keep ?? BotEstate.Keep;
        BotDues.Running = settings.DuesRunning ?? BotDues.Running;
        BotDues.Share = settings.Dues ?? BotDues.Share;
        BotDues.Ceiling = settings.DuesCeiling ?? BotDues.Ceiling;
        BotDues.Saving = settings.DuesSaving ?? BotDues.Saving;
        BotEstate.MaxHalls = settings.MaxHalls ?? BotEstate.MaxHalls;

        BotPlot.MultiID = settings.MultiID ?? BotPlot.MultiID;
        BotPlot.Near = settings.PlotNear ?? BotPlot.Near;
        BotPlot.Far = settings.PlotFar ?? BotPlot.Far;
        BotPlot.Apart = settings.PlotApart ?? BotPlot.Apart;
        BotPlot.Clearance = settings.PlotClearance ?? BotPlot.Clearance;
        BotPlot.Budget = settings.PlotBudget ?? BotPlot.Budget;

        BotSteward.LookMs = settings.LookMs ?? BotSteward.LookMs;
        BotSteward.LookAt = settings.LookAt ?? BotSteward.LookAt;
        BotHall.Prior = settings.Expects ?? BotHall.Prior;

        BotFittings.Running = settings.Fittings ?? BotFittings.Running;

        BotFurnish.Running = settings.AbodeFurnish ?? BotFurnish.Running;
        BotFurnish.BedPrice = settings.FurnishBedPrice ?? BotFurnish.BedPrice;
        BotFurnish.EveryHours = settings.FurnishEveryHours ?? BotFurnish.EveryHours;

        BotSupplier.Shelf = settings.ShelfDepth ?? BotSupplier.Shelf;
        BotSupplier.Batch = settings.ShelfBatch ?? BotSupplier.Batch;
        BotSupplier.Spend = settings.ShelfSpend ?? BotSupplier.Spend;
        BotShelf.Markup = settings.ShelfMarkup ?? BotShelf.Markup;
        BotShelf.Float = settings.ShelfFloat ?? BotShelf.Float;

        BotLand.Running = settings.Lands ?? BotLand.Running;
        BotLand.Reach = settings.LandReach ?? BotLand.Reach;
        BotLand.Home = settings.LandHome ?? BotLand.Home;
        BotLand.Abroad = settings.LandAbroad ?? BotLand.Abroad;

        BotRegard.Running = settings.Standing ?? BotRegard.Running;
        BotRegard.Warring = settings.War ?? BotRegard.Warring;
        BotRegard.Enmity = settings.WarBelow ?? BotRegard.Enmity;
        BotRegard.Amity = settings.PeaceAbove ?? BotRegard.Amity;
        BotRegard.Mend = settings.Mend ?? BotRegard.Mend;

        BotWar.Kills = settings.WarKills ?? BotWar.Kills;
        BotWar.Loot = settings.WarLoot ?? BotWar.Loot;
        BotWar.LeastMs = settings.WarLeastMinutes * 60000 ?? BotWar.LeastMs;
        BotWar.LongestMs = settings.WarLongestMinutes * 60000 ?? BotWar.LongestMs;
        BotWar.TruceMs = settings.TruceMinutes * 60000 ?? BotWar.TruceMs;
        BotWar.DeclareEveryMs = settings.DeclareEveryMinutes * 60000 ?? BotWar.DeclareEveryMs;
        BotWar.MostWars = settings.MostWars ?? BotWar.MostWars;
        BotFeud.Defend = settings.Defend ?? BotFeud.Defend;

        BotRegard.Comradeship = settings.Comradeship ?? BotRegard.Comradeship;
        BotRegard.Aid = settings.Aid ?? BotRegard.Aid;
        BotRegard.Alliance = settings.Alliance ?? BotRegard.Alliance;
        BotRegard.Estrangement = settings.Estrangement ?? BotRegard.Estrangement;
        BotRegard.MostAllies = settings.MostAllies ?? BotRegard.MostAllies;

        BotSeat.Configure(settings.Seats);
        BotSeat.Settled = settings.SeatSettled ?? BotSeat.Settled;
        BotSeat.AbroadShare = settings.SeatAbroadShare ?? BotSeat.AbroadShare;
    }
}
