/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ForeignProtocolTests.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class ForeignProtocolTests
{
    private static ForeignProtocolMatch Identify(byte[] bytes, out ForeignProtocolKind kind) =>
        ForeignProtocol.Identify(bytes, out kind);

    private static byte[] Ascii(string s) => Encoding.ASCII.GetBytes(s);

    [Theory]
    [InlineData("GET / HTTP/1.1\r\n")]
    [InlineData("POST /a HTTP/1.1\r\n")]
    [InlineData("HEAD / HTTP/1.0\r\n")]
    [InlineData("OPTIONS * HTTP/1.1\r\n")]
    [InlineData("CONNECT host:443 HTTP/1.1\r\n")]
    [InlineData("DELETE /x HTTP/1.1\r\n")]
    public void Http_requests_are_identified(string request)
    {
        Assert.Equal(ForeignProtocolMatch.Confirmed, Identify(Ascii(request), out var kind));
        Assert.Equal(ForeignProtocolKind.Http, kind);
    }

    [Fact]
    public void Ssh_banner_is_identified()
    {
        Assert.Equal(ForeignProtocolMatch.Confirmed, Identify(Ascii("SSH-2.0-OpenSSH_9.6"), out var kind));
        Assert.Equal(ForeignProtocolKind.Ssh, kind);
    }

    [Fact]
    public void Tls_client_hello_is_identified()
    {
        // handshake, TLS 1.2 record, length 0x0100, ClientHello
        byte[] hello = [0x16, 0x03, 0x03, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFC];

        Assert.Equal(ForeignProtocolMatch.Confirmed, Identify(hello, out var kind));
        Assert.Equal(ForeignProtocolKind.Tls, kind);
    }

    [Fact]
    public void Tls_prefix_without_a_client_hello_is_not_foreign()
    {
        // A seed can spell 0x16 0x03 0x0? -- the address 22.3.x.x. Byte five is a packet id, not a
        // handshake type, so it must fall through to normal parsing.
        byte[] seedThenLogin = [0x16, 0x03, 0x03, 0x04, 0x00, 0x80, 0x00, 0x00];

        Assert.Equal(ForeignProtocolMatch.None, Identify(seedThenLogin, out var kind));
        Assert.Equal(ForeignProtocolKind.None, kind);
    }

    [Fact]
    public void Seed_that_spells_an_http_method_falls_through()
    {
        // Seed 0x47455420 spells "GET ". The next byte is the 0x80 login packet id, not printable, so this
        // is a real client and must not be flagged.
        byte[] seedThenLogin = [(byte)'G', (byte)'E', (byte)'T', (byte)' ', 0x80, 0x00, 0x3A, 0x00];

        Assert.Equal(ForeignProtocolMatch.None, Identify(seedThenLogin, out _));
    }

    [Theory]
    [InlineData(0x80)] // login request
    [InlineData(0x91)] // game server login
    [InlineData(0xEF)] // new-style seed packet
    public void Ordinary_uo_openings_are_not_foreign(byte secondPacketId)
    {
        byte[] buffer = [0x7F, 0x00, 0x00, 0x01, secondPacketId, 0x00, 0x00, 0x00];

        Assert.Equal(ForeignProtocolMatch.None, Identify(buffer, out _));
    }

    [Fact]
    public void Encrypted_login_is_not_mistaken_for_a_foreign_protocol()
    {
        // A legitimate client with encryption on when the shard expects none. The login cipher is a
        // byte-for-byte XOR, so this is noise of exactly the right length.
        byte[] buffer = [0x7F, 0x00, 0x00, 0x01, 0xC3, 0x9A, 0x04, 0xE1, 0x55, 0xB2];

        Assert.Equal(ForeignProtocolMatch.None, Identify(buffer, out _));
    }

    [Fact]
    public void Prefix_match_without_enough_bytes_waits()
    {
        // Framing is never assumed: "GET " split from its request line must wait.
        Assert.Equal(ForeignProtocolMatch.Incomplete, Identify(Ascii("GET "), out _));
        Assert.Equal(ForeignProtocolMatch.Incomplete, Identify(Ascii("GET /"), out _));
    }

    [Fact]
    public void Too_few_bytes_to_match_a_prefix_is_not_foreign()
    {
        // Under four bytes the caller's own short-read handling applies.
        Assert.Equal(ForeignProtocolMatch.None, Identify(Ascii("GE"), out _));
        Assert.Equal(ForeignProtocolMatch.None, Identify([], out _));
    }
}
