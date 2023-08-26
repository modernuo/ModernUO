using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Server.Mobiles;

namespace Server.Spells;

public class UnsummonTimer : Timer
{
    // Track timers since some of them are really long and might hold references to long dead/deleted mobs
    private static readonly Dictionary<BaseCreature, UnsummonTimer> _timers = new();
    private BaseCreature _creature;
    private Action _onUnsummon;

    public static void StopTimer(BaseCreature creature)
    {
        if (_timers.Remove(creature, out var timer))
        {
            timer.Stop();
        }
    }

    public UnsummonTimer(BaseCreature creature, TimeSpan delay, Action onUnsummon = null) : base(delay)
    {
        _onUnsummon = onUnsummon;
        _creature = creature;

        ref var timer = ref CollectionsMarshal.GetValueRefOrAddDefault(_timers, creature, out bool exists);
        if (exists)
        {
            timer.Stop();
        }

        timer = this;
    }

    protected override void OnTick()
    {
        // BaseCreature.OnAfterDelete will remove the creature from the timers table
        _creature?.Delete();
        _onUnsummon?.Invoke();
    }
}
