/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
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
using System.Collections.Generic;
using System.IO;
using CV = Server.ClientVersion;

namespace Server.Network;

public static class IncomingAccountPackets
{
    private const int m_AuthIDWindowSize = 128;
    private static readonly Dictionary<int, AuthIDPersistence> m_AuthIDWindow =
        new(m_AuthIDWindowSize);

    internal struct AuthIDPersistence
    {
        public DateTime Age;
        public readonly ClientVersion Version;

        public AuthIDPersistence(ClientVersion v)
        {
            Age = Core.Now;
            Version = v;
        }
    }

    public static unsafe void Configure()
    {
        IncomingPackets.Register(0x00, 104, false, &CreateCharacter);
        IncomingPackets.Register(0x5D, 73, false, &PlayCharacter);
        IncomingPackets.Register(0x80, 62, false, &AccountLogin);
        IncomingPackets.Register(0x83, 39, false, &DeleteCharacter);
        IncomingPackets.Register(0x91, 65, false, &GameLogin);
        IncomingPackets.Register(0xA0, 3, false, &PlayServer);
        IncomingPackets.Register(0xBB, 9, false, &AccountID);
        IncomingPackets.Register(0xBD, 0, false, &ClientVersion);
        IncomingPackets.Register(0xCF, 0, false, &AccountLogin);
        IncomingPackets.Register(0xE1, 0, false, &ClientType);
        IncomingPackets.Register(0xEF, 21, false, &LoginServerSeed);
        IncomingPackets.Register(0xF8, 106, false, &CreateCharacter);
    }

    public static void CreateCharacter(NetState state, CircularBufferReader reader, int packetLength)
    {
        reader.Seek(9, SeekOrigin.Current);
        /*
        var unk1 = reader.ReadInt32();
        var unk2 = reader.ReadInt32();
        int unk3 = reader.ReadByte();
        */
        var name = reader.ReadAscii(30);

        reader.Seek(2, SeekOrigin.Current);
        var flags = reader.ReadInt32();
        reader.Seek(8, SeekOrigin.Current);
        int prof = reader.ReadByte();
        reader.Seek(15, SeekOrigin.Current);

        var genderRace = reader.ReadByte();

        var stats = new StatNameValue[]
        {
            new(StatType.Str, reader.ReadByte()),
            new(StatType.Dex, reader.ReadByte()),
            new(StatType.Int, reader.ReadByte())
        };

        var skills = new SkillNameValue[state.NewCharacterCreation ? 4 : 3];
        skills[0] = new SkillNameValue((SkillName)reader.ReadByte(), reader.ReadByte());
        skills[1] = new SkillNameValue((SkillName)reader.ReadByte(), reader.ReadByte());
        skills[2] = new SkillNameValue((SkillName)reader.ReadByte(), reader.ReadByte());
        if (state.NewCharacterCreation)
        {
            skills[3] = new SkillNameValue((SkillName)reader.ReadByte(), reader.ReadByte());
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
        Race race = Race.Races[raceID] ?? Race.DefaultRace;

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

        EventSink.InvokeCharacterCreated(args);

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

    public static void DeleteCharacter(NetState state, CircularBufferReader reader, int packetLength)
    {
        reader.Seek(30, SeekOrigin.Current);
        var index = reader.ReadInt32();

        EventSink.InvokeDeleteRequest(state, index);
    }

    public static void AccountID(NetState state, CircularBufferReader reader, int packetLength)
    {
    }

    public static void ClientVersion(NetState state, CircularBufferReader reader, int packetLength)
    {
        var version = state.Version = new CV(reader.ReadAscii());

        EventSink.InvokeClientVersionReceived(state, version);
    }

    public static void ClientType(NetState state, CircularBufferReader reader, int packetLength)
    {
        reader.ReadUInt16();

        int type = reader.ReadUInt16();
        var version = state.Version = new CV(reader.ReadAscii());

        EventSink.InvokeClientVersionReceived(state, version);
    }

    public static void PlayCharacter(NetState state, CircularBufferReader reader, int packetLength)
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

        state.Sequence = 0;

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

        EventSink.InvokeLogin(m);
    }

    private static int GenerateAuthID(this NetState state)
    {
        if (m_AuthIDWindow.Count == m_AuthIDWindowSize)
        {
            var oldestID = 0;
            var oldest = DateTime.MaxValue;

            foreach (var (key, authId) in m_AuthIDWindow)
            {
                if (authId.Age < oldest)
                {
                    oldestID = key;
                    oldest = authId.Age;
                }
            }

            m_AuthIDWindow.Remove(oldestID);
        }

        int authID;

        do
        {
            authID = Utility.Random(1, int.MaxValue - 1);

            if (Utility.RandomBool())
            {
                authID |= 1 << 31;
            }
        } while (m_AuthIDWindow.ContainsKey(authID));

        m_AuthIDWindow[authID] = new AuthIDPersistence(state.Version);

        return authID;
    }

    public static void GameLogin(NetState state, CircularBufferReader reader, int packetLength)
    {
        // TODO: Connection throttling

        if (state.SentFirstPacket)
        {
            state.Disconnect("Duplicate game login packet received.");
            return;
        }

        state.SentFirstPacket = true;

        var authID = reader.ReadInt32();

        if (!m_AuthIDWindow.TryGetValue(authID, out var ap))
        {
            state.LogInfo("Invalid client detected, disconnecting...");
            state.Disconnect("Unable to find auth id.");
        }

        if (state._authId != 0 && authID != state._authId || state._authId == 0 && authID != state._seed)
        {
            state.LogInfo("Invalid client detected, disconnecting...");
            state.Disconnect("Invalid auth id in game login packet.");
            return;
        }

        m_AuthIDWindow.Remove(authID);
        state.Version = ap.Version;

        var username = reader.ReadAscii(30);
        var password = reader.ReadAscii(30);

        var e = new GameLoginEventArgs(state, username, password);

        EventSink.InvokeGameLogin(e);

        if (e.Accepted)
        {
            state.CityInfo = e.CityInfo;

            // Comment out these lines to turn off huffman compression
            state.CompressionEnabled = true;
            state.PacketEncoder ??= NetworkCompression.Compress;

            state.SendSupportedFeature();
            state.SendCharacterList();
        }
        else
        {
            state.Disconnect("Login rejected by GameLogin packet handler.");
        }
    }

    public static void PlayServer(NetState state, CircularBufferReader reader, int packetLength)
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

            state._authId = GenerateAuthID(state);

            state.SentFirstPacket = false;
            state.SendPlayServerAck(si, state._authId);
        }
    }

    public static void LoginServerSeed(NetState state, CircularBufferReader reader, int packetLength)
    {
        state._seed = reader.ReadInt32();
        state.Seeded = true;

        if (state._seed == 0)
        {
            state.LogInfo("Invalid client detected, disconnecting");
            state.Disconnect("Duplicate seed sent.");
            return;
        }

        var clientMaj = reader.ReadInt32();
        var clientMin = reader.ReadInt32();
        var clientRev = reader.ReadInt32();
        var clientPat = reader.ReadInt32();

        state.Version = new ClientVersion(clientMaj, clientMin, clientRev, clientPat);
    }

    public static void AccountLogin(NetState state, CircularBufferReader reader, int packetLength)
    {
        // TODO: Throttle Connection

        if (state.SentFirstPacket)
        {
            state.Disconnect("Duplicate account login packet sent.");
            return;
        }

        state.SentFirstPacket = true;

        var username = reader.ReadAscii(30);
        var password = reader.ReadAscii(30);

        var accountLoginEventArgs = new AccountLoginEventArgs(state, username, password);

        EventSink.InvokeAccountLogin(accountLoginEventArgs);

        if (accountLoginEventArgs.Accepted)
        {
            var serverListEventArgs = new ServerListEventArgs(state, state.Account);

            EventSink.InvokeServerList(serverListEventArgs);

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
