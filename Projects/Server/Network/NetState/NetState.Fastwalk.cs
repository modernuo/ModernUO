/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: NetState.Fastwalk.cs                                            *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public partial class NetState
    {
        private long _nextRefill = -1;
        private long _tokenCount = Fastwalk.RefillAmount;

        public int Sequence { get; set; }

        public bool RefillFastwalkTokens()
        {
            if (Core.TickCount < _nextRefill || _tokenCount >= Fastwalk.RefillAmount)
            {
                return false;
            }

            WriteConsole($"Refilling {Fastwalk.RefillAmount - _tokenCount} tokens");
            _tokenCount = Fastwalk.RefillAmount;
            _nextRefill = Core.TickCount + Fastwalk.RefillDelay;
            return true;
        }

        public bool RemoveFastwalkToken()
        {
            if (_tokenCount <= 0 && !RefillFastwalkTokens())
            {
                return false;
            }

            WriteConsole("Removing token");
            _tokenCount--;
            return true;
        }
    }
}
