/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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
using System.IO;
using System.Text.Json.Serialization;
using Server.Json;
using Server.Maps;

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
    KR = 0x00000040,
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
    KR = 0x00000200,
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
    public const string ExpansionConfigurationPath = "Configuration/expansion.json";

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

    public static void StoreMapSelection(MapSelectionFlags mapSelectionFlags, Expansion expansion)
    {
        int expansionIndex = (int)expansion;
        Table[expansionIndex].MapSelectionFlags = mapSelectionFlags;
    }

    public static void SaveConfiguration()
    {
        var pathToExpansionFile = Path.Combine(Core.BaseDirectory, ExpansionConfigurationPath);
        JsonConfig.Serialize(pathToExpansionFile, GetInfo(Core.Expansion));
    }

    public static bool LoadConfiguration(out Expansion expansion)
    {
        var pathToExpansionFile = Path.Combine(Core.BaseDirectory, ExpansionConfigurationPath);

        ExpansionInfo expansionConfig = JsonConfig.Deserialize<ExpansionInfo>(pathToExpansionFile);
        if (expansionConfig == null)
        {
            expansion = Expansion.None;
            return false;
        }

        int currentExpansionIndex = expansionConfig.Id;
        Table[currentExpansionIndex] = expansionConfig;
        expansion = (Expansion)currentExpansionIndex;
        return true;
    }

    static ExpansionInfo()
    {
        var path = Path.Combine(Core.BaseDirectory, "Data/expansions.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Expansion file '{path}' could not be found.");
        }

        Table = JsonConfig.Deserialize<ExpansionInfo[]>(path);
    }

    public ExpansionInfo(
        int id,
        string name,
        ClientFlags clientFlags,
        FeatureFlags supportedFeatures,
        CharacterListFlags charListFlags,
        HousingFlags customHousingFlag,
        int mobileStatusVersion,
        MapSelectionFlags mapSelectionFlags
    ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag, mobileStatusVersion, mapSelectionFlags) =>
        ClientFlags = clientFlags;

    public ExpansionInfo(
        int id,
        string name,
        ClientVersion requiredClient,
        FeatureFlags supportedFeatures,
        CharacterListFlags charListFlags,
        HousingFlags customHousingFlag,
        int mobileStatusVersion,
        MapSelectionFlags mapSelectionFlags
    ) : this(id, name, supportedFeatures, charListFlags, customHousingFlag, mobileStatusVersion, mapSelectionFlags) =>
        RequiredClient = requiredClient;

    [JsonConstructor]
    public ExpansionInfo(
        int id,
        string name,
        FeatureFlags supportedFeatures,
        CharacterListFlags characterListFlags,
        HousingFlags housingFlags,
        int mobileStatusVersion,
        MapSelectionFlags mapSelectionFlags
    )
    {
        Id = id;
        Name = name;

        SupportedFeatures = supportedFeatures;
        CharacterListFlags = characterListFlags;
        HousingFlags = housingFlags;
        MobileStatusVersion = mobileStatusVersion;
        MapSelectionFlags = mapSelectionFlags;
    }

    public static ExpansionInfo CoreExpansion => GetInfo(Core.Expansion);

    public static ExpansionInfo[] Table { get; }

    public int Id { get; }
    public string Name { get; set; }
    public ClientFlags ClientFlags { get; set; }

    [JsonConverter(typeof(FlagsConverter<FeatureFlags>))]
    public FeatureFlags SupportedFeatures { get; set; }

    [JsonConverter(typeof(FlagsConverter<CharacterListFlags>))]
    public CharacterListFlags CharacterListFlags { get; set; }
    public ClientVersion RequiredClient { get; set; }

    [JsonConverter(typeof(FlagsConverter<HousingFlags>))]
    public HousingFlags HousingFlags { get; set; }
    public int MobileStatusVersion { get; set; }

    [JsonConverter(typeof(FlagsConverter<MapSelectionFlags>))]
    public MapSelectionFlags MapSelectionFlags { get; set; }

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
