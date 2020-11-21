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

using System.Collections.Generic;
using System.Linq;

namespace Server.Network
{
    public partial class NetState
    {
        private const uint _minKeyValue = 1;
        private const uint _maxKeyValue = uint.MaxValue - 1;
        private const int _maxKeys = 6; // This is not configurable because we are limited by client packets

        private long _nextRefill;
        private HashSet<uint> _moveKeys = new HashSet<uint>(_maxKeys);

        public int Sequence { get; set; }

        public void InitializeKeys()
        {
            while (_moveKeys.Count < _maxKeys)
            {
                _moveKeys.Add(Utility.RandomMinMax(_minKeyValue, _maxKeyValue));
            }

            this.SendInitialFaswalkStack(_moveKeys.ToArray());
            _nextRefill = Core.TickCount + Fastwalk.RefillDelay;
        }

        public void RefillKeys()
        {
            if (Core.TickCount <= _nextRefill || _moveKeys.Count >= _maxKeys)
            {
                return;
            }

            while (AddKey(out var addedKey))
            {
                this.SendFastwalkStackKey(addedKey);
            }
        }

        private bool AddKey(out uint addedKey)
        {
            if (_moveKeys.Count >= _maxKeys)
            {
                addedKey = 0;
                return false;
            }

            do
            {
                addedKey = Utility.RandomMinMax(_minKeyValue, _maxKeyValue);
            } while (_moveKeys.Add(addedKey));

            return true;
        }

        public bool RemoveKey(uint key) => key != 0 && _moveKeys.Remove(key);
    }
}
