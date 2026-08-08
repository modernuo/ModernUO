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
    private const int _authIDWindowSize = 128;

    // The gap between PlayServerAck and the client's game login is seconds. Two minutes is generous,
    // and bounds how long a stolen id stays usable.
    private static readonly TimeSpan _authIDLifetime = TimeSpan.FromMinutes(2.0);

    private static readonly Dictionary<int, AuthIDPersistence> _authIDWindow =
        new(_authIDWindowSize);

    internal struct AuthIDPersistence
    {
        public DateTime Age;
        public readonly ClientVersion Version;

        // The account and address that earned this id on the account login packet. GameLogin skips
        // its own password verify when both match, so the id is a bearer token and must be bound.
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
        // No such id, or it was issued for a different account or address. A client that presents
        // one of those is not one of ours; nothing here is worth a password check.
        Rejected,

        // Right account, right address, but too old to stand in for the verify. A player can idle
        // on the server list, so this is a normal thing to do -- fall back to checking the password
        // rather than turning it into a lockout.
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
        RegisterAuthId(state.Account, state.Address, state.Version);

    internal static int RegisterAuthId(IAccount account, IPAddress address, ClientVersion version)
    {
        // Ids are only consumed by a game login, so anyone who reaches the server list and never
        // picks a server leaves theirs behind. Reclaim the dead ones before evicting a live one --
        // otherwise enough abandoned logins fill the window and start pushing out ids that clients
        // are still on their way to redeem.
        if (_authIDWindow.Count >= _authIDWindowSize)
        {
            PurgeExpiredAuthIds();
        }

        if (_authIDWindow.Count >= _authIDWindowSize)
        {
            var oldestID = 0;
            var oldest = DateTime.MaxValue;

            foreach (var (key, authId) in _authIDWindow)
            {
                if (authId.Age < oldest)
                {
                    oldestID = key;
                    oldest = authId.Age;
                }
            }

            _authIDWindow.Remove(oldestID);
        }

        int authID;

        // A cryptographic draw across the whole int range: the id stands in for a password verify,
        // so it has to be unguessable. Zero is reserved -- GameLogin reads state.AuthId == 0 as
        // "no auth id was issued".
        do
        {
            authID = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
        } while (authID == 0 || _authIDWindow.ContainsKey(authID));

        _authIDWindow[authID] = new AuthIDPersistence(version, account, address);

        return authID;
    }

    /// <summary>
    /// Looks up and spends an auth id. The id is removed whether or not it vouches, so a guessed id
    /// cannot be reused to enumerate usernames. An address mismatch is
    /// <see cref="AuthIdResult.Rejected"/> rather than a fallback: network switching mid-login is
    /// not supported.
    /// </summary>
    internal static AuthIdResult ConsumeAuthId(
        int authId, string username, IPAddress address, out AuthIDPersistence entry
    )
    {
        if (!_authIDWindow.Remove(authId, out entry))
        {
            return AuthIdResult.Rejected;
        }

        // Address before age: a different address is a different client, however fresh the id is.
        if (!Utility.Intern(address).Equals(entry.Address))
        {
            return AuthIdResult.Rejected;
        }

        if (entry.Account == null || !username.InsensitiveEquals(entry.Account.Username))
        {
            return AuthIdResult.Rejected;
        }

        return Core.Now - entry.Age > _authIDLifetime ? AuthIdResult.Expired : AuthIdResult.Vouched;
    }

    private static void PurgeExpiredAuthIds()
    {
        var now = Core.Now;

        // Removing during enumeration is supported on Dictionary since .NET Core 3.0, so this
        // reclaims in one pass with no scratch list.
        foreach (var (key, entry) in _authIDWindow)
        {
            if (now - entry.Age > _authIDLifetime)
            {
                _authIDWindow.Remove(key);
            }
        }
    }

    internal static void ClearAuthIdWindow() => _authIDWindow.Clear();

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

        // Spends the id either way, so a guessed one cannot be reused to probe usernames.
        var authResult = ConsumeAuthId(authId, username, state.Address, out var ap);

        if (authResult == AuthIdResult.Rejected)
        {
            state.LogInfo("Invalid client detected, disconnecting...");
            state.Disconnect("Unable to find auth id.");
            return;
        }

        state.Version = ap.Version;
        state.Seeded = true;

        // Expired still carries a usable entry, and the client just pays the password verify it
        // would have paid before any of this existed.
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

            state.AuthId = GenerateAuthID(state);

            state.SentFirstPacket = false;
            state.SendPlayServerAck(si, state.AuthId);
        }
    }

    public static void LoginServerSeed(NetState state, SpanReader reader)
    {
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

        if (accountLoginEventArgs.Accepted)
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
            AccountLogin_ReplyRej(state, accountLoginEventArgs.RejectReason);
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
