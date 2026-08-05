// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.CompilerServices;

// Suppresses the `.locals init` IL flag across this assembly, so locals and `stackalloc`
// buffers are not zeroed on entry.
//
// This matters most in DequeueRioCompletions, which stackallocs RIORESULT[256] (6 KiB) and
// runs once per game-loop iteration: without this, every call memsets 6 KiB before doing any
// work. Profiling a near-idle ModernUO shard put that memset at ~2.8% of the main thread.
//
// The flag is baked into the IL at compile time and does NOT cross assembly boundaries, so
// a consumer declaring it (ModernUO does) has no effect here — it has to be declared in this
// assembly.
//
// Every stackalloc here writes what it passes on: DequeueRioCompletions reads back only the
// entries RIODequeueCompletion filled, and LinuxEpollGroup zeroes its epoll_event buffers
// explicitly. Struct locals built with `new T { ... }` emit `initobj` and are unaffected, as
// are the NativeMemory.AllocZeroed pools.
[module: SkipLocalsInit]
