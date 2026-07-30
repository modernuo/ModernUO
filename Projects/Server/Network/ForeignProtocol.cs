/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ForeignProtocol.cs                                              *
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

namespace Server.Network;

public enum ForeignProtocolKind
{
    None,
    Http,
    Tls,
    Ssh
}

public enum ForeignProtocolMatch
{
    None,

    /// <summary>A prefix matched, but more bytes are needed to be sure.</summary>
    Incomplete,

    Confirmed
}

/// <summary>
/// Identifies traffic that is positively some OTHER protocol (HTTP, TLS, SSH), rather than deciding whether
/// a connection is a good Ultima Online client.
/// </summary>
/// <remarks>
/// The direction matters. A legitimate client with encryption enabled when the shard expects none sends a
/// structurally correct connection whose payload is noise, because <c>LoginEncryption.ClientDecrypt</c> is a
/// byte-for-byte stream XOR: length survives, content does not. So "unreadable" cannot mean "hostile", while
/// "speaks HTTP" safely can. Nothing here assumes arrival framing; see
/// <c>dev-docs/ip-bans-and-allowlists.md</c>.
/// </remarks>
public static class ForeignProtocol
{
    private const int RequiredBytes = 8;
    private const int MaxTlsRecordLength = 16384;

    public static ForeignProtocolMatch Identify(ReadOnlySpan<byte> buffer, out ForeignProtocolKind kind)
    {
        kind = ForeignProtocolKind.None;

        // Too little to match a prefix; the caller's own short-read handling covers it.
        if (buffer.Length < 4)
        {
            return ForeignProtocolMatch.None;
        }

        var candidate = MatchPrefix(buffer);
        if (candidate == ForeignProtocolKind.None)
        {
            return ForeignProtocolMatch.None;
        }

        if (buffer.Length < RequiredBytes)
        {
            return ForeignProtocolMatch.Incomplete;
        }

        if (!Confirm(buffer, candidate))
        {
            return ForeignProtocolMatch.None;
        }

        kind = candidate;
        return ForeignProtocolMatch.Confirmed;
    }

    private static ForeignProtocolKind MatchPrefix(ReadOnlySpan<byte> buffer)
    {
        // TLS handshake record: content type 0x16, major version 3, minor version 0..4 (SSL 3.0 - TLS 1.3).
        if (buffer[0] == 0x16 && buffer[1] == 0x03 && buffer[2] <= 0x04)
        {
            return ForeignProtocolKind.Tls;
        }

        if (StartsWith(buffer, "SSH-"))
        {
            return ForeignProtocolKind.Ssh;
        }

        // HTTP request methods. Four bytes only selects a candidate; Confirm checks the request line.
        if (StartsWith(buffer, "GET ") || StartsWith(buffer, "POST") || StartsWith(buffer, "HEAD") ||
            StartsWith(buffer, "PUT ") || StartsWith(buffer, "OPTI") || StartsWith(buffer, "DELE") ||
            StartsWith(buffer, "CONN") || StartsWith(buffer, "TRAC") || StartsWith(buffer, "PATC"))
        {
            return ForeignProtocolKind.Http;
        }

        return ForeignProtocolKind.None;
    }

    private static bool Confirm(ReadOnlySpan<byte> buffer, ForeignProtocolKind candidate)
    {
        if (candidate == ForeignProtocolKind.Tls)
        {
            // A seed can collide with the 0x16 0x03 0x0? prefix (that is just the address 22.3.x.x), so
            // require a ClientHello inside a plausible record.
            var recordLength = (buffer[3] << 8) | buffer[4];
            return buffer[5] == 0x01 && recordLength is >= 4 and <= MaxTlsRecordLength;
        }

        // A UO client's fifth byte is a packet id (0x80, 0x91, 0xEF), none of them printable, so requiring
        // the request line to continue in ASCII lets a seed that spells "GET " fall through.
        for (var i = 4; i < buffer.Length && i < 16; i++)
        {
            if (!IsPrintableAscii(buffer[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPrintableAscii(byte value) =>
        value is >= 0x20 and <= 0x7E or (byte)'\r' or (byte)'\n' or (byte)'\t';

    private static bool StartsWith(ReadOnlySpan<byte> buffer, string ascii)
    {
        for (var i = 0; i < ascii.Length; i++)
        {
            if (buffer[i] != (byte)ascii[i])
            {
                return false;
            }
        }

        return true;
    }
}
