/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CrowdSecBans.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network.Bans;
using Server.Network.Bans.CrowdSec;

namespace Server.Misc;

/// <summary>
/// Registers the CrowdSec ban reporter with the Core <see cref="BanChannel"/> during the Configure sweep.
/// Registration is unconditional: the reporter self-disables when <c>crowdsec.json</c> has no credentials
/// (see <c>CrowdSecSettings.ReportingEnabled</c>), so no config gate is needed here.
/// </summary>
public static class CrowdSecBans
{
    public static void Configure()
    {
        BanChannel.Register(new CrowdSecReporter());
    }
}
