using System;
using System.Runtime.CompilerServices;

namespace Server.Engines.AdvancedSearch;

[Flags]
public enum AdvancedSearchFilterOptions : long
{
    None,
    FilterType = 0x00000001,
    FilterName = 0x00000002,
    FilterRange = 0x00000004,
    FilterRegion = 0x00000008,
    FilterPropertyTest = 0x00000040,
    FilterInternalMap = 0x00000080,
    FilterNullMap = 0x00000100,
    FilterAge = 0x00000200,
    FilterAgeDirection = 0x00000400,
    HideValidInternalMap = 0x00000800,
    FilterFelucca = 0x00001000,
    FilterTrammel = 0x00002000,
    FilterIlshenar = 0x00004000,
    FilterMalas = 0x00008000,
    FilterTokuno = 0x00010000,
    FilterTerMur = 0x00020000,
}

public record AdvancedSearchFilter
{
    private AdvancedSearchFilterOptions _options;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GetOptionsFlag(AdvancedSearchFilterOptions option) => (_options & option) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetOptionsFlag(AdvancedSearchFilterOptions option, bool value) =>
        _options = value ? _options | option : _options & ~option;

    public bool FilterType
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterType);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterType, value);
    }

    public bool FilterName
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterName);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterName, value);
    }

    public bool FilterRange
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterRange);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterRange, value);
    }

    public bool FilterRegion
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterRegion);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterRegion, value);
    }

    public bool FilterPropertyTest
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterPropertyTest);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterPropertyTest, value);
    }

    public bool FilterInternalMap
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterInternalMap);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterInternalMap, value);
    }

    public bool FilterNullMap
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterNullMap);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterNullMap, value);
    }

    public bool FilterAge
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterAge);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterAge, value);
    }

    public bool FilterAgeDirection
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterAgeDirection);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterAgeDirection, value);
    }

    public bool HideValidInternalMap
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.HideValidInternalMap);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.HideValidInternalMap, value);
    }

    public bool FilterFelucca
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterFelucca);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterFelucca, value);
    }

    public bool FilterTrammel
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterTrammel);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterTrammel, value);
    }

    public bool FilterIlshenar
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterIlshenar);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterIlshenar, value);
    }

    public bool FilterMalas
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterMalas);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterMalas, value);
    }

    public bool FilterTokuno
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterTokuno);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterTokuno, value);
    }

    public bool FilterTerMur
    {
        get => GetOptionsFlag(AdvancedSearchFilterOptions.FilterTerMur);
        set => SetOptionsFlag(AdvancedSearchFilterOptions.FilterTerMur, value);
    }

    // Must be older or younger than this Age based on FilterAgeDirection
    public TimeSpan? Age { get; set; }

    public int? Range { get; set; }

    public string? RegionName { get; set; }

    public string? PropertyTest { get; set; }

    public Type? Type { get; set; }

    public string? Name { get; set; }
}
