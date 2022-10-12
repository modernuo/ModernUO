using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Server;

[Parsable]
public abstract class Race
{
    private static string[] m_RaceNames;
    private static Race[] m_RaceValues;

    protected Race(
        int raceID, int raceIndex, string name, string pluralName, int maleBody, int femaleBody,
        int maleGhostBody, int femaleGhostBody, Expansion requiredExpansion
    )
    {
        RaceID = raceID;
        RaceIndex = raceIndex;
        RaceFlag = 1 << raceIndex;

        Name = name;

        MaleBody = maleBody;
        FemaleBody = femaleBody;
        MaleGhostBody = maleGhostBody;
        FemaleGhostBody = femaleGhostBody;

        RequiredExpansion = requiredExpansion;
        PluralName = pluralName;
    }

    public static Race[] Races { get; } = new Race[0x100];

    public static Race DefaultRace => Races[0];

    public static Race Human => Races[0];
    public static Race Elf => Races[1];
    public static Race Gargoyle => Races[2];

    public static List<Race> AllRaces { get; } = new();

    public const int AllowAllRaces = 0x7;      // Race.Human.RaceFlag | Race.Elf.RaceFlag | Race.Gargoyle.RaceFlag
    public const int AllowHumanOrElves = 0x3;  // Race.Human.RaceFlag | Race.Elf.RaceFlag
    public const int AllowElvesOnly = 0x2;     // Race.Elf.RaceFlag
    public const int AllowGargoylesOnly = 0x4; // Race.Gargoyle.RaceFlag

    public Expansion RequiredExpansion { get; }

    public int MaleBody { get; }

    public int MaleGhostBody { get; }

    public int FemaleBody { get; }

    public int FemaleGhostBody { get; }

    public int RaceID { get; }

    public int RaceIndex { get; }

    public int RaceFlag { get; }

    public string Name { get; set; }

    public string PluralName { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllowedRace(Race race, int allowedRaceFlags) => (allowedRaceFlags & race.RaceFlag) != 0;

    public static string[] GetRaceNames()
    {
        CheckNamesAndValues();
        return m_RaceNames;
    }

    public static Race[] GetRaceValues()
    {
        CheckNamesAndValues();
        return m_RaceValues;
    }

    public static Race Parse(string value)
    {
        CheckNamesAndValues();

        for (var i = 0; i < m_RaceNames.Length; ++i)
        {
            if (m_RaceNames[i].InsensitiveEquals(value))
            {
                return m_RaceValues[i];
            }
        }

        if (int.TryParse(value, out var index) && index >= 0 && index < Races.Length &&
            Races[index] != null)
        {
            return Races[index];
        }

        throw new ArgumentException("Invalid race name");
    }

    private static void CheckNamesAndValues()
    {
        if (m_RaceNames?.Length == AllRaces.Count)
        {
            return;
        }

        m_RaceNames = new string[AllRaces.Count];
        m_RaceValues = new Race[AllRaces.Count];

        for (var i = 0; i < AllRaces.Count; ++i)
        {
            var race = AllRaces[i];

            m_RaceNames[i] = race.Name;
            m_RaceValues[i] = race;
        }
    }

    public override string ToString() => Name;

    public virtual bool ValidateHair(Mobile m, int itemID) => ValidateHair(m.Female, itemID);

    public abstract bool ValidateHair(bool female, int itemID);

    public virtual int RandomHair(Mobile m) => RandomHair(m.Female);

    public abstract int RandomHair(bool female);

    public virtual bool ValidateFacialHair(Mobile m, int itemID) => ValidateFacialHair(m.Female, itemID);

    public abstract bool ValidateFacialHair(bool female, int itemID);

    public virtual int RandomFacialHair(Mobile m) => RandomFacialHair(m.Female);

    public abstract int RandomFacialHair(bool female); // For the *ahem* bearded ladies

    public abstract int ClipSkinHue(int hue);
    public abstract int RandomSkinHue();

    public abstract int ClipHairHue(int hue);
    public abstract int RandomHairHue();

    public virtual int Body(Mobile m) => m.Alive ? AliveBody(m.Female) : GhostBody(m.Female);

    public virtual int AliveBody(Mobile m) => AliveBody(m.Female);

    public virtual int AliveBody(bool female) => female ? FemaleBody : MaleBody;

    public virtual int GhostBody(Mobile m) => GhostBody(m.Female);

    public virtual int GhostBody(bool female) => female ? FemaleGhostBody : MaleGhostBody;
}
