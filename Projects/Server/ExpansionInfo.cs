/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ExpansionInfo.cs                                                *
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
using System.Text.Json.Serialization;
using Server.Json;

namespace Server;

public enum Expansion
{
    None,
    T2A,
    UOR,
    UOTD,
    LBR,
    AOS,
    SE,
    ML,
    SA,
    HS,
    TOL,
    EJ
}

[Flags]
public enum ClientFlags
{
    None = 0x00000000,
    Felucca = 0x00000001,
    Trammel = 0x00000002,
    Ilshenar = 0x00000004,
    Malas = 0x00000008,
    Tokuno = 0x00000010,
    TerMur = 0x00000020,
    Unk1 = 0x00000040,
    Unk2 = 0x00000080,
    UOTD = 0x00000100
}

[Flags]
public enum FeatureFlags
{
    None = 0x00000000,
    T2A = 0x00000001,
    UOR = 0x00000002, // In later clients, the T2A/UOR flags are negative feature flags to disable body replacement of Pre-AOS graphics.
    UOTD = 0x00000004,
    LBR = 0x00000008,
    AOS = 0x00000010,
    SixthCharacterSlot = 0x00000020,
    SE = 0x00000040,
    ML = 0x00000080,
    EighthAge = 0x00000100,
    NinthAge = 0x00000200, // Crystal/Shadow Custom House Tiles
    TenthAge = 0x00000400,
    IncreasedStorage = 0x00000800, // Increased Housing/Bank Storage
    SeventhCharacterSlot = 0x00001000,
    RoleplayFaces = 0x00002000,
    TrialAccount = 0x00004000,
    LiveAccount = 0x00008000,
    SA = 0x00010000,
    HS = 0x00020000,
    Gothic = 0x00040000,
    Rustic = 0x00080000,
    Jungle = 0x00100000,
    Shadowguard = 0x00200000,
    TOL = 0x00400000,
    EJ = 0x00800000,

    ExpansionNone = None,
    ExpansionT2A = T2A,
    ExpansionUOR = ExpansionT2A | UOR,
    ExpansionUOTD = ExpansionUOR | UOTD,
    ExpansionLBR = ExpansionUOTD | LBR,
    ExpansionAOS = LBR | AOS | LiveAccount,
    ExpansionSE = ExpansionAOS | SE,
    ExpansionML = ExpansionSE | ML | NinthAge,
    ExpansionSA = ExpansionML | SA | Gothic | Rustic,
    ExpansionHS = ExpansionSA | HS,
    ExpansionTOL = ExpansionHS | TOL | Jungle | Shadowguard,
    ExpansionEJ = ExpansionTOL | EJ
}

[Flags]
public enum CharacterListFlags
{
    None = 0x00000000,
    Unk1 = 0x00000001,
    OverwriteConfigButton = 0x00000002,
    OneCharacterSlot = 0x00000004,
    ContextMenus = 0x00000008,
    SlotLimit = 0x00000010,
    AOS = 0x00000020,
    SixthCharacterSlot = 0x00000040,
    SE = 0x00000080,
    ML = 0x00000100,
    Unk2 = 0x00000200,
    UO3DClientType = 0x00000400,
    Unk3 = 0x00000800,
    SeventhCharacterSlot = 0x00001000,
    Unk4 = 0x00002000,
    NewMovementSystem = 0x00004000, // Doesn't seem to be used on OSI
    NewFeluccaAreas = 0x00008000,

    ExpansionNone = ContextMenus,
    ExpansionT2A = ContextMenus,
    ExpansionUOR = ContextMenus,
    ExpansionUOTD = ContextMenus,
    ExpansionLBR = ContextMenus,
    ExpansionAOS = ContextMenus | AOS,
    ExpansionSE = ExpansionAOS | SE,
    ExpansionML = ExpansionSE | ML,
    ExpansionSA = ExpansionML,
    ExpansionHS = ExpansionSA,
    ExpansionTOL = ExpansionHS,
    ExpansionEJ = ExpansionTOL
}

[Flags]
public enum HousingFlags
{
    None = 0x0,
    AOS = 0x10,
    SE = 0x40,
    ML = 0x80,
    Crystal = 0x200,
    SA = 0x10000,
    HS = 0x20000,
    Gothic = 0x40000,
    Rustic = 0x80000,
    Jungle = 0x100000,
    Shadowguard = 0x200000,
    TOL = 0x400000,
    EJ = 0x800000,

    HousingAOS = AOS,
    HousingSE = HousingAOS | SE,
    HousingML = HousingSE | ML | Crystal,
    HousingSA = HousingML | SA | Gothic | Rustic,
    HousingHS = HousingSA | HS,
    HousingTOL = HousingHS | TOL | Jungle | Shadowguard,
    HousingEJ = HousingTOL | EJ
}

public class ExpansionInfo
{
    public static bool ForceOldAnimations { get; private set; }
    public static void Configure()
    {
        ForceOldAnimations = ServerConfiguration.GetSetting("expansion.forceOldAnimations", false);
    }

    public static string GetEraFolder(string parentDirectory)
    {
        var expansion = Core.Expansion;
        var folders = Directory.GetDirectories(
            parentDirectory,
            "*",
            new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }
        );

        while (expansion-- >= 0)
        {
            foreach (var folder in folders)
            {
                var di = new DirectoryInfo(folder);
                if (di.Name.InsensitiveEquals(expansion.ToString()))
                {
                    return folder;
                }
            }
        }

        return null;
    }

    static ExpansionInfo()
    {
        var path = Path.Combine(Core.BaseDirectory, "Data/expansion.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Expansion file '{path}' could not be found.");
        }

        var expansions = JsonConfig.Deserialize<List<ExpansionConfig>>(path);

        Table = new ExpansionInfo[expansions.Count];

        for (var i = 0; i < expansions.Count; i++)
        {
            var expansion = expansions[i];
            if (expansion.ClientVersion != null)
            {
                Table[i] = new ExpansionInfo(
                    i,
                    expansion.Name,
                    expansion.ClientVersion,
                    expansion.FeatureFlags,
                    expansion.CharacterListFlags,
                    expansion.HousingFlags,
                    expansion.MobileStatusVersion
                );
            }
            else
            {
                Table[i] = new ExpansionInfo(
                    i,
                    expansion.Name,
                    expansion.ClientFlags ?? ClientFlags.None,
                    expansion.FeatureFlags,
                    expansion.CharacterListFlags,
                    expansion.HousingFlags,
                    expansion.MobileStatusVersion
                );
            }
        }
    }

    public ExpansionInfo(
        int id,
        string name,
        ClientFlags clientFlags,
        FeatureFlags supportedFeatures,
        CharacterListFlags charListFlags,
        HousingFlags customHousingFlag,
        int mobileStatusVersion
    ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag, mobileStatusVersion) =>
        ClientFlags = clientFlags;

    public ExpansionInfo(
        int id,
        string name,
        ClientVersion requiredClient,
        FeatureFlags supportedFeatures,
        CharacterListFlags charListFlags,
        HousingFlags customHousingFlag,
        int mobileStatusVersion
    ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag, mobileStatusVersion) =>
        RequiredClient = requiredClient;

    private ExpansionInfo(
        int id,
        string name,
        FeatureFlags supportedFeatures,
        CharacterListFlags charListFlags,
        HousingFlags customHousingFlag,
        int mobileStatusVersion
    )
    {
        ID = id;
        Name = name;

        SupportedFeatures = supportedFeatures;
        CharacterListFlags = charListFlags;
        CustomHousingFlag = customHousingFlag;
        MobileStatusVersion = mobileStatusVersion;
    }

    public static ExpansionInfo CoreExpansion => GetInfo(Core.Expansion);

    public static ExpansionInfo[] Table { get; }

    public int ID { get; }
    public string Name { get; set; }
    public ClientFlags ClientFlags { get; set; }
    public FeatureFlags SupportedFeatures { get; set; }
    public CharacterListFlags CharacterListFlags { get; set; }
    public ClientVersion RequiredClient { get; set; }
    public HousingFlags CustomHousingFlag { get; set; }
    public int MobileStatusVersion { get; set; }

    public static ExpansionInfo GetInfo(Expansion ex) => GetInfo((int)ex);

    public static ExpansionInfo GetInfo(int ex)
    {
        var v = ex;

        if (v < 0 || v >= Table.Length)
        {
            v = 0;
        }

        return Table[v];
    }

    public override string ToString() => Name;
}

public record ExpansionConfig
{
    public string Name { get; init; }

    public ClientVersion? ClientVersion { get; init; }

    public ClientFlags? ClientFlags { get; init; }

    [JsonConverter(typeof(FlagsConverter<FeatureFlags>))]
    public FeatureFlags FeatureFlags { get; init; }

    [JsonConverter(typeof(FlagsConverter<CharacterListFlags>))]
    public CharacterListFlags CharacterListFlags { get; init; }

    [JsonConverter(typeof(FlagsConverter<HousingFlags>))]
    public HousingFlags HousingFlags { get; init; }

    public int MobileStatusVersion { get; set; }
}
