/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BanReasons.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network.Bans;

/// <summary>The <c>reason</c> slugs contributed through <see cref="BanChannel"/>. Policy keys off these.</summary>
public static class BanReasons
{
    /// <summary>An operator banned this address explicitly. Never exempt, never auto-denied.</summary>
    public const string Manual = "manual";

    public const string RateLimit = "rate-limit";

    /// <summary>Matched the reputation blocklist. Enforced by its own filter, not by behaviour.</summary>
    public const string Blocklist = "blocklist";

    /// <summary>Reaped without ever sending a byte.</summary>
    public const string SilentConnect = "silent-connect";

    /// <summary>Opened with a zero seed, which no real client sends.</summary>
    public const string InvalidSeed = "invalid-seed";

    /// <summary>Positively identified as another protocol entirely. See <see cref="ForeignProtocol"/>.</summary>
    public const string ForeignProtocol = "foreign-protocol";

    /// <summary>
    /// Verdicts the shard reached by watching the connection. Only these may be exempted, and only these feed
    /// the local denylist.
    /// </summary>
    /// <remarks>
    /// Opt-in rather than "everything except <see cref="Manual"/>", so a reason added later escalates normally
    /// instead of silently inheriting an exemption.
    /// </remarks>
    public static bool IsBehavioral(string reason) =>
        reason is RateLimit or SilentConnect or InvalidSeed or ForeignProtocol;
}
