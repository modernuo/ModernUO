using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.AntiBot
{
    public static class AntiBotSystem
    {
        private class AntiBotChallenge
        {
            public int Code { get; set; }
            public DateTime ChallengeExpiry { get; set; }
            public Action SuccessCallback { get; set; }
            public Timer TimeoutTimer { get; set; }
        }

        private static readonly Dictionary<Mobile, AntiBotChallenge> _activeChallenges = new();

        public static bool Enabled { get; set; } = true;
        public static int MaxAttempts { get; set; } = 1;
        public static TimeSpan ChallengeTimeout { get; set; } = TimeSpan.FromMinutes(2);

        public static bool CheckPlayer(Mobile from, Action onSuccess)
        {
            if (!Enabled || from is not PlayerMobile)
            {
                return true;
            }

            lock (_activeChallenges)
            {
                CleanupExpiredChallenges();

                if (_activeChallenges.TryGetValue(from, out var existing))
                {
                    from.CloseGump<AntiBotGump>();
                    from.SendGump(new AntiBotGump(from, existing.Code));
                    return false;
                }

                var challenge = new AntiBotChallenge
                {
                    Code = Utility.RandomMinMax(1000, 9999),
                    ChallengeExpiry = Core.Now.Add(ChallengeTimeout),
                    SuccessCallback = onSuccess
                };

                challenge.TimeoutTimer = Timer.DelayCall(ChallengeTimeout, () =>
                {
                    lock (_activeChallenges)
                    {
                        if (_activeChallenges.ContainsKey(from))
                        {
                            _activeChallenges.Remove(from);
                            from.SendMessage("Anti-Bot: Verification timed out. Disconnecting...");
                            from.NetState?.Disconnect("Anti-Bot: Verification failed by timing out.");
                        }
                    }
                });

                _activeChallenges[from] = challenge;
                from.CloseGump<AntiBotGump>();
                from.SendGump(new AntiBotGump(from, challenge.Code));
                return false;
            }
        }

        internal static void ProcessResponse(Mobile from, int enteredCode, bool cancelled)
        {
            lock (_activeChallenges)
            {
                if (!_activeChallenges.TryGetValue(from, out var challenge))
                {
                    return;
                }

                challenge.TimeoutTimer?.Stop();
                _activeChallenges.Remove(from);

                if (cancelled)
                {
                    from.SendMessage("Anti-Bot: Verification cancelled. Disconnecting...");
                    from.NetState?.Disconnect("Anti-Bot: Verification failed by cancellation.");
                    return;
                }

                if (enteredCode == challenge.Code)
                {
                    from.SendMessage("Anti-Bot: Verification successful!!!");
                }
                else
                {
                    from.SendMessage("Anti-Bot: Incorrect number. Disconnecting...");
                    from.NetState?.Disconnect("Anti-Bot: Verification failed by incorrect number.");
                }
            }
        }

        private static void CleanupExpiredChallenges()
        {
            var now = Core.Now;
            var toRemove = new List<Mobile>();

            foreach (var kvp in _activeChallenges)
            {
                if (kvp.Value.ChallengeExpiry < now)
                {
                    kvp.Value.TimeoutTimer?.Stop();
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var mobile in toRemove)
            {
                _activeChallenges.Remove(mobile);
            }
        }

        public static void CancelChallenge(Mobile from)
        {
            lock (_activeChallenges)
            {
                if (_activeChallenges.TryGetValue(from, out var challenge))
                {
                    challenge.TimeoutTimer?.Stop();
                    _activeChallenges.Remove(from);
                }
            }
        }
    }
}