/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AntiBotSystem.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.AntiBot
{
    public static class AntiBotSystem
    {
        private class AntiBotChallenge
        {
            public string ChallengeId { get; set; }
            public DateTime ChallengeExpiry { get; set; }
            public Action SuccessCallback { get; set; }
            public Timer TimeoutTimer { get; set; }
            public bool UseTurnstile { get; set; }
            public int FallbackCode { get; set; }
        }

        private static readonly Dictionary<Mobile, AntiBotChallenge> _activeChallenges = new();
        private static readonly HttpClient _httpClient = new();

        // enable or disable the entire anti-bot verification system
        public static bool Enabled { get; set; } = true;

        // if set to false (default) = uses a number matching verification
        // if set to true = uses Cloudflare's Turnstile verification
        public static bool UseTurnstile { get; set; } = false;

        // Cloudflare Turnstile
        // secret key from your Cloudflare account (https://dash.cloudflare.com/login)
        public static string TurnstileSecretKey { get; set; } = "YOUR_SECRET_KEY";

        // the base URL where the widget is hosted (must support HTTPS)
        // view the docs here: https://developers.cloudflare.com/turnstile/
        public static string VerificationUrl { get; set; } = "https://yourwebserver.com/verify";

        // timeout before disconnecting the user (applies to both Turnstile and number match verification)
        public static TimeSpan ChallengeTimeout { get; set; } = TimeSpan.FromMinutes(5);

        public static bool CheckPlayer(Mobile from, Action onSuccess)
        {
            if (!Enabled || from is not PlayerMobile)
            {
                return true;
            }

            CleanupExpiredChallenges();

            if (_activeChallenges.ContainsKey(from))
            {
                return false;
            }

            var challengeId = Guid.NewGuid().ToString("N")[..8];
            var challenge = new AntiBotChallenge
            {
                ChallengeId = challengeId,
                ChallengeExpiry = Core.Now.Add(ChallengeTimeout),
                SuccessCallback = onSuccess,
                UseTurnstile = UseTurnstile,
                FallbackCode = Utility.RandomMinMax(1000, 9999)
            };

            challenge.TimeoutTimer = Timer.DelayCall(ChallengeTimeout, () =>
            {
                if (_activeChallenges.ContainsKey(from))
                {
                    _activeChallenges.Remove(from);
                    from.SendMessage("Anti-Bot: Verification timed out. Disconnecting...");
                    from.NetState?.Disconnect("Anti-Bot: Verification failed by timing out.");
                }
            });

            _activeChallenges[from] = challenge;

            if (UseTurnstile)
            {
                from.CloseGump<AntiBotTurnstileGump>();
                from.SendGump(new AntiBotTurnstileGump(from, challengeId));
            }
            else
            {
                from.CloseGump<AntiBotGump>();
                from.SendGump(new AntiBotGump(from, challenge.FallbackCode));
            }

            return false;
        }

        public static async Task<bool> VerifyTurnstileToken(string token)
        {
            var formData = new List<KeyValuePair<string, string>>
            {
                new("secret", TurnstileSecretKey),
                new("response", token)
            };

            var response = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(formData)
            );

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TurnstileResponse>(jsonResponse);

            return result?.Success == true;
        }

        internal static void ProcessResponse(Mobile from, int enteredCode, bool cancelled)
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

            if (enteredCode == challenge.FallbackCode)
            {
                from.SendMessage("Anti-Bot: Verification successful!");
                challenge.SuccessCallback?.Invoke();
            }
            else
            {
                from.SendMessage("Anti-Bot: Incorrect number. Disconnecting...");
                from.NetState?.Disconnect("Anti-Bot: Verification failed by incorrect number.");
            }
        }

        internal static async void ProcessTurnstileResponse(Mobile from, string token)
        {
            if (!_activeChallenges.TryGetValue(from, out var challenge))
            {
                return;
            }

            challenge.TimeoutTimer?.Stop();
            _activeChallenges.Remove(from);

            var isValid = await VerifyTurnstileToken(token);

            if (isValid)
            {
                from.SendMessage("Anti-Bot: Verification successful!");
                challenge.SuccessCallback?.Invoke();
            }
            else
            {
                from.SendMessage("Anti-Bot: Verification failed. Disconnecting...");
                from.NetState?.Disconnect("Anti-Bot: Verification failed.");
            }
        }

        public static void ProcessTurnstileVerification(string challengeId, string token)
        {
            Mobile targetMobile = null;
            foreach (var kvp in _activeChallenges)
            {
                if (kvp.Value.ChallengeId == challengeId)
                {
                    targetMobile = kvp.Key;
                    break;
                }
            }

            if (targetMobile != null)
            {
                ProcessTurnstileResponse(targetMobile, token);
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
            if (_activeChallenges.TryGetValue(from, out var challenge))
            {
                challenge.TimeoutTimer?.Stop();
                _activeChallenges.Remove(from);
            }
        }

        private class TurnstileResponse
        {
            public bool Success { get; set; }
        }
    }
}