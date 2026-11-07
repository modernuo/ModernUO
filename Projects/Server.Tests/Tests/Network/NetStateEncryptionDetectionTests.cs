using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class NetStateEncryptionDetectionTests
{
    private const int LoginLength = 62;
    private const int GameLoginLength = 65;

    private static readonly ClientVersion ModernVersion = new(7, 0, 106, 21);

    private static readonly FieldInfo ModeField =
        typeof(EncryptionManager).GetField("_mode", BindingFlags.NonPublic | BindingFlags.Static);

    private static byte[] _accountLogin;
    private static byte[] _gameLogin;

    // Handlers see the packet without its id byte
    private static void CaptureAccountLogin(NetState state, SpanReader reader) => _accountLogin = reader.Buffer.ToArray();

    private static void CaptureGameLogin(NetState state, SpanReader reader) => _gameLogin = reader.Buffer.ToArray();

    // Stands in for the UOContent handlers so the test sees exactly the bytes the parser delivered
    private sealed unsafe class Harness : IDisposable
    {
        private readonly object _previousMode;
        private readonly NetTestHelpers.HandlerRegistration _login;
        private readonly NetTestHelpers.HandlerRegistration _game;

        public Harness(EncryptionMode mode)
        {
            _accountLogin = null;
            _gameLogin = null;
            _previousMode = ModeField.GetValue(null);
            ModeField.SetValue(null, mode);
            _login = NetTestHelpers.RegisterHandler(0x80, LoginLength, &CaptureAccountLogin);
            _game = NetTestHelpers.RegisterHandler(0x91, GameLoginLength, &CaptureGameLogin);
        }

        public void Dispose()
        {
            _game.Dispose();
            _login.Dispose();
            ModeField.SetValue(null, _previousMode);
        }
    }

    private static byte[] AccountLogin(string username = "user", string password = "pass")
    {
        var packet = new byte[LoginLength];
        packet[0] = 0x80;
        Encoding.ASCII.GetBytes(username).CopyTo(packet, 1);
        Encoding.ASCII.GetBytes(password).CopyTo(packet, 31);
        packet[61] = 0x5D;
        return packet;
    }

    private static byte[] GameLogin(uint authId)
    {
        var packet = new byte[GameLoginLength];
        packet[0] = 0x91;
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(1), authId);
        Encoding.ASCII.GetBytes("user").CopyTo(packet, 5);
        Encoding.ASCII.GetBytes("pass").CopyTo(packet, 35);
        return packet;
    }

    // Both ciphers XOR the stream with a keystream that does not depend on the data, so the client's
    // encryption is the server's decryption run over the plaintext
    private static byte[] LoginEncrypt(byte[] plain, uint seed, LoginKeys keys)
    {
        var cipher = (byte[])plain.Clone();
        new LoginEncryption(seed, keys).ClientDecrypt(cipher);
        return cipher;
    }

    private static byte[] GameEncrypt(byte[] plain, uint seed)
    {
        var cipher = (byte[])plain.Clone();
        new GameEncryption(seed).ClientDecrypt(cipher);
        return cipher;
    }

    // An auth id whose encrypted 0x91 starts with the given byte
    private static uint FindGameSeed(Func<byte, bool> firstByte)
    {
        for (var seed = 0x12345678u; ; seed++)
        {
            if (firstByte(GameEncrypt(GameLogin(seed), seed)[0]))
            {
                return seed;
            }
        }
    }

    private static bool IsUnambiguous(byte b) => b is not (0x80 or 0x91);

    private static NetState Connect(NetState.ProtocolState state, uint seed, ClientVersion version, out Socket client)
    {
        var ns = PacketTestUtilities.CreateTestNetState(out client);
        ns._protocolState = state;
        ns.Seed = (int)seed;
        ns.Seeded = true;
        ns.Version = version;
        return ns;
    }

    private static void AssertAccountLogin(NetState ns, byte[] plain, bool encrypted)
    {
        NetTestHelpers.SliceUntil(() => _accountLogin != null || ns._protocolState == NetState.ProtocolState.Error);

        Assert.Equal(plain[1..], _accountLogin);
        Assert.Null(_gameLogin);
        Assert.Equal(NetState.ProtocolState.LoginServer_AwaitingServerSelect, ns._protocolState);
        Assert.Equal(encrypted, ns.Encryption is LoginEncryption);
        Assert.True(ns.Running);
    }

    private static void AssertGameLogin(NetState ns, byte[] plain, bool encrypted)
    {
        NetTestHelpers.SliceUntil(
            () => _gameLogin != null || _accountLogin != null || ns._protocolState == NetState.ProtocolState.Error
        );

        Assert.Null(_accountLogin);
        Assert.Equal(plain[1..], _gameLogin);
        Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
        Assert.Equal(encrypted, ns.Encryption is GameEncryption);
        Assert.True(ns.Running);
    }

    [Fact]
    public void EncryptedLogin_WhoseCiphertextStartsWith0x80_IsDecrypted()
    {
        // The first login keystream byte is the seed's low byte ^ 0xAA
        const uint seed = 0x0A0B0CAA;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin();
        var cipher = LoginEncrypt(plain, seed, LoginKeys.GetKeys(ModernVersion));
        Assert.Equal(0x80, cipher[0]);

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, seed, ModernVersion, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedLogin_WhoseCiphertextStartsWith0x80_IsDecrypted_WhenOnlyEncryptedIsAllowed()
    {
        const uint seed = 0x0A0B0CAA;
        using var harness = new Harness(EncryptionMode.Encrypted);
        var plain = AccountLogin();
        var cipher = LoginEncrypt(plain, seed, LoginKeys.GetKeys(ModernVersion));

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, seed, ModernVersion, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_WhoseCiphertextStartsWith0x91_IsDecrypted()
    {
        using var harness = new Harness(EncryptionMode.Both);
        var seed = FindGameSeed(b => b == 0x91);
        var plain = GameLogin(seed);
        var cipher = GameEncrypt(plain, seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher);
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_WhoseCiphertextStartsWith0x80_IsNotRoutedToAccountLogin()
    {
        using var harness = new Harness(EncryptionMode.Both);
        var seed = FindGameSeed(b => b == 0x80);
        var plain = GameLogin(seed);
        var cipher = GameEncrypt(plain, seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher);
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_SplitAfter62Bytes_IsNotRoutedToAccountLogin()
    {
        using var harness = new Harness(EncryptionMode.Both);
        var seed = FindGameSeed(IsUnambiguous);
        var plain = GameLogin(seed);
        var cipher = GameEncrypt(plain, seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher.AsSpan(0, LoginLength));
            NetTestHelpers.SliceUntil(
                () => ns._socket.RecvBuffer.ReadableBytes == LoginLength || ns._protocolState == NetState.ProtocolState.Error
            );
            NetState.Slice();

            Assert.Equal(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, ns._protocolState);
            Assert.True(ns.Running);

            client.Send(cipher.AsSpan(LoginLength));
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Both)]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void PlainLogin_IsHandled(EncryptionMode mode)
    {
        using var harness = new Harness(mode);
        var plain = AccountLogin();

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, 0x0100007F, ModernVersion, out var client);
        try
        {
            client.Send(plain);
            AssertAccountLogin(ns, plain, encrypted: false);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void PlainLogin_ThatFailsTheDecryptionTrial_IsDeliveredUnchanged()
    {
        // A 30-character password leaves no NUL at byte 60, so the structural test fails and the
        // decryption trial runs. With a 0xAA low seed byte the first keystream byte is 0, so the trial
        // gets past byte 0 before failing; the plaintext fallback must still see the original bytes.
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin(password: new string('p', 30));

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, 0x0A0B0CAA, ModernVersion, out var client);
        try
        {
            client.Send(plain);
            AssertAccountLogin(ns, plain, encrypted: false);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void PlainLogin_WhenOnlyEncryptedIsAllowed_IsRejected()
    {
        using var harness = new Harness(EncryptionMode.Encrypted);

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, 0x0100007F, ModernVersion, out var client);
        try
        {
            client.Send(AccountLogin());
            NetTestHelpers.SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);
            Assert.Null(_accountLogin);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedLogin_IsDecrypted()
    {
        const uint seed = 0x0100007F;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin();
        var cipher = LoginEncrypt(plain, seed, LoginKeys.GetKeys(ModernVersion));
        Assert.True(IsUnambiguous(cipher[0]));

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, seed, ModernVersion, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void EncryptedLogin_WhenEncryptedIsNotAllowed_IsRejected(EncryptionMode mode)
    {
        const uint seed = 0x0100007F;
        using var harness = new Harness(mode);
        var cipher = LoginEncrypt(AccountLogin(), seed, LoginKeys.GetKeys(ModernVersion));

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, seed, ModernVersion, out var client);
        try
        {
            client.Send(cipher);
            NetTestHelpers.SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);
            Assert.Null(_accountLogin);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Both)]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void PlainGameLogin_IsHandled(EncryptionMode mode)
    {
        const uint seed = 0x12345678;
        using var harness = new Harness(mode);
        var plain = GameLogin(seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(plain);
            AssertGameLogin(ns, plain, encrypted: false);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_IsDecrypted()
    {
        using var harness = new Harness(EncryptionMode.Both);
        var seed = FindGameSeed(IsUnambiguous);
        var plain = GameLogin(seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(GameEncrypt(plain, seed));
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Both)]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void PlainLogin_OnTheGameConnection_IsHandledWithNothingAfterIt(EncryptionMode mode)
    {
        // Pre-0xEF clients seed the login server with a raw 4-byte seed, so their 0x80 arrives in the
        // game-connection state and nothing follows it until the server answers
        using var harness = new Harness(mode);
        var plain = AccountLogin();

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, 0x0100007F, null, out var client);
        try
        {
            client.Send(plain);
            AssertAccountLogin(ns, plain, encrypted: false);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedLogin_WhoseCiphertextStartsWithAnInfoPacketId_IsDecrypted()
    {
        // 0x80 ^ (0x97 ^ 0xAA) == 0xBD, the client version packet
        const uint seed = 0x0A0B0C97;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin();
        var cipher = LoginEncrypt(plain, seed, LoginKeys.GetKeys(ModernVersion));
        Assert.Equal(0xBD, cipher[0]);

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, seed, ModernVersion, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_WhoseCiphertextStartsWithAnInfoPacketId_IsDecrypted()
    {
        using var harness = new Harness(EncryptionMode.Both);
        var seed = FindGameSeed(IncomingPackets.IsInfoPacket);
        var plain = GameLogin(seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(GameEncrypt(plain, seed));
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void InfoPacket_BeforeLogin_IsNotDispatched(EncryptionMode mode)
    {
        using var harness = new Harness(mode);
        using var ping = NetTestHelpers.RegisterPing();

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, 0x0100007F, ModernVersion, out var client);
        try
        {
            client.Send([NetTestHelpers.PingPacketId, 0x42, ..AccountLogin()]);
            NetTestHelpers.SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);

            Assert.Equal(0, NetTestHelpers.LastPing);
            Assert.Null(_accountLogin);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Both)]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void InfoPacket_AfterLogin_IsDispatched(EncryptionMode mode)
    {
        using var harness = new Harness(mode);
        using var ping = NetTestHelpers.RegisterPing();
        var plain = AccountLogin();

        var ns = Connect(NetState.ProtocolState.LoginServer_AwaitingLogin, 0x0100007F, ModernVersion, out var client);
        try
        {
            client.Send([..plain, NetTestHelpers.PingPacketId, 0x42]);
            NetTestHelpers.SliceUntil(() => NetTestHelpers.LastPing == 0x42 || ns._protocolState == NetState.ProtocolState.Error);

            AssertAccountLogin(ns, plain, encrypted: false);
            Assert.Equal(0x42, NetTestHelpers.LastPing);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void EncryptedGameLogin_WhenOnlyEncryptedIsAllowed_IsDecrypted()
    {
        using var harness = new Harness(EncryptionMode.Encrypted);
        var seed = FindGameSeed(b => b == 0x91);
        var plain = GameLogin(seed);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(GameEncrypt(plain, seed));
            AssertGameLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void PlainGameLogin_WhenOnlyEncryptedIsAllowed_IsRejected()
    {
        const uint seed = 0x12345678;
        using var harness = new Harness(EncryptionMode.Encrypted);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(GameLogin(seed));
            NetTestHelpers.SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);
            Assert.Null(_gameLogin);
            Assert.Null(_accountLogin);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(EncryptionMode.Unencrypted)]
    [InlineData(EncryptionMode.None)]
    public void EncryptedGameLogin_WhenEncryptedIsNotAllowed_IsRejected(EncryptionMode mode)
    {
        using var harness = new Harness(mode);
        var seed = FindGameSeed(IsUnambiguous);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(GameEncrypt(GameLogin(seed), seed));
            NetTestHelpers.SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);
            Assert.Null(_gameLogin);
            Assert.Null(_accountLogin);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    public static TheoryData<int> LegacyKeyIndexes()
    {
        var data = new TheoryData<int>();
        for (var i = 0; i < LoginKeys.LegacyKeys.Length; i++)
        {
            data.Add(i);
        }

        return data;
    }

    // Pre-0xEF clients send no version, so every legacy key is a candidate. The 5.0.x and 6.0.x (x <= 4)
    // keystreams differ only in bytes 16-23 and 37-44, inside the credential fields, so both pass the
    // structural check on bytes 0, 30 and 60.
    [Theory]
    [MemberData(nameof(LegacyKeyIndexes))]
    public void LegacyEncryptedLogin_OnTheGameConnection_IsDecrypted(int keyIndex)
    {
        const uint seed = 0x0100007F;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin(password: "password1");
        var cipher = LoginEncrypt(plain, seed, LoginKeys.LegacyKeys[keyIndex]);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    // A non-ASCII byte where the 5.0.0 and 6.0.0 keystreams differ fails the well-formed check under both
    // keys; the first candidate, 5.0.0, must still win as it did before the tie-break existed
    [Fact]
    public void LegacyEncryptedLogin_AmbiguousBetweenKeys_KeepsTheFirstCandidate()
    {
        const uint seed = 0x0100007F;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin("userwithlongname", "password1");
        plain[17] = 0xE9; // packet byte 17, inside the differing bytes 16..23
        var cipher = LoginEncrypt(plain, seed, LoginKeys.LegacyKeys[1]);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    // Long credentials fill the differing bytes with characters rather than padding
    [Theory]
    [MemberData(nameof(LegacyKeyIndexes))]
    public void LegacyEncryptedLogin_WithLongCredentials_IsDecrypted(int keyIndex)
    {
        const uint seed = 0x0A0B0CAA;
        using var harness = new Harness(EncryptionMode.Both);
        var plain = AccountLogin(new string('u', 29), new string('p', 29));
        var cipher = LoginEncrypt(plain, seed, LoginKeys.LegacyKeys[keyIndex]);

        var ns = Connect(NetState.ProtocolState.GameServer_AwaitingGameServerLogin, seed, null, out var client);
        try
        {
            client.Send(cipher);
            AssertAccountLogin(ns, plain, encrypted: true);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }
}
