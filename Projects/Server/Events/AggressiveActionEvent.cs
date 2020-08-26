/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: AggressiveActionEvent.cs                                        *
 * Created: 2020/04/11 - Updated: 2020/04/11                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server
{
    public class AggressiveActionEventArgs : EventArgs
    {
        private static readonly Queue<AggressiveActionEventArgs> m_Pool = new Queue<AggressiveActionEventArgs>();

        private AggressiveActionEventArgs(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            Aggressed = aggressed;
            Aggressor = aggressor;
            Criminal = criminal;
        }

        public Mobile Aggressed { get; private set; }

        public Mobile Aggressor { get; private set; }

        public bool Criminal { get; private set; }

        public static AggressiveActionEventArgs Create(Mobile aggressed, Mobile aggressor, bool criminal)
        {
            AggressiveActionEventArgs args;

            if (m_Pool.Count > 0)
            {
                args = m_Pool.Dequeue();

                args.Aggressed = aggressed;
                args.Aggressor = aggressor;
                args.Criminal = criminal;
            }
            else
            {
                args = new AggressiveActionEventArgs(aggressed, aggressor, criminal);
            }

            return args;
        }

        public void Free()
        {
            m_Pool.Enqueue(this);
        }
    }

    public static partial class EventSink
    {
        public static event Action<AggressiveActionEventArgs> AggressiveAction;
        public static void InvokeAggressiveAction(AggressiveActionEventArgs e) => AggressiveAction?.Invoke(e);
    }
}
