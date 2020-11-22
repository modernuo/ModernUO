/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Fastwalk.cs                                                     *
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

namespace Server.Network
{
    public static class Fastwalk
    {
        public static AccessLevel ExemptionLevel { get; private set; } = AccessLevel.Counselor;
        public static int RefillAmount { get; private set; } = 6;
        private static long _refillDelay = 540; // milliseconds

        public static long RefillDelay
        {
            get => _refillDelay;
            set => _refillDelay = Math.Clamp(value, 10, 1000);
        }

        public static void Configure()
        {
            ExemptionLevel = ServerConfiguration.GetOrUpdateSetting("netstate.fastwalk.exemptionLevel", ExemptionLevel);
            RefillAmount = ServerConfiguration.GetOrUpdateSetting("netstate.fastwalk.refillAmount", RefillAmount);
            RefillDelay = ServerConfiguration.GetOrUpdateSetting("netstate.fastwalk.refillDelay", _refillDelay);
            // Testing
            // ExemptionLevel = (AccessLevel)((int)AccessLevel.Owner + 1);
        }
    }
}
