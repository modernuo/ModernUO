/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BanExemptions.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Net;
using Server.Network.Bans;

namespace Server.Network;

/// <summary>
/// The single answer to "should this ban contribution be dropped?", combining the operator's file allowlist
/// with the one addresses earn by logging in. Owns <see cref="BanChannel.IsExempt"/> so neither source has to
/// know about the other.
/// </summary>
/// <remarks>
/// Suppresses escalation only. Whatever gate reached the verdict has already acted, so the shard is
/// protected either way; what is withheld is pushing the address to an external bouncer.
/// </remarks>
public static class BanExemptions
{
    public static void Configure()
    {
        BanChannel.IsExempt = IsExempt;
    }

    public static bool IsExempt(IPAddress address, string reason) =>
        IsExempt(address, reason, LoginAllowlist.IsExemptFromEscalation);

    /// <summary>
    /// Split for testing. <paramref name="loginAllowlist"/> is stateful — calling it spends a strike — so it
    /// must not be invoked once the answer is already decided.
    /// </summary>
    internal static bool IsExempt(IPAddress address, string reason, System.Func<IPAddress, string, bool> loginAllowlist)
    {
        if (address == null)
        {
            return false;
        }

        // Never suppress an operator's explicit ban. Checked first so it also costs no strike.
        if (!BanReasons.IsBehavioral(reason))
        {
            return false;
        }

        // Deliberate and unconditional, so it wins and must not spend the earned list's strikes.
        if (FileAllowlist.Contains(address))
        {
            return true;
        }

        return loginAllowlist(address, reason);
    }
}
