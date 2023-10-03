using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Server.Collections;
using Server.Mobiles;

namespace Server.Engines.CannedEvil;

public class ChampionTitleSystem : GenericPersistence
{
    private static ChampionTitleSystem _championTitlePersistence;

    // All of the players with murders
    private static readonly Dictionary<PlayerMobile, ChampionTitleContext> _championTitleContexts = new();

    private static readonly Timer _championTitleTimer = new ChampionTitleTimer();

    public static void Configure()
    {
        _championTitlePersistence = new ChampionTitleSystem();
    }

    public static void Initialize()
    {
        EventSink.PlayerDeleted += OnPlayerDeleted;

        _championTitleTimer.Start();
    }

    private static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm)
        {
            _championTitleContexts.Remove(pm);
        }
    }

    public ChampionTitleSystem() : base("ChampionTitles", 10)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; ++i)
        {
            var context = new ChampionTitleContext(reader.ReadEntity<PlayerMobile>());
            context.Deserialize(reader);

            _championTitleContexts.Add(context.Player, context);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_championTitleContexts.Count);
        foreach (var (m, context) in _championTitleContexts)
        {
            writer.Write(m);
            context.Serialize(writer);
        }
    }

    public static bool GetChampionTitleContext(PlayerMobile player, out ChampionTitleContext context)
    {
        if (player != null && _championTitleContexts.TryGetValue(player, out context))
        {
            return true;
        }

        context = null;
        return false;
    }

    public static ChampionTitleContext GetOrCreateChampionTitleContext(PlayerMobile player)
    {
        ref var context = ref CollectionsMarshal.GetValueRefOrAddDefault(_championTitleContexts, player, out var exists);
        if (!exists)
        {
            context = new ChampionTitleContext(player);
        }

        return context;
    }

    // Called when killing a harrower. Will give a minimum of 1 point.
    public static void AwardHarrowerTitle(PlayerMobile pm)
    {
        var context = GetOrCreateChampionTitleContext(pm);

        var count = 1;
        for (var i = 0; i < ChampionSpawnInfo.Table.Length; i++)
        {
            var title = context.GetTitle(ChampionSpawnInfo.Table[i].Type);
            if (title?.Value > 900)
            {
                count++;
            }
        }

        context.Harrower = Math.Max(count, context.Harrower); // Harrower titles never decay.
    }

    public static int GetChampionTitleLabel(PlayerMobile player)
    {
        if (!GetChampionTitleContext(player, out var context))
        {
            return 0;
        }

        if (context.Harrower > 0)
        {
            return 1113082 + Math.Min(context.Harrower, 10);
        }

        var highestValue = 0;
        var highestType = 0;

        for (var i = 0; i < ChampionSpawnInfo.Table.Length; i++)
        {
            var t = context.GetTitle(ChampionSpawnInfo.Table[i].Type);
            if (t == null)
            {
                continue;
            }

            var v = t.Value;

            if (v > highestValue)
            {
                highestValue = v;
                highestType = i;
            }
        }

        var offset = highestValue switch
        {
            > 800 => 3,
            > 300 => highestValue / 300,
            _     => 0
        };

        if (offset > 0)
        {
            var champInfo = ChampionSpawnInfo.Table[highestType];
            var championLevelName = champInfo.LevelNames[Math.Min(offset, champInfo.LevelNames.Length) - 1];
            return championLevelName.Number;
        }

        return 0;
    }

    private class ChampionTitleTimer : Timer
    {
        public ChampionTitleTimer() : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
        {
        }

        protected override void OnTick()
        {
            if (_championTitleContexts.Count == 0)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();

            foreach (var context in _championTitleContexts.Values)
            {
                if (!context.CheckAtrophy())
                {
                    queue.Enqueue(context.Player);
                }
            }

            while (queue.Count > 0)
            {
                _championTitleContexts.Remove((PlayerMobile)queue.Dequeue());
            }
        }
    }
}
