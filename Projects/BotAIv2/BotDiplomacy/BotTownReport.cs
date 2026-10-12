using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// How a town stands, from the shard's own numbers: what its shops are out of, what the market wants, how dangerous the ground
/// round it reads, who lives there. What the town's voice tells a guild's envoy, and what the tasks are chosen from.
///
/// <para>
/// <b>Patrick's order of 29.09.2026, point 3: "the debugger is summoned, and feedback on the town's situation — economic,
/// hostile, trade — is taken from him."</b> Nothing here is the model's: every figure is read off a record the shard already
/// keeps — the shopkeepers <c>BotShops</c> knows inside the town's box, the island's shortages (<c>BotShops.Dry</c>) that none
/// of them sells now, the market's board (<c>BotMarket.Board</c>), the squares of <c>BotQuad</c> within
/// <see cref="Reach"/> of the town's hall, and the residents (<c>BotResidence</c>). The model is handed this and phrases it; the
/// rule says it plainly. Built once per audience, so a pass over a thousand shopkeepers and fifty squares costs nothing that
/// matters.
/// </para>
/// </summary>
public sealed class BotTownReport
{
    /// <summary>How far round the town's hall its ground is read, in tiles. Ninety: three squares each way.</summary>
    public static int Reach { get; set; } = 90;

    public string Town { get; init; }

    public int Shops { get; init; }

    public int Residents { get; init; }

    /// <summary>What the island is short of and no shop in this town has on its shelf now, commonest shortage first.</summary>
    public List<Type> Out { get; } = [];

    /// <summary>The squares within <see cref="Reach"/> reading unsafe or worse, how many of the population died in them, and the worst.</summary>
    public int Unsafe { get; init; }

    public int Dead { get; init; }

    public BotQuad.Quad Worst { get; init; }

    public double WorstReading { get; init; }

    public bool Quarantined { get; init; }

    /// <summary>The market's board in one phrase.</summary>
    public string Market { get; init; }

    /// <summary>The whole of it for the model.</summary>
    public string Facts { get; set; }

    /// <summary>The rule's report, one or two plain sentences.</summary>
    public string Said { get; set; }

    public static BotTownReport Of(BotTowns.Town town, Point3D hall, Guild guild)
    {
        var map = BotPopulation.Home;
        var shops = 0;
        List<BaseVendor> here = [];
        var known = BotShops.Shops;

        for (var i = 0; i < known.Count; i++)
        {
            var vendor = known[i];

            if (vendor is { Deleted: false } && vendor.Map == map && town.Bounds.Contains(vendor.Location))
            {
                shops++;
                here.Add(vendor);
            }
        }

        var unsafeCount = 0;
        var dead = 0;
        BotQuad.Quad worst = null;
        var worstReading = BotQuad.Safest;

        for (var qx = (hall.X - Reach) / BotQuad.Side; qx <= (hall.X + Reach) / BotQuad.Side; qx++)
        {
            for (var qy = (hall.Y - Reach) / BotQuad.Side; qy <= (hall.Y + Reach) / BotQuad.Side; qy++)
            {
                var at = new Point3D(qx * BotQuad.Side + BotQuad.Side / 2, qy * BotQuad.Side + BotQuad.Side / 2, 0);
                var quad = BotQuad.Known(map, at);

                if (quad == null || quad.Deep || quad.Townbound)
                {
                    continue;
                }

                var reading = BotQuad.Reading(quad);

                if (reading > BotQuad.Unsafe)
                {
                    continue;
                }

                unsafeCount++;
                dead += quad.Deaths;

                if (reading < worstReading)
                {
                    worstReading = reading;
                    worst = quad;
                }
            }
        }

        var report = new BotTownReport
        {
            Town = town.Name,
            Shops = shops,
            Residents = BotResidence.Residents(town),
            Unsafe = unsafeCount,
            Dead = dead,
            Worst = worst,
            WorstReading = worst == null ? BotQuad.Fresh : worstReading,
            Quarantined = map != null && BotBarred.Holds(map, hall),
            Market = BotMarket.Board(4)
        };

        // The island's shortages that this town's shelves cannot answer either: the ones the town would be glad of.
        var dry = BotShops.Dry();

        for (var i = 0; i < dry.Count && report.Out.Count < 3; i++)
        {
            var kind = dry[i].Kind;
            var sold = false;

            for (var v = 0; v < here.Count && !sold; v++)
            {
                sold = BotShops.Sells(here[v], kind, out _);
            }

            if (!sold)
            {
                report.Out.Add(kind);
            }
        }

        report.Said = Plain(report);
        report.Facts = Brief(report, town, guild);

        return report;
    }

    private static string Names(List<Type> kinds)
    {
        if (kinds.Count == 0)
        {
            return "nothing the island is short of";
        }

        List<string> names = [];

        for (var i = 0; i < kinds.Count; i++)
        {
            names.Add(kinds[i].Name);
        }

        return string.Join(", ", names);
    }

    /// <summary>The rule's words for the town: its shelves and its ground, in two sentences at most.</summary>
    private static string Plain(BotTownReport r)
    {
        var shelves = r.Out.Count > 0
            ? $"{r.Town}'s {r.Shops} shops are out of {Names(r.Out)}."
            : $"{r.Town}'s {r.Shops} shops want for nothing the island is short of.";

        var ground = r.Worst == null
            ? "The ground round the town is quiet."
            : $"{r.Unsafe} squares round the town read unsafe, and {r.Dead} of ours have died in them; the worst is at {r.Worst.Middle.X},{r.Worst.Middle.Y}.";

        return $"{shelves} {ground}";
    }

    private static string Brief(BotTownReport r, BotTowns.Town town, Guild guild)
    {
        var worst = r.Worst == null
            ? "none"
            : $"the square at ({r.Worst.Middle.X}, {r.Worst.Middle.Y}), reading {BotQuad.Band(r.WorstReading)} ({r.WorstReading:F2}) on {r.Worst.Deaths} dead and {r.Worst.Mobs} hostile at the last look, asking {BotQuad.Muscle(r.WorstReading):F0} strength of a lone bot";

        var standing = BotBurgh.Of(guild?.Name, town);

        return $"The town: {r.Town}. {r.Shops} shopkeepers, {r.Residents} of the population live here, {town.Seated} guilds are seated by it.{(r.Quarantined ? " Its hall stands under a quarantine." : "")}\n"
            + $"Its shops are out of: {Names(r.Out)}.\n"
            + $"The island's market (what is wanted and what is stocked): {r.Market}.\n"
            + $"Danger within {Reach} tiles of its hall: {r.Unsafe} squares read unsafe or worse, {r.Dead} of the population have died in them; the worst is {worst}.\n"
            + $"The guild before you: {guild?.Name}, {guild?.Members?.Count ?? 0} members, might {BotWar.Might(guild?.Name):F0}. "
            + $"Its standing here: prices ×{standing.Factor:F3}, {standing.Fails} tasks failed in a row ({BotBurgh.FailsToExile} in a row and it is put out of the town for {BotBurgh.ExileHours:0} hours), {standing.Done} done and {standing.Failed} failed in all.\n";
    }
}
