using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// SE-era archery ammo recovery. When a player misses a shot there is a chance the spent ammo
    /// can be gathered back up afterwards. The banked ammo is held off of <see cref="PlayerMobile"/>
    /// (in this side table) so the vast majority of players who never miss with a bow carry no extra
    /// state. A single repeating timer per tracked player gathers the ammo, but only once the archer
    /// has disengaged: out of warmode and not running (standing still or walking is fine).
    /// </summary>
    public static class AmmoRecovery
    {
        // How often we re-check whether the archer has settled enough to gather their ammo.
        private static readonly TimeSpan RecoveryInterval = TimeSpan.FromSeconds(10);

        // A running step is only "current" if one landed this recently. Comfortably above the running
        // step cadence (run-foot ~200ms, run-mount ~100ms) so an actively running archer always reads as
        // running, while a stopped runner ages out within ~half a second.
        private const long RunningWindowMillis = 500;

        // Only players who have banked recoverable ammo appear here, so this stays tiny.
        private static readonly Dictionary<PlayerMobile, RecoveryState> _states = new();

        private sealed class RecoveryState
        {
            public readonly Dictionary<Type, int> Ammo = new();
            public RecoveryTimer Timer;
        }

        private sealed class RecoveryTimer : Timer
        {
            private readonly PlayerMobile _player;

            // delay == interval, count 0 => first fire after the interval, then repeats until Stop().
            public RecoveryTimer(PlayerMobile player) : base(RecoveryInterval, 0) => _player = player;

            protected override void OnTick() => OnRecoveryTick(_player);
        }

        // Called from BaseRanged.OnMiss when a player misses and the shot is recoverable.
        public static void Bank(PlayerMobile player, Type ammoType)
        {
            if (!Core.SE || ammoType == null)
            {
                return;
            }

            if (!_states.TryGetValue(player, out var state))
            {
                state = new RecoveryState();
                state.Timer = new RecoveryTimer(player);
                state.Timer.Start();
                _states[player] = state;
            }

            state.Ammo.TryGetValue(ammoType, out var count);
            state.Ammo[ammoType] = count + 1;
        }

        // Stops tracking a player and cancels their pending recovery (e.g. on delete).
        public static void Forget(PlayerMobile player)
        {
            if (_states.Remove(player, out var state))
            {
                state.Timer?.Stop();
            }
        }

        private static void OnRecoveryTick(PlayerMobile player)
        {
            if (!_states.TryGetValue(player, out var state))
            {
                return;
            }

            if (player.Deleted || state.Ammo.Count == 0)
            {
                Forget(player);
                return;
            }

            // Gather the scattered ammo only once the archer has disengaged: out of warmode and not running.
            if (!player.Alive || player.Warmode || IsRunning(player))
            {
                return; // Not settled yet; try again on the next tick.
            }

            Recover(player, state);
            Forget(player);
        }

        // The Direction.Running bit is left set after the player stops, so it alone can't tell us whether
        // the archer is still running. We pair it with movement recency: a running step is only "current"
        // if one landed within roughly two run-step intervals. A stopped runner ages out of that window,
        // and a walker never sets the bit at all -- both are treated as "not running", which is what we want.
        private static bool IsRunning(Mobile m) =>
            (m.Direction & Direction.Running) != 0 &&
            Core.TickCount - m.LastMoveTime < RunningWindowMillis;

        private static void Recover(PlayerMobile player, RecoveryState state)
        {
            foreach (var (type, amount) in state.Ammo)
            {
                if (amount <= 0)
                {
                    continue;
                }

                Item ammo = null;

                try
                {
                    ammo = type.CreateInstance<Item>();
                }
                catch
                {
                    // ignored
                }

                if (ammo == null)
                {
                    continue;
                }

                ammo.Amount = amount;

                var name = ammo.Name;
                if (name == null)
                {
                    var label = ammo.LabelNumber;

                    // Arrow (1023903/1023904) and bolt (1027163/1027164) name clilocs keep their plural form
                    // two entries above the singular, so bump the label when recovering more than one.
                    if (ammo.Amount != 1 && label is 1023903 or 1023904 or 1027163 or 1027164)
                    {
                        label += 2;
                    }

                    name = $"#{label}";
                }

                player.PlaceInBackpack(ammo);
                player.SendLocalizedMessage(1073504, $"{ammo.Amount}\t{name}"); // You recover ~1_NUM~ ~2_AMMO~.
            }

            state.Ammo.Clear();
        }
    }
}
