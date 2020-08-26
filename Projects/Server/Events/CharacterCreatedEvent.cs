/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: CharacterCreatedEvent.cs                                        *
 * Created: 2020/04/11 - Updated: 2020/04/11                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Accounting;
using Server.Network;

namespace Server
{
    public class CharacterCreatedEventArgs : EventArgs
    {
        public CharacterCreatedEventArgs(
            NetState state, IAccount a, string name, bool female, int hue, int str, int dex,
            int intel, CityInfo city, SkillNameValue[] skills, int shirtHue, int pantsHue, int hairID, int hairHue,
            int beardID, int beardHue, int profession, Race race
        )
        {
            State = state;
            Account = a;
            Name = name;
            Female = female;
            Hue = hue;
            Str = str;
            Dex = dex;
            Int = intel;
            City = city;
            Skills = skills;
            ShirtHue = shirtHue;
            PantsHue = pantsHue;
            HairID = hairID;
            HairHue = hairHue;
            BeardID = beardID;
            BeardHue = beardHue;
            Profession = profession;
            Race = race;
        }

        public NetState State { get; }

        public IAccount Account { get; }

        public Mobile Mobile { get; set; }

        public string Name { get; }

        public bool Female { get; }

        public int Hue { get; }

        public int Str { get; }

        public int Dex { get; }

        public int Int { get; }

        public CityInfo City { get; }

        public SkillNameValue[] Skills { get; }

        public int ShirtHue { get; }

        public int PantsHue { get; }

        public int HairID { get; }

        public int HairHue { get; }

        public int BeardID { get; }

        public int BeardHue { get; }

        public int Profession { get; set; }

        public Race Race { get; }
    }

    public struct SkillNameValue
    {
        public SkillName Name { get; }

        public int Value { get; }

        public SkillNameValue(SkillName name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public static partial class EventSink
    {
        public static event Action<CharacterCreatedEventArgs> CharacterCreated;
        public static void InvokeCharacterCreated(CharacterCreatedEventArgs e) => CharacterCreated?.Invoke(e);
    }
}
