// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

namespace System.Network.EPoll;

// The kernel epoll_event layout (12 bytes packed on x86_64, 16 bytes aligned
// elsewhere) is handled directly via raw byte offsets in LinuxEpollGroup — see
// EpollCtlMod and PollAndExecute — so no managed struct is defined for it.

/// <summary>
/// epoll event flags.
/// </summary>
[Flags]
internal enum epoll_events : uint
{
    EPOLLIN      = 0x001,
    EPOLLPRI     = 0x002,
    EPOLLOUT     = 0x004,
    EPOLLERR     = 0x008,
    EPOLLHUP     = 0x010,
    EPOLLRDNORM  = 0x040,
    EPOLLRDBAND  = 0x080,
    EPOLLWRNORM  = 0x100,
    EPOLLWRBAND  = 0x200,
    EPOLLRDHUP   = 0x2000,
    EPOLLET      = 0x80000000,
    EPOLLONESHOT = 0x40000000,
}

/// <summary>
/// epoll_ctl operations.
/// </summary>
internal enum epoll_op : int
{
    EPOLL_CTL_ADD = 1,
    EPOLL_CTL_DEL = 2,
    EPOLL_CTL_MOD = 3,
}
