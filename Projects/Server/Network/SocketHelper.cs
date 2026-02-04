/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SocketHelper.cs                                                 *
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
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Server.Network;

/// <summary>
/// Platform-specific socket utilities for working with raw socket handles.
/// </summary>
public static partial class SocketHelper
{
    /// <summary>
    /// Gets the remote IP address from a socket handle.
    /// </summary>
    /// <param name="socket">The socket handle.</param>
    /// <returns>The remote IP address, or null if unable to retrieve.</returns>
    public static IPAddress GetRemoteAddress(nint socket)
    {
        if (socket is 0 or -1)
        {
            return null;
        }

        try
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetRemoteAddressWindows(socket)
                : GetRemoteAddressUnix(socket);
        }
        catch
        {
            return null;
        }
    }

    private static unsafe IPAddress GetRemoteAddressWindows(nint socket)
    {
        Span<byte> buffer = stackalloc byte[128];
        var len = buffer.Length;

        fixed (byte* ptr = buffer)
        {
            if (getpeername(socket, ptr, ref len) != 0)
            {
                return null;
            }
        }

        return ParseSockAddr(buffer[..len]);
    }

    private static unsafe IPAddress GetRemoteAddressUnix(nint socket)
    {
        Span<byte> buffer = stackalloc byte[128];
        var len = (uint)buffer.Length;

        fixed (byte* ptr = buffer)
        {
            if (getpeername_unix(socket, ptr, ref len) != 0)
            {
                return null;
            }
        }

        return ParseSockAddr(buffer[..(int)len]);
    }

    /// <summary>
    /// Gets the local endpoint from a socket handle.
    /// </summary>
    /// <param name="socket">The socket handle.</param>
    /// <returns>The local endpoint, or null if unable to retrieve.</returns>
    public static IPEndPoint GetLocalEndPoint(nint socket)
    {
        if (socket is 0 or -1)
        {
            return null;
        }

        try
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetLocalEndPointWindows(socket)
                : GetLocalEndPointUnix(socket);
        }
        catch
        {
            return null;
        }
    }

    private static unsafe IPEndPoint GetLocalEndPointWindows(nint socket)
    {
        Span<byte> buffer = stackalloc byte[128];
        var len = buffer.Length;

        fixed (byte* ptr = buffer)
        {
            if (getsockname(socket, ptr, ref len) != 0)
            {
                return null;
            }
        }

        return ParseSockAddrEndPoint(buffer[..len]);
    }

    private static unsafe IPEndPoint GetLocalEndPointUnix(nint socket)
    {
        Span<byte> buffer = stackalloc byte[128];
        var len = (uint)buffer.Length;

        fixed (byte* ptr = buffer)
        {
            if (getsockname_unix(socket, ptr, ref len) != 0)
            {
                return null;
            }
        }

        return ParseSockAddrEndPoint(buffer[..(int)len]);
    }

    private static IPAddress ParseSockAddr(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 2)
        {
            return null;
        }

        // macOS/BSD: sockaddr has sin_len (1 byte) + sin_family (1 byte)
        // Linux: sockaddr has sa_family (2 bytes)
        var isBsd = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        var family = isBsd
            ? (AddressFamily)buffer[1]  // BSD: family is second byte
            : (AddressFamily)BitConverter.ToInt16(buffer);  // Linux: family is first 2 bytes

        if (family == AddressFamily.InterNetwork && buffer.Length >= 8)
        {
            // IPv4: family (2) + port (2) + addr (4)
            return new IPAddress(buffer.Slice(4, 4));
        }

        if (family == AddressFamily.InterNetworkV6 && buffer.Length >= 28)
        {
            // IPv6: family (2) + port (2) + flowinfo (4) + addr (16) + scope (4)
            return new IPAddress(buffer.Slice(8, 16));
        }

        return null;
    }

    private static IPEndPoint ParseSockAddrEndPoint(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 4)
        {
            return null;
        }

        // macOS/BSD: sockaddr has sin_len (1 byte) + sin_family (1 byte)
        // Linux: sockaddr has sa_family (2 bytes)
        var isBsd = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        var family = isBsd
            ? (AddressFamily)buffer[1]  // BSD: family is second byte
            : (AddressFamily)BitConverter.ToInt16(buffer);  // Linux: family is first 2 bytes

        // Port is in network byte order (big-endian), at offset 2 on both platforms
        var port = (buffer[2] << 8) | buffer[3];

        if (family == AddressFamily.InterNetwork && buffer.Length >= 8)
        {
            // IPv4: family (2) + port (2) + addr (4)
            return new IPEndPoint(new IPAddress(buffer.Slice(4, 4)), port);
        }

        if (family == AddressFamily.InterNetworkV6 && buffer.Length >= 28)
        {
            // IPv6: family (2) + port (2) + flowinfo (4) + addr (16) + scope (4)
            return new IPEndPoint(new IPAddress(buffer.Slice(8, 16)), port);
        }

        return null;
    }

    // Windows getpeername
    [LibraryImport("ws2_32.dll", SetLastError = true)]
    private static unsafe partial int getpeername(nint s, byte* name, ref int namelen);

    // Unix/Linux getpeername
    [LibraryImport("libc", EntryPoint = "getpeername", SetLastError = true)]
    private static unsafe partial int getpeername_unix(nint sockfd, byte* addr, ref uint addrlen);

    // Windows getsockname
    [LibraryImport("ws2_32.dll", SetLastError = true)]
    private static unsafe partial int getsockname(nint s, byte* name, ref int namelen);

    // Unix/Linux getsockname
    [LibraryImport("libc", EntryPoint = "getsockname", SetLastError = true)]
    private static unsafe partial int getsockname_unix(nint sockfd, byte* addr, ref uint addrlen);
}
