/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: MessagePumpService.cs                                           *
 * Created: 2020/04/12 - Updated: 2020/04/12                             *
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

using System.Buffers;
using System.Collections.Concurrent;

namespace Server.Network
{
    public interface IMessagePumpService
    {
        void QueueWork(NetState ns, IMemoryOwner<byte> memOwner, int length, OnPacketReceive onReceive);
        void DoWork();
    }

    public class MessagePumpService : IMessagePumpService
    {
        private readonly ConcurrentQueue<Work> m_WorkQueue = new ConcurrentQueue<Work>();

        public void QueueWork(NetState ns, IMemoryOwner<byte> memOwner, int length, OnPacketReceive onReceive)
        {
            m_WorkQueue.Enqueue(new Work(ns, memOwner, length, onReceive));
            Core.Set();
        }

        public void DoWork()
        {
            var count = 0;
            while (!m_WorkQueue.IsEmpty && count++ < 250)
            {
                if (!m_WorkQueue.TryDequeue(out var work))
                    break;

                var seq = new ReadOnlySequence<byte>(work.MemoryOwner.Memory.Slice(0, work.Length));
                work.OnReceive(work.State, new PacketReader(seq));
                work.MemoryOwner.Dispose();
            }
        }

        private class Work
        {
            public readonly int Length;
            public readonly IMemoryOwner<byte> MemoryOwner;
            public readonly OnPacketReceive OnReceive;
            public readonly NetState State;

            public Work(NetState ns, IMemoryOwner<byte> memOwner, int length, OnPacketReceive onReceive)
            {
                State = ns;
                MemoryOwner = memOwner;
                OnReceive = onReceive;
                Length = length;
            }
        }
    }
}
