using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items;

public class DisguisePersistence : GenericPersistence
{
    private static DisguisePersistence _disguisePersistence;
    public static Dictionary<Mobile, Timer> Timers { get; } = new();

    public static void Configure()
    {
        _disguisePersistence = new DisguisePersistence();
    }

    public DisguisePersistence() : base("Disguises", 10)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; ++i)
        {
            var m = reader.ReadEntity<Mobile>();
            CreateTimer(m, reader.ReadTimeSpan());
            m.NameMod = reader.ReadString();
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(Timers.Count);
        foreach (var (m, timer) in Timers)
        {
            writer.Write(m);
            writer.Write(timer.Next - Core.Now);
            writer.Write(m.NameMod);
        }
    }

    public static void CreateTimer(Mobile m, TimeSpan delay)
    {
        if (m != null && !IsDisguised(m))
        {
            Timers[m] = new InternalTimer(m, delay);
        }
    }

    public static void StartTimer(Mobile m)
    {
        if (Timers.TryGetValue(m, out var t))
        {
            t.Start();
        }
    }

    public static bool IsDisguised(Mobile m) => Timers.ContainsKey(m);

    public static void StopTimer(Mobile m)
    {
        if (!Timers.TryGetValue(m, out var t))
        {
            return;
        }

        t.Delay = Utility.Max(t.Next - Core.Now, TimeSpan.Zero);
        t.Stop();
    }

    public static void RemoveTimer(Mobile m)
    {
        if (Timers.Remove(m, out var t))
        {
            t.Stop();
        }
    }

    public static TimeSpan TimeRemaining(Mobile m) =>
        Timers.TryGetValue(m, out var t) ? t.Next - Core.Now : TimeSpan.Zero;

    private class InternalTimer : Timer
    {
        private readonly Mobile _player;

        public InternalTimer(Mobile m, TimeSpan delay) : base(delay) => _player = m;

        protected override void OnTick()
        {
            _player.NameMod = null;

            if (_player is PlayerMobile mobile)
            {
                mobile.SetHairMods(-1, -1);
            }

            RemoveTimer(_player);
        }
    }
}

[ManualDirtyChecking]
[TypeAlias("Server.Items.DisguisePersistance")]
[Obsolete("Deprecated in favor of the static system. Only used for legacy deserialization")]
public class DisguisePersistenceTimers : Item
{
    public DisguisePersistenceTimers() : base(1)
    {
        Movable = false;
    }

    public DisguisePersistenceTimers(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Disguise Persistence - Internal";

    public override void Serialize(IGenericWriter writer)
    {
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        var version = reader.ReadInt();

        var count = reader.ReadInt();
        for (var i = 0; i < count; ++i)
        {
            var m = reader.ReadEntity<Mobile>();
            DisguisePersistence.CreateTimer(m, reader.ReadTimeSpan());
            m.NameMod = reader.ReadString();
        }

        Timer.DelayCall(Delete);
    }
}
