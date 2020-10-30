/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: Packets.Accounts.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.IO;

namespace Server.Network
{
    public static partial class Packets
    {
        public static void CreateCharacter(this NetState state, CircularBufferReader reader)
        {
            var unk1 = reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            int unk3 = reader.ReadByte();
            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);
            var flags = reader.ReadInt32();
            reader.Seek(8, SeekOrigin.Current);
            int prof = reader.ReadByte();
            reader.Seek(15, SeekOrigin.Current);

            int genderRace = reader.ReadByte();

            int str = reader.ReadByte();
            int dex = reader.ReadByte();
            int intl = reader.ReadByte();
            int is1 = reader.ReadByte();
            int vs1 = reader.ReadByte();
            int is2 = reader.ReadByte();
            int vs2 = reader.ReadByte();
            int is3 = reader.ReadByte();
            int vs3 = reader.ReadByte();
            int hue = reader.ReadUInt16();
            int hairVal = reader.ReadInt16();
            int hairHue = reader.ReadInt16();
            int hairValf = reader.ReadInt16();
            int hairHuef = reader.ReadInt16();
            reader.ReadByte();
            int cityIndex = reader.ReadByte();
            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();
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

            Race race;

            if (state.StygianAbyss)
            {
                var raceID = (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1);
                race = Race.Races[raceID];
            }
            else
            {
                race = Race.Races[(byte)(genderRace / 2)];
            }

            race ??= Race.DefaultRace;

            var info = state.CityInfo;
            var a = state.Account;

            if (info == null || a == null || cityIndex < 0 || cityIndex >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal)
                    {
                        state.WriteConsole("Account in use");
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
                    str,
                    dex,
                    intl,
                    info[cityIndex],
                    new[]
                    {
                        new SkillNameValue((SkillName)is1, vs1),
                        new SkillNameValue((SkillName)is2, vs2),
                        new SkillNameValue((SkillName)is3, vs3)
                    },
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
                    state.Dispose();
                }
            }
        }

        public static void CreateCharacter70160(this NetState state, CircularBufferReader reader)
        {
            var unk1 = reader.ReadInt32();
            var unk2 = reader.ReadInt32();
            int unk3 = reader.ReadByte();
            var name = reader.ReadAscii(30);

            reader.Seek(2, SeekOrigin.Current);
            var flags = reader.ReadInt32();
            reader.Seek(8, SeekOrigin.Current);
            int prof = reader.ReadByte();
            reader.Seek(15, SeekOrigin.Current);

            int genderRace = reader.ReadByte();

            int str = reader.ReadByte();
            int dex = reader.ReadByte();
            int intl = reader.ReadByte();
            int is1 = reader.ReadByte();
            int vs1 = reader.ReadByte();
            int is2 = reader.ReadByte();
            int vs2 = reader.ReadByte();
            int is3 = reader.ReadByte();
            int vs3 = reader.ReadByte();
            int is4 = reader.ReadByte();
            int vs4 = reader.ReadByte();

            int hue = reader.ReadUInt16();
            int hairVal = reader.ReadInt16();
            int hairHue = reader.ReadInt16();
            int hairValf = reader.ReadInt16();
            int hairHuef = reader.ReadInt16();
            reader.ReadByte();
            int cityIndex = reader.ReadByte();
            var charSlot = reader.ReadInt32();
            var clientIP = reader.ReadInt32();
            int shirtHue = reader.ReadInt16();
            int pantsHue = reader.ReadInt16();

            /*
            0x00, 0x01
            0x02, 0x03 -> Human Male, Human Female
            0x04, 0x05 -> Elf Male, Elf Female
            0x05, 0x06 -> Gargoyle Male, Gargoyle Female
            */

            var female = genderRace % 2 != 0;

            Race race;

            var raceID = (byte)(genderRace < 4 ? 0 : genderRace / 2 - 1);
            race = Race.Races[raceID] ?? Race.DefaultRace;

            var info = state.CityInfo;
            var a = state.Account;

            if (info == null || a == null || cityIndex < 0 || cityIndex >= info.Length)
            {
                state.Dispose();
            }
            else
            {
                // Check if anyone is using this account
                for (var i = 0; i < a.Length; ++i)
                {
                    var check = a[i];

                    if (check != null && check.Map != Map.Internal)
                    {
                        state.WriteConsole("Account in use");
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
                    str,
                    dex,
                    intl,
                    info[cityIndex],
                    new[]
                    {
                        new SkillNameValue((SkillName)is1, vs1),
                        new SkillNameValue((SkillName)is2, vs2),
                        new SkillNameValue((SkillName)is3, vs3),
                        new SkillNameValue((SkillName)is4, vs4)
                    },
                    shirtHue,
                    pantsHue,
                    hairVal,
                    hairHue,
                    hairValf,
                    hairHuef,
                    prof,
                    race
                );

                SendClientVersionRequest(state);

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
                    state.Dispose();
                }
            }
        }
    }
}
