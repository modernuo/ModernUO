/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Pipe.cs                                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Threading;

namespace Server.Network
{
    public class Pipe
    {
        internal readonly byte[] m_Buffer;
        internal volatile uint _writeIdx;
        internal volatile uint _readIdx;

        internal volatile bool _awaitBeginning;
        internal volatile WaitCallback _readerContinuation;

        public PipeWriter Writer { get; }
        public PipeReader Reader { get; }

        public uint Size => (uint)m_Buffer.Length;

        public Pipe(byte[] buffer)
        {
            m_Buffer = buffer;
            _writeIdx = 0;
            _readIdx = 0;

            Writer = new PipeWriter(this);
            Reader = new PipeReader(this);
        }

        public static PipeResult GetPipeResult() => new PipeResult(2);
    }
};
