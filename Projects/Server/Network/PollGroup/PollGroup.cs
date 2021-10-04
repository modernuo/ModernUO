/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PollGroup.cs                                                    *
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
    public interface IPollGroup : IDisposable
    {
        void Add(NetState state);
        void Remove(NetState state);
        int Poll(ref NetState[] states);
    }

    public static class PollGroup
    {
        public static IPollGroup Create()
        {
            if (Core.IsBSD)
            {
                return new KQueuePollGroup();
            }

            return new EPollGroup();
        }
    }
}
