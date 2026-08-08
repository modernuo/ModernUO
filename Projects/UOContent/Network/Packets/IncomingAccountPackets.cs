/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: IncomingAccountPackets.cs                                       *
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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using Server.Accounting;
using Server.Engines.CharacterCreation;
using Server.Misc;
using Server.Mobiles;

namespace Server.Network;

public static class IncomingAccountPackets
{
    // Initial capacity and the point at which issuing sweeps expired ids. Not a cap; the window
    // grows rather than evicting a live id.
    private const int _authIDWindowSize = 128;

    private static int _authIdPurgeThreshold = _authIDWindowSize;

    // The gap between PlayServerAck and the game login is seconds. Bounds how long a stolen id
    // stays usable.
    private static readonly TimeSpan _authIDLifetime = TimeSpan.FromMinutes(2.0);

    private static readonly Dictionary<int, AuthIDPersistence> _authIDWindow =
        new(_authIDWindowSize);

    internal struct AuthIDPersistence
    {
        public DateTime Age;
        public readonly ClientVersion Version;

        // GameLogin skips its password verify when both match, so the id is a bearer token and has
        // to be bound to whatever earned it.
        public readonly IAccount Account;
        public readonly IPAddress Address;

        public AuthIDPersistence(ClientVersion v, IAccount account, IPAddress address)
        {
            Age = Core.Now;
            Version = v;
            Account = account;
            Address = Utility.Intern(address);
        }
    }

    internal enum AuthIdResult
    {
        // No such id, or it was issued for a different account or address.
        Rejected,

        // Right account and address, too old to stand in for the verify. Idling on the server list
        // is normal, so this falls back to the password check rather than becoming a lockout.
        Expired,

        // Issued to this account, from this address, recently. Stands in for the password verify.
        Vouched
    }

    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x00, &CreateCharacter, 104, outgameOnly: true);
        IncomingPackets.Register(0x5D, &PlayCharacter, 73, outgameOnly: true);
        IncomingPackets.Register(0x80, &AccountLogin, 62, outgameOnly: true);
        IncomingPackets.Register(0x83, &DeleteCharacter, 39, outgameOnly: true);
        IncomingPackets.Register(0x91, &GameLogin, 65, outgameOnly: true);
        IncomingPackets.Register(0xA0, &PlayServer, 3, outgameOnly: true);
        IncomingPackets.Register(0xBD, &ClientVersion);
        IncomingPackets.Register(0xE1, &ClientType);
        IncomingPackets.Register(0xEF, &LoginServerSeed, 21, outgameOnly: true);
        IncomingPackets.Register(0xF8, &CreateCharacter, 106, outgameOnly: true);
    }

    public static void CreateCharacter(NetState state, SpanReader reader)
    {
        reader.Seek(9, SeekOrigin.Current);
        /*
        var unk1 = reader.ReadInt32();
        var unk2 = reader.ReadInt32();
        int unk3 = reader.ReadByte();
        */
        var name = reader.ReadLatin1Safe(30);

        reader.Seek(2, SeekOrigin.Current);
        var flags = reader.ReadInt32();
        reader.Seek(8, SeekOrigin.Current);
        int prof = reader.ReadByte();
        reader.Seek(15, SeekOrigin.Current);

        var genderRace = reader.ReadByte();

        // Strength, Dex, Intelligence
        byte[] stats = [reader.ReadByte(), reader.ReadByte(), reader.ReadByte()];

        var skills = new (SkillName, byte)[state.NewCharacterCreation ? 4 : 3];
        skills[0] = ((SkillName)reader.ReadByte(), reader.ReadByte());
        skills[1] = ((SkillName)reader.ReadByte(), reader.ReadByte());
        skills[2] = ((SkillName)reader.ReadByte(), reader.ReadByte());

        if (state.NewCharacterCreation)
        {
            skills[3] = ((SkillName)reader.ReadByte(), reader.ReadByte());
        }

        int hue = reader.ReadUInt16();
        int hairVal = reader.ReadInt16();
        int hairHue = reader.ReadInt16();
        int hairValf = reader.ReadInt16();
        int hairHuef = reader.ReadInt16();
        reader.ReadByte();
        int cityIndex = reader.ReadByte();
        reader.Seek(8, SeekOrigin.Current);
        /*
        var charSlot = reader.ReadInt32();
        var clientIP = reader.ReadInt32();
        */
        int shirtHue = reader.ReadInt16();
        int pantsHue = reader.ReadInt16();

        /*
        Pre-7.0.0.0:
        0x00, 0x01 -> Human Male, Human Female
        0x02, 0x03 -> Elf Male, Elf Female

        Post-7.0.0.0:
        0x00, 0x01
        0x02, 0x03 -> Human Male, Human Female
        0x04, 0x05 -> Elf Male, Elf Female
        0x05, 0x06 -> Gargoyle Male, Gargoyle Female
        */

        var female = genderRace % 2 != 0;

        var raceID = state.StygianAbyss ? (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1) : (byte)(genderRace / 2);
        var race = Race.Races[raceID] ?? Race.DefaultRace;

        var info = state.CityInfo;
        var a = state.Account;

        if (info == null || a == null || cityIndex >= info.Length)
        {
            state.Disconnect("Invalid city selected during character creation.");
            return;
        }

        // Check if anyone is using this account
        for (var i = 0; i < a.Length; ++i)
        {
            var check = a[i];

            if (check != null && check.Map != Map.Internal)
            {
                state.LogInfo("Account in use");
                state.SendPopupMessage(PMMessage.CharInWorld);
                return;
            }
        }

        state.Flags = (ClientFlags)flags;

        var args = new CharacterCreatedEventArgs(
            state,
            a,
            name,
            female,
            hue,
            stats,
            info[cityIndex],
            skills,
            shirtHue,
            pantsHue,
            hairVal,
            hairHue,
            hairValf,
            hairHuef,
            prof,
            race
        );

        state.SendClientVersionRequest();

        state.BlockAllPackets = true;

        CharacterCreation.CharacterCreatedEvent(args);

        var m = args.Mobile;

        if (m != null)
        {
            state.Mobile = m;
            m.NetState = state;
            new LoginTimer(state, m).Start();
        }
        else
        {
            state.BlockAllPackets = false;
            state.Disconnect("Character creation blocked.");
        }
    }

    public static void DeleteCharacter(NetState state, SpanReader reader)
    {
        reader.Seek(30, SeekOrigin.Current);
        var index = reader.ReadInt32();

        AccountHandler.DeleteRequest(state, index);
    }

    public static void ClientVersion(NetState state, SpanReader reader)
    {
        var version = state.Version = new ClientVersion(reader.ReadAscii());

        // Record RTT if this is a response to our probe
        state.RecordRttMeasurement();

        ClientVerification.ClientVersionReceived(state, version);
    }

    public static void ClientType(NetState state, SpanReader reader)
    {
        reader.ReadUInt16();

        int type = reader.ReadUInt16();
        var version = state.Version = new ClientVersion(reader.ReadAscii());

        // Record RTT if this is a response to our probe
        state.RecordRttMeasurement();

        ClientVerification.ClientVersionReceived(state, version);
    }

    public static void PlayCharacter(NetState state, SpanReader reader)
    {
        reader.Seek(36, SeekOrigin.Current); // 4 = 0xEDEDEDED, 30 = Name, 2 = unknown
        var flags = reader.ReadInt32();
        reader.Seek(24, SeekOrigin.Current);
        var charSlot = reader.ReadInt32();
        reader.Seek(4, SeekOrigin.Current); // var clientIP = reader.ReadInt32();

        var a = state.Account;

        if (a == null || charSlot < 0 || charSlot >= a.Length)
        {
            state.Disconnect("Invalid character slot selected.");
            return;
        }

        var m = a[charSlot];

        // Check if anyone is using this account
        for (var i = 0; i < a.Length; ++i)
        {
            var check = a[i];

            if (check != null && check.Map != Map.Internal && check != m)
            {
                state.LogInfo("Account in use");
                state.SendPopupMessage(PMMessage.CharInWorld);
                return;
            }
        }

        if (m == null)
        {
            state.Disconnect("Empty character slot selected.");
            return;
        }

        m.NetState?.Disconnect("Character selected for a player already logged in.");

        state.SendClientVersionRequest();

        state.BlockAllPackets = true;

        state.Flags = (ClientFlags)flags;

        state.Mobile = m;
        m.NetState = state;

        new LoginTimer(state, m).Start();
    }

    public static void DoLogin(this NetState state, Mobile m)
    {
        state.SendLoginConfirmation(m);

        state.SendMapChange(m.Map);

        state.SendMapPatches();

        state.SendSeasonChange((byte)m.GetSeason(), true);

        state.SendSupportedFeature();

        state.ResetMovementState();

        state.SendMobileUpdate(m);
        state.SendMobileUpdate(m);

        m.CheckLightLevels(true);

        state.SendMobileUpdate(m);

        state.SendMobileIncoming(m, m);

        state.SendMobileStatus(m);
        state.SendSetWarMode(m.Warmode);

        m.SendEverything();

        state.SendSupportedFeature();
        state.SendMobileUpdate(m);

        state.SendMobileStatus(m);
        state.SendSetWarMode(m.Warmode);
        state.SendMobileIncoming(m, m);

        state.SendLoginComplete();
        state.SendCurrentTime();
        state.SendSeasonChange((byte)m.GetSeason(), true);
        state.SendMapChange(m.Map);

        state.SendPlayMusic(m.Region.Music);

        if (m is PlayerMobile pm)
        {
            PlayerMobile.PlayerLoginEvent(pm);
        }
    }

    private static int GenerateAuthID(this NetState state) =>
        EnsureAuthId(state.AuthId, state.Account, state.Address, state.Version);

    /// <summary>
    /// One id per connection, by construction. Choosing a server queues a disconnect that is not
    /// drained until the next slice, so a client pipelining another select into the same buffer
    /// arrives here again; handing back the id it already holds cannot orphan one.
    /// </summary>
    internal static int EnsureAuthId(int existingAuthId, IAccount account, IPAddress address, ClientVersion version)
        => existingAuthId != 0 ? existingAuthId : RegisterAuthId(account, address, version);

    internal static int RegisterAuthId(IAccount account, IPAddress address, ClientVersion version)
    {
        // Sweep the ids left behind by clients that picked a server and never arrived, but never
        // evict a live one to make room -- the client holding it is on its way to redeem it. If all
        // are live the window grows, which is a login rush, not a backlog. Each entry costs a
        // successful password verify, so the size is self-limiting.
        if (_authIDWindow.Count >= _authIdPurgeThreshold)
        {
            PurgeExpiredAuthIds();
            _authIdPurgeThreshold = Math.Max(_authIDWindowSize, _authIDWindow.Count * 2);
        }

        int authID;

        // The id stands in for a password verify, so it has to be unguessable. Zero is reserved:
        // GameLogin reads state.AuthId == 0 as "no auth id was issued".
        do
        {
            authID = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        } while (authID == 0 || _authIDWindow.ContainsKey(authID));

        _authIDWindow[authID] = new AuthIDPersistence(version, account, address);

        return authID;
    }

    /// <summary>
    /// Spends an auth id, but only for the account and address it was issued to. An address
    /// mismatch is <see cref="AuthIdResult.Rejected"/> rather than a fallback: network switching
    /// mid-login is not supported.
    /// </summary>
    internal static AuthIdResult ConsumeAuthId(int authId, string username, IPAddress address, out AuthIDPersistence entry)
    {
        if (!_authIDWindow.TryGetValue(authId, out entry))
        {
            return AuthIdResult.Rejected;
        }

        // Look, then take: removing before ownership is proven would let anyone landing on a live id
        // burn it, leaving its owner to log in again. Address before username, so a remote guesser
        // never learns whether a username matched.
        if (!Utility.Intern(address).Equals(entry.Address)
            || entry.Account == null || !username.InsensitiveEquals(entry.Account.Username))
        {
            entry = default;
            return AuthIdResult.Rejected;
        }

        // Theirs, so spend it. Expired counts as spent; it has done all it is ever going to do.
        _authIDWindow.Remove(authId);

        return Core.Now - entry.Age > _authIDLifetime ? AuthIdResult.Expired : AuthIdResult.Vouched;
    }

    private static void PurgeExpiredAuthIds()
    {
        var now = Core.Now;

        foreach (var (key, entry) in _authIDWindow)
        {
            if (now - entry.Age > _authIDLifetime)
            {
                _authIDWindow.Remove(key);
            }
        }
    }

    internal static void ClearAuthIdWindow()
    {
        _authIDWindow.Clear();
        _authIdPurgeThreshold = _authIDWindowSize;
    }

    internal static int AuthIdWindowCount => _authIDWindow.Count;

    public static void GameLogin(NetState state, SpanReader reader)
    {
        if (state.SentFirstPacket)
        {
            state.Disconnect("Duplicate game login packet received.");
            return;
        }

        state.SentFirstPacket = true;

        var authId = reader.ReadInt32();

        if (state.AuthId != 0 && authId != state.AuthId || state.AuthId == 0 && authId != state.Seed)
        {
            state.LogInfo("Invalid client detected, disconnecting...");
            state.Disconnect("Invalid auth id in game login packet.");
            return;
        }

        var username = reader.ReadLatin1Safe(30);
        var password = reader.ReadLatin1Safe(30);

        var authResult = ConsumeAuthId(authId, username, state.Address, out var ap);

        if (authResult == AuthIdResult.Rejected)
        {
            state.LogInfo("Invalid client detected, disconnecting...");
            state.Disconnect("Unable to find auth id.");
            return;
        }

        state.Version = ap.Version;
        state.Seeded = true;

        // Expired carries a usable entry; only the password verify skip is withheld.
        var e = new GameServer.GameLoginEventArgs(
            state,
            username,
            password,
            authResult == AuthIdResult.Vouched
        );

        GameServer.GameServerLoginEvent(e);

        if (e.Accepted)
        {
            state.CityInfo = e.CityInfo;

            // Comment out these lines to turn off huffman compression
            state.CompressionEnabled = true;

            state.SendSupportedFeature();
            state.SendCharacterList();
        }
        else
        {
            state.Disconnect("Login rejected by GameLogin packet handler.");
        }
    }

    public static void PlayServer(NetState state, SpanReader reader)
    {
        // A server is picked once per connection. Picking again hands back an id this connection may
        // already have spent on a game login, which the client could never redeem.
        if (state.AuthId != 0)
        {
            state.Disconnect("Duplicate play server packet sent.");
            return;
        }

        int index = reader.ReadInt16();
        var info = state.ServerInfo;
        var a = state.Account;

        if (info == null || a == null || index < 0 || index >= info.Length)
        {
            state.Disconnect("Invalid server selected.");
        }
        else
        {
            var si = info[index];

            state.AuthId = state.GenerateAuthID();

            state.SentFirstPacket = false;
            state.SendPlayServerAck(si, state.AuthId);
        }
    }

    public static void LoginServerSeed(NetState state, SpanReader reader)
    {
        // Seeding happens once per connection. A second one restarts a handshake this connection
        // already completed, which no real client does.
        if (state.Seeded)
        {
            state.Disconnect("Duplicate login server seed packet sent.");
            return;
        }

        state.Seed = reader.ReadInt32();
        state.Seeded = true;

        if (state.Seed == 0)
        {
            state.LogInfo("Invalid client detected, disconnecting");
            state.Disconnect("Invalid client detected");
            return;
        }

        var clientMaj = reader.ReadInt32();
        var clientMin = reader.ReadInt32();
        var clientRev = reader.ReadInt32();
        var clientPat = reader.ReadInt32();

        state.Version = new ClientVersion(clientMaj, clientMin, clientRev, clientPat);
    }

    public static void AccountLogin(NetState state, SpanReader reader)
    {
        if (state.SentFirstPacket)
        {
            state.Disconnect("Duplicate account login packet sent.");
            return;
        }

        state.SentFirstPacket = true;

        var username = reader.ReadLatin1Safe(30);
        var password = reader.ReadLatin1Safe(30);

        var accountLoginEventArgs = new AccountLoginEventArgs(state, username, password);

        EventSink.InvokeAccountLogin(accountLoginEventArgs);

        // The password check moved off the loop; whoever took it replies when the verdict lands.
        if (accountLoginEventArgs.Deferred)
        {
            return;
        }

        CompleteAccountLogin(state, accountLoginEventArgs.Accepted, accountLoginEventArgs.RejectReason);
    }

    /// <summary>
    /// Replies to an account login. Split out so a verdict produced off the loop reaches the client
    /// through exactly the same path as one produced inline.
    /// </summary>
    internal static void CompleteAccountLogin(NetState state, bool accepted, ALRReason rejectReason)
    {
        if (accepted)
        {
            var serverListEventArgs = new GatewayServer.ServerListEventArgs(state, state.Account);

            GatewayServer.ServerListEvent(serverListEventArgs);

            if (serverListEventArgs.Rejected)
            {
                state.Account = null;
                AccountLogin_ReplyRej(state, ALRReason.BadComm);
            }
            else
            {
                state.ServerInfo = serverListEventArgs.Servers.ToArray();
                state.SendAccountLoginAck();
            }
        }
        else
        {
            state.Account = null;
            AccountLogin_ReplyRej(state, rejectReason);
        }
    }

    private static void AccountLogin_ReplyRej(this NetState state, ALRReason reason)
    {
        state.SendAccountLoginRejected(reason);
        state.Disconnect($"Account login rejected due to {reason}");
    }

    private class LoginTimer : Timer
    {
        private readonly Mobile _mobile;
        private readonly NetState _state;

        public LoginTimer(NetState state, Mobile m) : base(TimeSpan.FromMilliseconds(64), TimeSpan.FromMilliseconds(64))
        {
            _state = state;
            _mobile = m;
        }

        protected override void OnTick()
        {
            if (_state != null)
            {
                if (_state.Account == null)
                {
                    _state.Disconnect("Account was deleted during the login process.");
                }
                else if (_mobile == null)
                {
                    _state.Disconnect("Player was deleted during the login process.");
                }
                else if (_state.Version != null)
                {
                    _state.BlockAllPackets = false;
                    DoLogin(_state, _mobile);
                }
                else // Waiting to receive the client version before we continue the login process
                {
                    return;
                }
            }

            Stop();
        }
    }
}
