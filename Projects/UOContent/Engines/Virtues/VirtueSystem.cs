using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Server.Collections;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Virtues;

public enum VirtueLevel
{
    None,
    Seeker,
    Follower,
    Knight
}

[Flags]
public enum VirtueName
{
    Humility,
    Sacrifice,
    Compassion,
    Spirituality,
    Valor,
    Honor,
    Justice,
    Honesty
}

public class VirtueSystem : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(VirtueSystem));

    private static readonly Dictionary<PlayerMobile, VirtueContext> _playerVirtues = new();

    private static VirtueSystem _virtueSystemPersistence;

    public static void Configure()
    {
        _virtueSystemPersistence = new VirtueSystem();
    }

    public VirtueSystem() : base("Virtues", 10)
    {
    }

    private static void FixVirtue(Mobile m, int[] virtueValues)
    {
        if (m is not PlayerMobile pm)
        {
            return;
        }

        var virtues = pm.Virtues;
        for (var i = 0; i < virtueValues.Length; i++)
        {
            var val = virtueValues[i];
            if (val > 0)
            {
                virtues.SetValue(i, val);
            }
        }
    }

    public static void Initialize()
    {
        var migrations = Mobile.VirtueMigrations;
        if (migrations?.Count > 0)
        {
            foreach (var (m, values) in migrations)
            {
                FixVirtue(m, values);
            }
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_playerVirtues.Count);
        foreach (var (pm, virtues) in _playerVirtues)
        {
            writer.Write(pm);
            virtues.Serialize(writer);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var contextCount = reader.ReadEncodedInt();
        for (var i = 0; i < contextCount; i++)
        {
            var player = reader.ReadEntity<PlayerMobile>();
            var virtues = new VirtueContext();
            virtues.Deserialize(reader);

            if (virtues.IsUsed())
            {
                _playerVirtues.Add(player, virtues);
            }
        }
    }

    public static VirtueContext GetVirtues(PlayerMobile from) =>
        from != null && _playerVirtues.TryGetValue(from, out var context) ? context : null;

    public static VirtueContext GetOrCreateVirtues(PlayerMobile from)
    {
        if (from == null)
        {
            return null;
        }

        ref VirtueContext context = ref CollectionsMarshal.GetValueRefOrAddDefault(_playerVirtues, from, out bool exists);
        if (!exists)
        {
            context = new VirtueContext();
        }

        return context;
    }

    public static bool IsHighestPath(PlayerMobile from, VirtueName virtue) =>
        GetVirtues(from)?.GetValue((int)virtue) >= GetMaxAmount(virtue);

    public static VirtueLevel GetLevel(Mobile from, VirtueName virtue)
    {
        var v = GetVirtues(from as PlayerMobile)?.GetValue((int)virtue) ?? 0;
        int vl;

        if (v < 4000)
        {
            vl = 0;
        }
        else if (v >= GetMaxAmount(virtue))
        {
            vl = 3;
        }
        else
        {
            vl = (v + 9999) / 10000;
        }

        return (VirtueLevel)vl;
    }

    public static string GetName(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Humility     => "Humility",
            VirtueName.Sacrifice    => "Sacrifice",
            VirtueName.Compassion   => "Compassion",
            VirtueName.Spirituality => "Spirituality",
            VirtueName.Valor        => "Valor",
            VirtueName.Honor        => "Honor",
            VirtueName.Justice      => "Justice",
            VirtueName.Honesty      => "Honesty",
            _                       => ""
        };

    public static string GetLowerCaseName(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Humility     => "humility",
            VirtueName.Sacrifice    => "sacrifice",
            VirtueName.Compassion   => "compassion",
            VirtueName.Spirituality => "spirituality",
            VirtueName.Valor        => "valor",
            VirtueName.Honor        => "honor",
            VirtueName.Justice      => "justice",
            VirtueName.Honesty      => "honesty",
            _                       => ""
        };

    public static int GetMaxAmount(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Honor => 20000,
            VirtueName.Sacrifice => 22000,
            _ => 21000
        };

    public static int GetGainedLocalizedMessage(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Sacrifice    => 1054160, // You have gained in sacrifice.
            VirtueName.Compassion   => 1053002, // You have gained in compassion.
            VirtueName.Spirituality => 1155832, // You have gained in Spirituality.
            VirtueName.Valor        => 1054030, // You have gained in Valor!
            VirtueName.Honor        => 1063225, // You have gained in Honor.
            VirtueName.Justice      => 1049363, // You have gained in Justice.
            VirtueName.Humility     => 1052070, // You have gained in Humility.
            _                       => 0
        };

    public static int GetGainedAPathLocalizedMessage(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Sacrifice    => 1052008, // You have gained a path in Sacrifice!
            VirtueName.Spirituality => 1155833, // "You have gained a path in Spirituality!" (Why are there quotes?)
            VirtueName.Valor        => 1054032, // You have gained a path in Valor!
            VirtueName.Honor        => 1063226, // You have gained a path in Honor!
            VirtueName.Justice      => 1049367, // You have gained a path in Justice!
            VirtueName.Humility     => 1155811, // You have gained a path in Humility!
            _                       => 0
        };

    public static int GetHightestPathLocalizedMessage(VirtueName virtue) =>
        virtue switch
        {
            VirtueName.Compassion => 1053003, // You have achieved the highest path of compassion and can no longer gain any further.
            VirtueName.Spirituality => 1155831, // You cannot gain more Spirituality.
            VirtueName.Valor => 1054031, // You have achieved the highest path in Valor and can no longer gain any further.
            VirtueName.Honor => 1063228, // You cannot gain more Honor.
            VirtueName.Justice => 1049534, // You cannot gain more Justice.
            VirtueName.Humility => 1155808, // You cannot gain more Humility.
            VirtueName.Honesty => 1153771, // You have achieved the highest path in Honesty and can no longer gain any further.
            _ => 1052050, // You have achieved the highest path in this virtue.
        };

    public static bool Award(PlayerMobile from, VirtueName virtue, int amount, ref bool gainedPath)
    {
        var virtues = from.Virtues;

        var current = virtues.GetValue((int)virtue);

        var maxAmount = GetMaxAmount(virtue);

        if (current >= maxAmount)
        {
            return false;
        }

        if (current + amount >= maxAmount)
        {
            amount = maxAmount - current;
        }

        var oldLevel = GetLevel(from, virtue);

        virtues.SetValue((int)virtue, current + amount);

        gainedPath = GetLevel(from, virtue) != oldLevel;

        return true;
    }

    public static bool Atrophy(PlayerMobile from, VirtueName virtue, int amount = 1)
    {
        var virtues = GetVirtues(from);
        if (virtues == null)
        {
            return false;
        }

        var current = virtues.GetValue((int)virtue);

        if (current - amount >= 0)
        {
            virtues.SetValue((int)virtue, current - amount);
        }
        else
        {
            virtues.SetValue((int)virtue, 0);
        }

        return current > 0;
    }

    public static bool IsSeeker(PlayerMobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Seeker;

    public static bool IsFollower(PlayerMobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Follower;

    public static bool IsKnight(PlayerMobile from, VirtueName virtue) => GetLevel(from, virtue) >= VirtueLevel.Knight;

    public static void AwardVirtue(PlayerMobile pm, VirtueName virtue, int amount)
    {
        var virtues = GetOrCreateVirtues(pm);
        if (virtue == VirtueName.Compassion)
        {
            if (virtues.CompassionGains > 0 && Core.Now > virtues.NextCompassionDay)
            {
                virtues.NextCompassionDay = DateTime.MinValue;
                virtues.CompassionGains = 0;
            }

            if (virtues.CompassionGains >= 5)
            {
                pm.SendLocalizedMessage(1053004); // You must wait about a day before you can gain in compassion again.
                return;
            }
        }

        var gainedPath = false;
        var virtueName = GetName(virtue);

        if (Award(pm, virtue, amount, ref gainedPath))
        {
            if (gainedPath)
            {
                var gainedPathMessage = GetGainedAPathLocalizedMessage(virtue);
                if (gainedPathMessage != 0)
                {
                    pm.SendLocalizedMessage(gainedPathMessage);
                }
                else
                {
                    pm.SendMessage($"You have gained a path in {virtueName}!");
                }
            }
            else
            {
                var gainMessage = GetGainedLocalizedMessage(virtue);
                if (gainMessage != 0)
                {
                    pm.SendLocalizedMessage(gainMessage);
                }
                else
                {
                    pm.SendMessage($"You have gained in {virtueName}.");
                }
            }

            if (virtue == VirtueName.Compassion)
            {
                virtues.NextCompassionDay = Core.Now + TimeSpan.FromDays(1.0);

                if (++virtues.CompassionGains >= 5)
                {
                    // You must wait about a day before you can gain in compassion again.
                    pm.SendLocalizedMessage(1053004);
                }
            }
        }
        else
        {
            pm.SendLocalizedMessage(GetHightestPathLocalizedMessage(virtue));
        }
    }

    public static void CheckAtrophies(PlayerMobile pm)
    {
        SacrificeVirtue.CheckAtrophy(pm);
        JusticeVirtue.CheckAtrophy(pm);
        CompassionVirtue.CheckAtrophy(pm);
        ValorVirtue.CheckAtrophy(pm);
    }

    private class VirtueTimer : Timer
    {
        public VirtueTimer() : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0))
        {
        }

        public static void Initialize()
        {
            new VirtueTimer().Start();
        }

        protected override void OnTick()
        {
            if (_playerVirtues.Count == 0)
            {
                return;
            }

            using var queue = PooledRefQueue<Mobile>.Create();

            // This is not particularly efficient. If it gets too slow, then use a different architecture.
            foreach (var (player, virtues) in _playerVirtues)
            {
                CheckAtrophies(player);

                if (!virtues.IsUsed())
                {
                    queue.Enqueue(player);
                }
            }

            while (queue.Count > 0)
            {
                _playerVirtues.Remove((PlayerMobile)queue.Dequeue());
            }
        }

        ~VirtueTimer()
        {
            VirtueSystem.logger.Error($"{nameof(VirtueTimer)} is no longer running!");
        }
    }
}
