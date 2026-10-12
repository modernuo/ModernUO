using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-market.json</c> may say: speeds and distances, never prices. Keys are PascalCase, like every other
/// file of this project's — a lower-case key is silently ignored by the engine's reader.
/// </summary>
public sealed class BotMarketSettings
{
    public bool? Running { get; set; }

    public double? RaiseStep { get; set; }

    public double? CutStep { get; set; }

    public int? BriskMs { get; set; }

    public int? StaleMs { get; set; }

    public int? RaiseMs { get; set; }

    public double? MostMultiple { get; set; }

    public double? LeastMultiple { get; set; }

    public int? Floor { get; set; }

    public int? MaxWants { get; set; }

    public int? BeatMs { get; set; }

    public int? SayWantMs { get; set; }

    /// <summary>A risen buyer's grace before its wants are asked whether it can pay; a want's longest stand at a price it cannot raise (02.10.2026, build 391).</summary>
    public int? RisenGraceMs { get; set; }

    public int? MostAgeMs { get; set; }

    public int? BankReach { get; set; }

    public int? MeetTiles { get; set; }

    public int? CourierReach { get; set; }

    public int? CourierLeastGold { get; set; }

    public int? DeliverGiveUpMs { get; set; }

    /// <summary>The balance of supply against spoken demand (01.10.2026, Patrick's point 8). See <see cref="BotGlut"/>.</summary>
    public bool? GlutEnabled { get; set; }

    public int? GlutWindowMs { get; set; }

    public double? GlutFloor { get; set; }

    public int? GlutTolerance { get; set; }

    public double? GlutShunned { get; set; }

    /// <summary>The meeting between buyer and carrier (02.10.2026, Patrick's order). See <see cref="BotRendezvous"/>.</summary>
    public bool? Meetings { get; set; }

    public int? MeetingCallTiles { get; set; }

    public int? MeetingMs { get; set; }

    public int? MeetingWaitMs { get; set; }

    public int? MeetingSafeReach { get; set; }

    public double? MeetingExpects { get; set; }

    /// <summary>The buyer that stands for its carrier (12.10.2026, w7). See <see cref="BotRendezvous.StandTiles"/>.</summary>
    public int? MeetingStandTiles { get; set; }

    public int? MeetingStandMs { get; set; }

    public int? MeetingStandMostMs { get; set; }

    /// <summary>How long a carrier walks after its buyer coming no nearer before it lets it go (12.10.2026, w7). See <see cref="BotDeliver.LosingMs"/>.</summary>
    public int? DeliverLosingMs { get; set; }
}

public static class BotMarketConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarketConfig));

    private const string ConfigPath = "Configuration/bot-market.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotMarketSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotMarketSettings());

            logger.Information("Wrote a starter market file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotMarket.Running = settings.Running ?? BotMarket.Running;
        BotMarket.RaiseStep = settings.RaiseStep ?? BotMarket.RaiseStep;
        BotMarket.CutStep = settings.CutStep ?? BotMarket.CutStep;
        BotMarket.BriskMs = settings.BriskMs ?? BotMarket.BriskMs;
        BotMarket.StaleMs = settings.StaleMs ?? BotMarket.StaleMs;

        // Only when the file gives it: read back as "?? RaiseMs" it would freeze today's StaleMs into it for good.
        if (settings.RaiseMs is { } raiseMs)
        {
            BotMarket.RaiseMs = raiseMs;
        }

        BotMarket.MostMultiple = settings.MostMultiple ?? BotMarket.MostMultiple;
        BotMarket.LeastMultiple = settings.LeastMultiple ?? BotMarket.LeastMultiple;
        BotMarket.Floor = settings.Floor ?? BotMarket.Floor;
        BotMarket.MaxWants = settings.MaxWants ?? BotMarket.MaxWants;
        BotMarket.BeatMs = settings.BeatMs ?? BotMarket.BeatMs;
        BotMarket.SayWantMs = settings.SayWantMs ?? BotMarket.SayWantMs;
        BotMarket.RisenGraceMs = settings.RisenGraceMs ?? BotMarket.RisenGraceMs;
        BotMarket.MostAgeMs = settings.MostAgeMs ?? BotMarket.MostAgeMs;
        BotMarket.BankReach = settings.BankReach ?? BotMarket.BankReach;
        BotHandover.MeetTiles = settings.MeetTiles ?? BotHandover.MeetTiles;
        BotCourier.Reach = settings.CourierReach ?? BotCourier.Reach;
        BotCourier.LeastGold = settings.CourierLeastGold ?? BotCourier.LeastGold;
        BotDeliver.GiveUpMs = settings.DeliverGiveUpMs ?? BotDeliver.GiveUpMs;
        BotGlut.Enabled = settings.GlutEnabled ?? BotGlut.Enabled;
        BotGlut.WindowMs = settings.GlutWindowMs ?? BotGlut.WindowMs;
        BotGlut.Floor = settings.GlutFloor ?? BotGlut.Floor;
        BotGlut.Tolerance = settings.GlutTolerance ?? BotGlut.Tolerance;
        BotGlut.Shunned = settings.GlutShunned ?? BotGlut.Shunned;
        BotRendezvous.Running = settings.Meetings ?? BotRendezvous.Running;
        BotRendezvous.CallTiles = settings.MeetingCallTiles ?? BotRendezvous.CallTiles;
        BotRendezvous.MeetMs = settings.MeetingMs ?? BotRendezvous.MeetMs;
        BotRendezvous.WaitMs = settings.MeetingWaitMs ?? BotRendezvous.WaitMs;
        BotRendezvous.SafeReach = settings.MeetingSafeReach ?? BotRendezvous.SafeReach;
        BotRendezvous.Prior = settings.MeetingExpects ?? BotRendezvous.Prior;
        BotRendezvous.StandTiles = settings.MeetingStandTiles ?? BotRendezvous.StandTiles;
        BotRendezvous.StandMs = settings.MeetingStandMs ?? BotRendezvous.StandMs;
        BotRendezvous.StandMostMs = settings.MeetingStandMostMs ?? BotRendezvous.StandMostMs;
        BotDeliver.LosingMs = settings.DeliverLosingMs ?? BotDeliver.LosingMs;
    }
}
