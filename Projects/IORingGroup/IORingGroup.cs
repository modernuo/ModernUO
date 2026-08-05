// SPDX-License-Identifier: BSD-3-Clause
// Copyright (c) 2025, ModernUO

using System.Runtime.InteropServices;

namespace System.Network;

/// <summary>
/// Factory for creating platform-appropriate IIORingGroup implementations.
/// </summary>
public static class IORingGroup
{
    /// <summary>
    /// Default size for the submission and completion queues.
    /// </summary>
    public const int DefaultQueueSize = 4096;

    /// <summary>
    /// Default maximum connections for Windows RIO mode.
    /// </summary>
    public const int DefaultMaxConnections = 1024;

    /// <summary>
    /// Creates an IIORingGroup instance appropriate for the current platform.
    /// </summary>
    /// <param name="queueSize">Size of the submission and completion queues. Must be power of 2.</param>
    /// <param name="maxConnections">Maximum concurrent connections. Determines external buffer capacity (maxConnections * 2).</param>
    /// <returns>Platform-specific IIORingGroup implementation.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is not supported.</exception>
    public static IIORingGroup Create(
        int queueSize = DefaultQueueSize,
        int maxConnections = DefaultMaxConnections,
        int maxOutstandingSends = 1
    )
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return CreateWindowsRing(maxConnections, maxOutstandingSends);
        }

        // Other backends ignore maxOutstandingSends and report 1 via MaxOutstandingSendsPerSocket:
        // they complete sends on copy, so extra sends in flight gain nothing and cost ordering
        // guarantees (io_uring) or correctness (epoll/kqueue hold one pending send per connection).

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return CreateLinuxRing(queueSize, maxConnections);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return CreateDarwinRing(queueSize, maxConnections);
        }

        throw new PlatformNotSupportedException(
            $"IORingGroup is not supported on platform: {RuntimeInformation.OSDescription}");
    }

    private static IIORingGroup CreateWindowsRing(int maxConnections, int maxOutstandingSends) =>
        new Windows.WindowsManagedRIOGroup(maxConnections, maxOutstandingSends);

    private static IIORingGroup CreateLinuxRing(int queueSize, int maxConnections)
    {
        if (IORing.LinuxIORingGroup.IsAvailable())
        {
            return new IORing.LinuxIORingGroup(queueSize, maxConnections);
        }

        // Fallback to epoll when io_uring is unavailable
        return new EPoll.LinuxEpollGroup(queueSize, maxConnections);
    }

    /// <summary>
    /// Creates an epoll-based IIORingGroup for Linux explicitly.
    /// Useful for testing or when io_uring should be bypassed.
    /// </summary>
    /// <param name="queueSize">Size of the submission and completion queues. Must be power of 2.</param>
    /// <param name="maxConnections">Maximum concurrent connections.</param>
    /// <returns>Epoll-based IIORingGroup implementation.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if not running on Linux.</exception>
    public static IIORingGroup CreateLinuxEpoll(int queueSize = DefaultQueueSize, int maxConnections = DefaultMaxConnections)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new PlatformNotSupportedException("epoll requires Linux");
        }

        return new EPoll.LinuxEpollGroup(queueSize, maxConnections);
    }

    private static Darwin.DarwinIORingGroup CreateDarwinRing(int queueSize, int maxConnections) =>
        new Darwin.DarwinIORingGroup(queueSize, maxConnections);

    /// <summary>
    /// Returns true if this is a power of 2 (used to validate queue size).
    /// </summary>
    internal static bool IsPowerOfTwo(int x) => x > 0 && (x & (x - 1)) == 0;
}
