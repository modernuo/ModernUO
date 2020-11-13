/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MovementRecord.cs                                               *
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

namespace Server.Mobiles
{
    public class MovementRecord
    {
        private static readonly Queue<MovementRecord> m_InstancePool = new Queue<MovementRecord>();
        private long m_End;

        private MovementRecord(long end) => m_End = end;

        public static MovementRecord NewInstance(long end)
        {
            MovementRecord r;

            if (m_InstancePool.Count > 0)
            {
                r = m_InstancePool.Dequeue();

                r.m_End = end;
            }
            else
            {
                r = new MovementRecord(end);
            }

            return r;
        }

        public bool Expired()
        {
            var v = Core.TickCount - m_End >= 0;

            if (v)
            {
                m_InstancePool.Enqueue(this);
            }

            return v;
        }
    }
}
