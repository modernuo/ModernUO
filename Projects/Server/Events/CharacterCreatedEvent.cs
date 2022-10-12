/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CharacterCreatedEvent.cs                                        *
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
using System.Runtime.CompilerServices;
using Server.Accounting;
using Server.Network;

namespace Server;

public class CharacterCreatedEventArgs
{
    public CharacterCreatedEventArgs(
        NetState state, IAccount a, string name, bool female, int hue, StatNameValue[] stats, CityInfo city,
        SkillNameValue[] skills, int shirtHue, int pantsHue, int hairID, int hairHue, int beardID, int beardHue,
        int profession, Race race
    )
    {
        State = state;
        Account = a;
        Name = name;
        Female = female;
        Hue = hue;
        Stats = stats;
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

    public StatNameValue[] Stats { get; }

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

public readonly record struct SkillNameValue(SkillName Name, int Value);
public readonly record struct StatNameValue(StatType Name, int Value);

public static partial class EventSink
{
    public static event Action<CharacterCreatedEventArgs> CharacterCreated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InvokeCharacterCreated(CharacterCreatedEventArgs e) => CharacterCreated?.Invoke(e);
}
