/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: KQueuePollGroup.cs                                              *
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
using System.IO;
using System.Runtime.InteropServices;

namespace Server.Network
{
    public class KQueuePollGroup : IPollGroup
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct kevent
        {
            public IntPtr ident;
            public kqueue_filter filter;
            public kqueue_flags flags;
            public kqueue_fflags fflags;
            public IntPtr data;
            public IntPtr udata;
        }

        [Flags]
        private enum kqueue_filter : short
        {
            READ = -1,
            WRITE = -2,
            AIO = -3,
            VNODE = -4,
            PROC = -5,
            SIGNAL = -6,
            TIMER = -7,
            MACHPORT = -8,
            FS = -9,
            USER = -10,
            UNUSED = -11,
            VM = -12,
            EXCEPT = -15
        }

        [Flags]
        private enum kqueue_flags : ushort
        {
            ADD = 0x0001,
            DELETE = 0x0002,
            ENABLE = 0x0004,
            DISABLE = 0x0008,
            ONESHOT = 0x0010,
            CLEAR = 0x0020,
            RECEIPT = 0x0040,
            DISPATCH = 0x0080,
            UDATA_SPECIFIC = 0x0100,
            DISPATCH2 = (DISPATCH | UDATA_SPECIFIC),
            VANISHED = 0x0200,
            SYSFLAGS = 0xF000,
            FLAG0 = 0x1000,
            FLAG1 = 0x2000,
            EOF = 0x8000,
            ERROR = 0x4000
        }

        [Flags]
        private enum kqueue_fflags : uint
        {
            TRIGGER = 0x01000000,
            FFNOP = 0x00000000,
            FFAND = 0x40000000,
            FFOR = 0x80000000,
            FFCOPY = 0xc0000000,
            FFCTRLMASK = 0xc0000000,
            FFFLAGSMASK = 0x00ffffff,
            LOWAT = 0x00000001,
            DELETE = 0x00000001,
            WRITE = 0x00000002,
            EXTEND = 0x00000004,
            ATTRIB = 0x00000008,
            LINK = 0x00000010,
            RENAME = 0x00000020,
            REVOKE = 0x00000040,
            NONE = 0x00000080,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct timespec
        {
            public long tv_sec;
            public long tv_nsec;

            public timespec(long sec, long nsec)
            {
                tv_sec = sec;
                tv_nsec = nsec;
            }
        }

        private static readonly kevent[] _singleEvent = new kevent[1];
        private static readonly IntPtr _zeroTimeoutPtr;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private static readonly timespec _zeroTimeout;

        static KQueuePollGroup()
        {
            _zeroTimeout = new timespec(0, 0);
            _zeroTimeoutPtr = Marshal.AllocHGlobal(Marshal.SizeOf<timespec>());
            Marshal.StructureToPtr(_zeroTimeout, _zeroTimeoutPtr, false);
        }

        private static class BSD
        {
            [DllImport ("libc", SetLastError = true)]
            public static extern int close (int fd);

            [DllImport("libc", SetLastError = true)]
            public static extern int kqueue();

            [DllImport("libc", SetLastError = true)]
            public static extern int kevent(int kq, kevent[] changelist, int nchanges, [In, Out] kevent[] eventlist, int nevents, IntPtr timeout);

            public static int kevent(
                int kq,
                IntPtr ident,
                kqueue_filter filter,
                kqueue_flags flags,
                kqueue_fflags fflags = 0,
                IntPtr data = default,
                IntPtr udata = default
            )
            {
                _singleEvent[0] = new kevent
                {
                    ident = ident,
                    filter = filter,
                    flags = flags,
                    fflags = fflags,
                    data = data,
                    udata = udata
                };

                var rc = kevent(kq, _singleEvent, 1, null, 0, _zeroTimeoutPtr);
                if (rc != 0)
                {
                    throw new Exception($"kqueue failed to {flags} with error code {Marshal.GetLastWin32Error()}");
                }

                if (_singleEvent[0].flags.HasFlag(kqueue_flags.ERROR))
                {
                    throw new IOException($"kqueue failed to {flags} with error {_singleEvent[0].data}");
                }

                return rc;
            }
        }

        private readonly int _kqueueHndle;

        public KQueuePollGroup()
        {
            _kqueueHndle = BSD.kqueue();

            if (_kqueueHndle == 0)
            {
                throw new Exception("Unable to initialize poll group");
            }
        }

        public void Dispose()
        {
            BSD.close(_kqueueHndle);
            Marshal.FreeHGlobal(_zeroTimeoutPtr);
        }

        public void Add(NetState state)
        {
            var rc = BSD.kevent(
                _kqueueHndle,
                state.Connection.Handle,
                kqueue_filter.READ | kqueue_filter.WRITE,
                kqueue_flags.ADD | kqueue_flags.CLEAR,
                udata: (IntPtr)state._handle
            );

            if (rc != 0)
            {
                throw new Exception($"kevent failed with error code {Marshal.GetLastWin32Error()}");
            }
        }

        public void Remove(NetState state)
        {
            var rc = BSD.kevent(
                _kqueueHndle,
                state.Connection.Handle,
                kqueue_filter.READ | kqueue_filter.WRITE,
                kqueue_flags.DELETE,
                udata: (IntPtr)state._handle
            );

            if (rc != 0)
            {
                throw new Exception($"kevent failed with error code {Marshal.GetLastWin32Error()}");
            }
        }

        private kevent[] _events = new kevent[2048];

        public int Poll(ref NetState[] states)
        {
            if (states.Length > _events.Length)
            {
                var newLength = Math.Max(states.Length, _events.Length + (_events.Length >> 2));
                _events = new kevent[newLength];
            }

            var rc = BSD.kevent(_kqueueHndle, null, 0, _events, _events.Length, _zeroTimeoutPtr);

            if (rc <= 0)
            {
                return rc;
            }

            int count = 0;

            for (int i = 0; i < rc; i++)
            {
                if (((GCHandle)_events[i].udata).Target is not NetState state)
                {
                    continue;
                }

                states[count++] = state;
            }

            return count;
        }
    }
}
