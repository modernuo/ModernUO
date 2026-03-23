using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Logging;
using Server.Misc;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.Engines.PlayerMurderSystem;

public class PlayerMurderSystem : GenericPersistence
{
    private static PlayerMurderSystem _playerMurderPersistence;

    private static readonly ILogger logger = LogFactory.GetLogger(typeof(PlayerMurderSystem));

    // All of the players with murders
    private static readonly Dictionary<PlayerMobile, MurderContext> _murderContexts = new();

    // Only the players that are online
    private static readonly HashSet<MurderContext> _contextTerms = new(MurderContext.EqualityComparer.Default);

    private static readonly HashSet<(Mobile, Mobile)> _recentlyReported = new();
    private static TimeSpan _recentlyReportedDelay;

    private static TimeSpan _shortTermMurderDuration;

    private static TimeSpan _longTermMurderDuration;

    public static TimeSpan ShortTermMurderDuration => _shortTermMurderDuration;

    public static TimeSpan LongTermMurderDuration => _longTermMurderDuration;

    public static bool PingPongEnabled => Core.T2A && !Core.LBR;

    public static bool BountiesEnabled { get; private set; }

    private static TimeSpan _bountyExpiry;

    public static void Configure()
    {
        _shortTermMurderDuration = ServerConfiguration.GetOrUpdateSetting("murderSystem.shortTermMurderDuration", TimeSpan.FromHours(8));
        _longTermMurderDuration = ServerConfiguration.GetOrUpdateSetting("murderSystem.longTermMurderDuration", TimeSpan.FromHours(40));
        BountiesEnabled = ServerConfiguration.GetOrUpdateSetting("murderSystem.bountiesEnabled", !Core.LBR);
        _recentlyReportedDelay = ServerConfiguration.GetOrUpdateSetting("murderSystem.recentlyReportedDelay", TimeSpan.FromMinutes(10));
        _bountyExpiry = ServerConfiguration.GetOrUpdateSetting("murderSystem.bountyExpiry", TimeSpan.FromDays(14));

        _playerMurderPersistence = new PlayerMurderSystem();
    }

    public static void Initialize()
    {
        EventSink.Disconnected += OnDisconnected;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm && _murderContexts.Remove(pm, out var context))
        {
            _contextTerms.Remove(context);
        }
    }

    public PlayerMurderSystem() : base("PlayerMurders", 10)
    {
    }

    // Only used for migrations!
    public static void MigrateContext(PlayerMobile player, TimeSpan shortTerm, TimeSpan longTerm)
    {
        if (!World.Loading)
        {
            logger.Error(
                $"Attempted to call MigrateContext outside of world loading.{Environment.NewLine}{{StackTrace}}",
                new StackTrace()
            );
            return;
        }

        var context = GetOrCreateMurderContext(player);

        // We make a big assumption that by the time this is called, the Mobile/PlayerMobile info is deserialized
        if (Mobile.MurderMigrations?.TryGetValue(player, out var shortTermMurders) == true)
        {
            context.ShortTermMurders = shortTermMurders;
        }

        context.ShortTermElapse = shortTerm;
        context.LongTermElapse = longTerm;
        UpdateMurderContext(context);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (!GetMurderContext(pm, out var context))
        {
            return;
        }

        if (context.CheckStart())
        {
            _contextTerms.Add(context);
        }
        else
        {
            _contextTerms.Remove(context);
            if (context.CanRemove())
            {
                _murderContexts.Remove(pm);
            }
        }
    }

    private static void OnDisconnected(Mobile m)
    {
        if (m is not PlayerMobile pm || !_murderContexts.TryGetValue(pm, out var context))
        {
            return;
        }

        context.DecayKills();
        _contextTerms.Remove(context);

        if (context.CanRemove())
        {
            _murderContexts.Remove(pm);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; ++i)
        {
            var context = new MurderContext(reader.ReadEntity<PlayerMobile>());
            context.Deserialize(reader);

            _murderContexts.Add(context.Player, context);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_murderContexts.Count);
        foreach (var (m, context) in _murderContexts)
        {
            writer.Write(m);
            context.Serialize(writer);
        }
    }

    public static bool GetMurderContext(PlayerMobile player, out MurderContext context)
    {
        if (player != null && _murderContexts.TryGetValue(player, out context))
        {
            return true;
        }

        context = null;
        return false;
    }

    public static MurderContext GetOrCreateMurderContext(PlayerMobile player)
    {
        if (player == null)
        {
            return null;
        }

        ref var context = ref CollectionsMarshal.GetValueRefOrAddDefault(_murderContexts, player, out var exists);
        if (!exists)
        {
            context = new MurderContext(player);
        }

        return context;
    }

    public static int GetBounty(PlayerMobile player) =>
        GetMurderContext(player, out var context) ? context.Bounty : 0;

    public static void AddBounty(PlayerMobile player, int amount)
    {
        if (GetMurderContext(player, out var context))
        {
            context.Bounty += amount;
        }
    }

    public static void ClearBounty(PlayerMobile player)
    {
        if (GetMurderContext(player, out var context))
        {
            context.Bounty = 0;
        }
    }

    public static int GetActiveBountyCount()
    {
        var neverExpire = _bountyExpiry == TimeSpan.Zero;
        var cutoff = neverExpire ? DateTime.MinValue : Core.Now - _bountyExpiry;
        var count = 0;

        foreach (var (player, context) in _murderContexts)
        {
            if (player.Murderer && context.Bounty > 0 && (neverExpire || context.LastMurderTime >= cutoff))
            {
                count++;
            }
        }

        return count;
    }

    // Reused across calls — safe because the server is single-threaded.
    private static readonly List<(PlayerMobile Player, int Bounty)> _activeBountyCache = [];

    public static List<(PlayerMobile Player, int Bounty)> GetActiveBounties()
    {
        _activeBountyCache.Clear();
        var neverExpire = _bountyExpiry == TimeSpan.Zero;
        var cutoff = neverExpire ? DateTime.MinValue : Core.Now - _bountyExpiry;

        foreach (var (player, context) in _murderContexts)
        {
            if (player.Murderer && context.Bounty > 0 && (neverExpire || context.LastMurderTime >= cutoff))
            {
                _activeBountyCache.Add((player, context.Bounty));
            }
        }

        _activeBountyCache.Sort(static (a, b) => b.Bounty.CompareTo(a.Bounty));
        return _activeBountyCache;
    }

    public static void ManuallySetPingPong(PlayerMobile player, int pingPong)
    {
        var context = GetOrCreateMurderContext(player);
        context.PingPongs = Math.Max(pingPong, 0);
        UpdateMurderContext(context);
    }

    public static void ManuallySetShortTermMurders(PlayerMobile player, int shortTermMurders)
    {
        var context = GetOrCreateMurderContext(player);
        context.ShortTermMurders = shortTermMurders;

        context.ResetKillTime();
        UpdateMurderContext(context);
    }

    public static void OnPlayerMurder(PlayerMobile player)
    {
        var context = GetOrCreateMurderContext(player);
        context.ShortTermMurders++;
        player.Kills++;

        if (PingPongEnabled && player.Kills == 5)
        {
            context.PingPongs++;
        }

        context.LastMurderTime = Core.Now;
        context.ResetKillTime();
        UpdateMurderContext(context);
    }

    public static bool IsRecentlyReported(Mobile reporter, Mobile killer) =>
        _recentlyReported.Contains((reporter, killer));

    public static bool ReportMurder(PlayerMobile reporter, Mobile killer)
    {
        if (killer?.Deleted != false || killer is not PlayerMobile pk)
        {
            return false;
        }

        if (!_recentlyReported.Add((reporter, killer)))
        {
            return false;
        }

        Timer.DelayCall(
            _recentlyReportedDelay,
            static (f, k) => _recentlyReported.Remove((f, k)),
            reporter,
            killer
        );

        var wasMurderer = killer.Murderer;
        OnPlayerMurder(pk);

        Titles.SetKarma(pk, pk.Kills * -1000, true);

        pk.SendLocalizedMessage(1049067); // You have been reported for murder!

        if (!wasMurderer && killer.Murderer)
        {
            pk.SendLocalizedMessage(502134); // You are now known as a murderer!
        }

        // with the introduction of PingPongs, a red can technically have 1 kill.
        if (Stealing.SuspendOnMurder && pk.Kills == 1 && pk.NpcGuild == NpcGuild.ThievesGuild)
        {
            pk.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.
        }

        return true;
    }

    private static void UpdateMurderContext(MurderContext context)
    {
        var player = context.Player;

        if (!context.CheckStart())
        {
            if (context.CanRemove())
            {
                _murderContexts.Remove(player);
            }
            _contextTerms.Remove(context);
        }
        else if (player.NetState != null)
        {
            _contextTerms.Add(context);
        }
    }

    public static void ReportKillsToSelf(PlayerMobile player)
    {
        if (Core.SA)
        {
            player.SendLocalizedMessage(1114370, $"{player.ShortTermMurders}\t{player.Kills}");
        }
        else if (Core.SE)
        {
            player.SendMessage($"Short Term Murders: {player.ShortTermMurders} Long Term Murders: {player.Kills}");
        }
        else if (Core.AOS)
        {
            player.SendMessage($"Short Term Murders: {player.ShortTermMurders}");
            player.SendMessage($"Long Term Murders: {player.Kills}");
            if (PingPongEnabled)
            {
                player.SendMessage($"Ping Pongs: {player.PingPongs}");
            }
        }
        else if (!Core.T2A)
        {
            // No "i must consider my sins" before t2a
        }
        else if (PingPongEnabled && player.PingPongs >= 5)
        {
            // Thou art known throughout the land as a murderous brigand.
            player.SendLocalizedMessage(502123, "", player.ShortTermMurders >= 5 ? 0x22 : 0x59);
        }
        else if (player.ShortTermMurders >= 5)
        {
            // If thou should return to the land of the living, the innocent shall wreak havoc upon thy soul
            player.SendLocalizedMessage(502126, "", 0x22);
        }
        else if (player.ShortTermMurders > 0)
        {
            // Although thou hast slain the innocent, thy deeds shall not bring retribution upon thy return to the living
            player.SendLocalizedMessage(502125, "", 0x59);
        }
        else if (player.Kills > 0)
        {
            // Fear not, thou hast not slain the innocent in some time...
            player.SendLocalizedMessage(502124, "", 0x59);
        }
        else  // no kills
        {
            // Fear not, thou hast not slain the innocent.
            player.SendLocalizedMessage(502122, "", 0x59);
        }
    }

    private class MurdererTimer : Timer
    {
        public MurdererTimer() : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
        {
        }

        public static void Initialize()
        {
            new MurdererTimer().Start();
        }

        protected override void OnTick()
        {
            if (_contextTerms.Count == 0)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();

            foreach (var context in _contextTerms)
            {
                context.DecayKills();
                if (!context.CheckStart())
                {
                    queue.Enqueue(context.Player);
                }
            }

            while (queue.Count > 0)
            {
                var pm = (PlayerMobile)queue.Dequeue();
                if (_murderContexts.TryGetValue(pm, out var ctx))
                {
                    if (ctx.CanRemove())
                    {
                        _murderContexts.Remove(pm);
                    }
                    _contextTerms.Remove(ctx);
                }
            }
        }

        ~MurdererTimer()
        {
            PlayerMurderSystem.logger.Error($"{nameof(MurdererTimer)} is no longer running!");
        }
    }
}
