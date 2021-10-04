/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: EPollGroup.cs                                                   *
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
using System.Runtime.InteropServices;

namespace Server.Network
{
    public sealed class EPollGroup : IPollGroup
    {
        [Flags]
        private enum epoll_flags
        {
            NONE = 0,
            CLOEXEC = 0x02000000,
            NONBLOCK = 0x04000,
        }

        [Flags]
        private enum epoll_events : uint
        {
            EPOLLIN = 0x001,
            EPOLLPRI = 0x002,
            EPOLLOUT = 0x004,
            EPOLLRDNORM = 0x040,
            EPOLLRDBAND = 0x080,
            EPOLLWRNORM = 0x100,
            EPOLLWRBAND = 0x200,
            EPOLLMSG = 0x400,
            EPOLLERR = 0x008,
            EPOLLHUP = 0x010,
            EPOLLRDHUP = 0x2000,
            EPOLLONESHOT = 1 << 30,
            EPOLLET = unchecked((uint)(1 << 31))
        }

        private enum epoll_op
        {
            EPOLL_CTL_ADD = 1,
            EPOLL_CTL_DEL = 2,
            EPOLL_CTL_MOD = 3,
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct epoll_data
        {
            [FieldOffset(0)]
            public int fd;

            [FieldOffset(0)]
            public IntPtr ptr;

            [FieldOffset(0)]
            public uint u32;

            [FieldOffset(0)]
            public ulong u64;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct epoll_event
        {
            public epoll_events events;
            public epoll_data data;
        }

        private static class Windows
        {
            [DllImport("wepoll.dll", SetLastError = true)]
            public static extern int epoll_create1(epoll_flags flags);

            [DllImport("wepoll.dll", SetLastError = true)]
            public static extern int epoll_close(int epfd);

            [DllImport("wepoll.dll", SetLastError = true)]
            public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event ee);

            [DllImport("wepoll.dll", SetLastError = true)]
            public static extern int epoll_wait(int epfd, [In, Out] epoll_event[] ee, int maxevents, int timeout);
        }

        private static class Linux
        {
            [DllImport("libc", SetLastError = true)]
            public static extern int epoll_create1(epoll_flags flags);

            [DllImport("libc", SetLastError = true)]
            public static extern int epoll_close(int epfd);

            [DllImport("libc", SetLastError = true)]
            public static extern int epoll_ctl(int epfd, epoll_op op, int fd, ref epoll_event ee);

            [DllImport("libc", SetLastError = true)]
            public static extern int epoll_wait(int epfd, [In, Out] epoll_event[] ee, int maxevents, int timeout);
        }

        private readonly int _epHndle;

        public EPollGroup()
        {
            _epHndle = Core.IsWindows ? Windows.epoll_create1(epoll_flags.NONE) : Linux.epoll_create1(epoll_flags.NONE);

            if (_epHndle == 0)
            {
                throw new Exception("Unable to initialize poll group");
            }
        }

        public void Dispose()
        {
            if (Core.IsWindows)
            {
                Windows.epoll_close(_epHndle);
            }
            else
            {
                Linux.epoll_close(_epHndle);
            }
        }

        public void Add(NetState state)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR
            };

            ev.data.ptr = (IntPtr)state._handle;

            var rc = Core.IsWindows ?
                Windows.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)state.Connection.Handle, ref ev) :
                Linux.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_ADD, (int)state.Connection.Handle, ref ev);


            if (rc != 0)
            {
                throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
            }
        }

        public void Remove(NetState state)
        {
            var ev = new epoll_event
            {
                events = epoll_events.EPOLLIN | epoll_events.EPOLLERR,
            };
            ev.data.ptr = (IntPtr)state._handle;

            var rc = Core.IsWindows ?
                Windows.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)state.Connection.Handle, ref ev) :
                Linux.epoll_ctl(_epHndle, epoll_op.EPOLL_CTL_DEL, (int)state.Connection.Handle, ref ev);

            if (rc != 0)
            {
                throw new Exception($"epoll_ctl failed with error code {Marshal.GetLastWin32Error()}");
            }
        }

        private epoll_event[] _events = new epoll_event[2048];

        public int Poll(ref NetState[] states)
        {
            if (states.Length > _events.Length)
            {
                var newLength = Math.Max(states.Length, _events.Length + (_events.Length >> 2));
                _events = new epoll_event[newLength];
            }

            var rc = Core.IsWindows ?
                Windows.epoll_wait(_epHndle, _events, states.Length, 0) :
                Linux.epoll_wait(_epHndle, _events, states.Length, 0);

            if (rc <= 0)
            {
                return rc;
            }

            int count = 0;

            for (int i = 0; i < rc; i++)
            {
                if (((GCHandle)_events[i].data.ptr).Target is not NetState state)
                {
                    continue;
                }

                states[count++] = state;
            }

            return count;
        }

    }
}
