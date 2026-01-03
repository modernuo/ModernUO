/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpawnerControllerGump.cs                                        *
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
using Server.Collections;
using Server.Engines.Spawners;
using Server.Network;

namespace Server.Gumps;

public enum SpawnSearchType
{
    Creature = 0,
    Coords,
    Props,
    Name
}

public class SpawnSearch
{
    public SpawnSearchType Type { get; set; } = SpawnSearchType.Creature;
    public string SearchPattern { get; set; } = "";
}

public class SpawnerControllerGump : DynamicGump
{
    // Grid dimensions
    private const int GumpWidth = 1000;
    private const int GumpHeight = 800;
    private const int RowHeight = 45;
    private const int HeaderHeight = 30;
    private const int EntryCount = 5;
    private const int TypeCount = 15;

    // Column spec for the 12-column list
    private const string ColumnSpec = "6* 8* 18* 6* 12* 6* 5* 5* 8* 8* 9* *";
    // Row spec for main grid: search (10%), list (*), footer (100px)
    private const string MainRowSpec = "10* * 100";

    private readonly Mobile _mobile;
    private int _page;
    private readonly SpawnSearch _search;
    private BaseSpawner _copy;
    private BaseSpawner[] _spawners = Array.Empty<BaseSpawner>();
    private int _lineCount;

    public static int GetButtonID(int type, int index) => 1 + type + index * TypeCount;

    public static void Configure()
    {
        CommandSystem.Register("SpawnAdmin", AccessLevel.GameMaster, OpenSpawnGump_OnCommand);
    }

    [Usage("Spawn")]
    [Aliases("Spawn")]
    [Description("Opens the spawn administration gump.")]
    public static void OpenSpawnGump_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new SpawnerControllerGump(e.Mobile));
    }

    public SpawnerControllerGump(Mobile mobile, int page = 0, BaseSpawner copy = null, SpawnSearch search = null)
        : base(20, 30)
    {
        _mobile = mobile;
        _page = page;
        _copy = copy;
        _search = search ?? new SpawnSearch();

        SearchSpawner();
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        // Background
        builder.AddBackground(0, 0, GumpWidth, GumpHeight, 5054);
        builder.AddImageTiled(0, 0, GumpWidth, GumpHeight, 2624);
        builder.AddAlphaRegion(0, 0, GumpWidth, GumpHeight);

        // Calculate main grid: 1 column x 3 rows
        Span<int> mainRowPos = stackalloc int[3];
        Span<int> mainRowHeights = stackalloc int[3];

        Span<GridSizeSpec> mainRowSpecs = stackalloc GridSizeSpec[3];
        GridSizeSpec.ParseAll(MainRowSpec, mainRowSpecs);
        GridCalculator.ComputeTrackSizes(mainRowSpecs, GumpHeight, 0, mainRowPos, mainRowHeights);

        // Calculate list view layout
        Span<int> listColPos = stackalloc int[12];
        Span<int> listColWidths = stackalloc int[12];

        var listView = ListViewLayout.Create(
            0,
            mainRowPos[1],
            GumpWidth,
            mainRowHeights[1] - 7, // marginY adjustment
            _spawners.Length,
            _page,
            RowHeight,
            HeaderHeight,
            ColumnSpec,
            listColPos,
            listColWidths);

        _lineCount = listView.ItemsPerPage;

        // Draw borders
        var row1Y = mainRowPos[1];
        var row2Y = mainRowPos[2];
        builder.AddImageTiled(0, row1Y, GumpWidth, 3, 9357);
        builder.AddAlphaRegion(0, row1Y, GumpWidth, 3);
        builder.AddImageTiled(0, row1Y + 21, GumpWidth, 3, 9357);
        builder.AddAlphaRegion(0, row1Y + 21, GumpWidth, 3);

        for (var i = 0; i < listView.ColumnCount - 1; i++)
        {
            var x = listColPos[i] + listColWidths[i] - 8;
            var y = mainRowPos[1] + 10;
            builder.AddImageTiled(x, y, 3, 20, 9355);
            builder.AddAlphaRegion(x, y, 3, 20);
        }

        builder.AddImageTiled(0, row2Y - 11, GumpWidth, 6, 9357);
        builder.AddAlphaRegion(0, row2Y - 11, GumpWidth, 6);
        builder.AddImageTiled(0, row2Y + 55, GumpWidth, 6, 9357);
        builder.AddAlphaRegion(0, row2Y + 55, GumpWidth, 6);

        // Draw search area
        DrawSearch(ref builder, mainRowPos[0], mainRowHeights[0]);

        // Draw column headers
        ReadOnlySpan<string> headers =
        [
            "Copy", "Paste", "Name/Serial", "Map", "Coords",
            "Entry", "Walk", "Home", "Min Delay", "Max Delay",
            "Next Spawn", "Actions"
        ];

        for (var i = 0; i < listView.ColumnCount && i < headers.Length; i++)
        {
            var cell = listView.GetHeaderCell(i, listColPos, listColWidths);
            var offsetX = i >= 4 ? -4 : 0;
            builder.AddHtml(cell.X + offsetX, cell.Y + 10, cell.Width, 30, headers[i], GridColors.Gold, 4, align: TextAlignment.Center);
        }

        // Draw spawner rows
        const int vCenter = 15;
        for (var i = 0; i < listView.VisibleCount; i++)
        {
            var dataIndex = listView.GetDataIndex(i);
            var spawner = _spawners[dataIndex];
            var rowY = listView.GetRowY(i);

            // Column 0: Copy button
            var col0X = listColPos[0];
            var col0Width = listColWidths[0];
            builder.AddButton(col0X + 15, rowY + 5, 4011, 4012, GetButtonID(2, dataIndex));
            builder.AddHtml(col0X, rowY + 26, col0Width, 30, "copy", GridColors.White, 3, align: TextAlignment.Center);

            // Column 1: Paste buttons
            var col1X = listColPos[1];
            var col1Width = listColWidths[1];
            var col1HCenter = col1X + col1Width / 2;
            builder.AddButton(col1X + 2, rowY + 5, 4029, 4031, GetButtonID(3, dataIndex));
            builder.AddHtml(col1X + 3, rowY + 26, col1Width, 30, "props", GridColors.White, 3);

            builder.AddButton(col1HCenter - 2, rowY + 5, 4029, 4031, GetButtonID(4, dataIndex));
            builder.AddHtml(col1HCenter - 3, rowY + 26, 55, 30, "entry", GridColors.White, 3);

            // Column 2: Name/Serial
            var col2X = listColPos[2];
            var col2Width = listColWidths[2];
            var entryIndex = i * EntryCount;

            builder.AddImageTiled(col2X - 2, rowY + 18, col2Width - 8, 1, 9357);
            builder.AddTextEntry(col2X, rowY + 2, col2Width - 8, 80, (int)GridHues.White, entryIndex, spawner.Name);
            builder.AddButton(col2X, rowY + vCenter + 9, 5837, 5838, GetButtonID(6, dataIndex));
            builder.AddHtml(col2X, rowY + vCenter + 10, col2Width, 30, spawner.Serial.ToString(), GridColors.White, 4);

            // Column 3: Map
            var col3X = listColPos[3];
            var col3Width = listColWidths[3];
            builder.AddHtml(col3X, rowY + vCenter, col3Width, 30, spawner.Map.ToString(), GridColors.White, 4, align: TextAlignment.Center);

            // Column 4: Coords with teleport button
            var col4X = listColPos[4];
            var col4Width = listColWidths[4];
            builder.AddButton(col4X - 5, rowY + vCenter, 2062, 2062, GetButtonID(5, dataIndex));
            builder.AddImage(col4X - 5, rowY + vCenter, 2062, 936);
            builder.AddHtml(col4X - 7, rowY + vCenter, col4Width, 30, $"X {spawner.Location.X} Y {spawner.Location.Y}", GridColors.White, 4);

            // Column 5: Entry count with button
            var col5X = listColPos[5];
            var col5Width = listColWidths[5];
            builder.AddButton(col5X + 6, rowY + vCenter - 4, 0x2635, 0x2635, GetButtonID(7, dataIndex));
            builder.AddImage(col5X + 6, rowY + vCenter - 7, 0x2635, 827);
            builder.AddHtml(col5X, rowY + vCenter, col5Width, 30, spawner.Entries?.Count.ToString() ?? "0", GridColors.White, 4, align: TextAlignment.Center);

            // Column 6: Walk Range
            var col6X = listColPos[6];
            var col6Width = listColWidths[6];
            builder.AddImageTiled(col6X + 8, rowY + 29, col6Width / 2, 1, 9357);
            builder.AddTextEntry(col6X + 12, rowY + 13, col6Width, 80, (int)GridHues.White, entryIndex + 1, spawner.WalkingRange.ToString());

            // Column 7: Home Range
            var col7X = listColPos[7];
            var col7Width = listColWidths[7];
            if (spawner.IsHomeRangeStyle)
            {
                builder.AddImageTiled(col7X + 8, rowY + 29, col7Width / 2, 1, 9357);
                builder.AddTextEntry(col7X + 12, rowY + 13, col7Width, 80, (int)GridHues.White, entryIndex + 2, spawner.HomeRange.ToString());
            }
            else
            {
                builder.AddHtml(col7X - 6, rowY + 13, col7Width, 30, "Custom", GridColors.Yellow, 4, align: TextAlignment.Center);
            }

            // Column 8: Min Delay
            var col8X = listColPos[8];
            var col8Width = listColWidths[8];
            builder.AddImageTiled(col8X + 6, rowY + 29, col8Width - 20, 1, 9357);
            builder.AddTextEntry(col8X + 8, rowY + 13, col8Width, 80, (int)GridHues.White, entryIndex + 3, spawner.MinDelay.ToString());

            // Column 9: Max Delay
            var col9X = listColPos[9];
            var col9Width = listColWidths[9];
            builder.AddImageTiled(col9X + 6, rowY + 29, col9Width - 20, 1, 9357);
            builder.AddTextEntry(col9X + 8, rowY + 13, col9Width, 80, (int)GridHues.White, entryIndex + 4, spawner.MaxDelay.ToString());

            // Column 10: Next Spawn
            var col10X = listColPos[10];
            var col10Width = listColWidths[10];
            builder.AddHtml(col10X, rowY + vCenter, col10Width, 30, spawner.NextSpawn.ToString(@"hh\:mm\:ss"), GridColors.White, 4, align: TextAlignment.Center);

            // Column 11: Actions
            var col11X = listColPos[11];
            builder.AddButton(col11X, rowY + 5, 4023, 4025, GetButtonID(8, dataIndex));
            builder.AddHtml(col11X + 4, rowY + 26, 55, 30, "save", GridColors.White, 3);

            builder.AddButton(col11X + RowHeight, rowY + 5, 4017, 4018, GetButtonID(9, dataIndex));
            builder.AddHtml(col11X + RowHeight, rowY + 26, 55, 30, "delete", GridColors.White, 3);

            // Row separator
            builder.AddImageTiled(0, rowY + RowHeight, GumpWidth, 1, 9357);
            builder.AddAlphaRegion(0, rowY + RowHeight, GumpWidth, 1);
        }

        // Draw footer
        var col0HCenter = GumpWidth / 2;
        var row2VCenter = row2Y + mainRowHeights[2] / 2;

        if (listView.CanGoNext)
        {
            builder.AddButton(col0HCenter + 60, row2VCenter + 16, 4005, 4006, GetButtonID(10, 0));
        }

        if (listView.CanGoBack)
        {
            builder.AddButton(col0HCenter - 60, row2VCenter + 16, 4014, 4015, GetButtonID(11, 0));
        }

        builder.AddButton(15, row2Y, 4029, 4031, GetButtonID(1, 5));
        builder.AddHtml(55, row2Y + 4, GumpWidth, 20, $"Paste props for {_spawners.Length} spawners", GridColors.Orange, 4);

        builder.AddButton(15, row2Y + 28, 4029, 4031, GetButtonID(1, 6));
        builder.AddHtml(55, row2Y + 32, GumpWidth, 20, $"Paste entry for {_spawners.Length} spawners", GridColors.Orange, 4);

        builder.AddButton(275, row2Y, 4029, 4031, GetButtonID(1, 7));
        builder.AddHtml(315, row2Y + 4, GumpWidth, 20, "Paste copy to the ground", GridColors.Yellow, 4);

        builder.AddHtml(16, row2VCenter + 17, GumpWidth, 20, $"Pages {listView.CurrentPage + 1}/{listView.TotalPages}", GridColors.White, 4);
        builder.AddHtml(120, row2VCenter + 17, GumpWidth, 20, $"Total Spawners {_spawners.Length}", GridColors.White, 4);
    }

    private void DrawSearch(ref DynamicGumpBuilder builder, int row0Y, int searchHeight)
    {
        // Create 2-column subgrid for search area
        Span<int> searchColPos = stackalloc int[2];
        Span<int> searchColWidths = stackalloc int[2];
        GridCalculator.ComputeUniformTrackSizes(2, GumpWidth, 0, searchColPos, searchColWidths);

        var col0X = searchColPos[0];
        var col1X = searchColPos[1];
        var col1HCenter = col1X + searchColWidths[1] / 2;
        var row0VCenter = row0Y + searchHeight / 2;

        const int deltaX = 140;

        // Labels
        builder.AddHtml(col0X, row0Y + 8, searchColWidths[0], 30, "Search type", GridColors.Gold, 4, align: TextAlignment.Center);
        builder.AddHtml(col1X, row0Y + 8, searchColWidths[1], 30, "Search Match", GridColors.Gold, 4, align: TextAlignment.Center);

        // Separators
        builder.AddImageTiled(0, row0Y + 30, GumpWidth, 3, 9357);
        builder.AddAlphaRegion(0, row0Y + 30, GumpWidth, 3);
        builder.AddImageTiled(col1X, row0Y, 3, searchHeight, 9355);
        builder.AddAlphaRegion(col1X, row0Y, 3, searchHeight);

        // Search type buttons
        var creature = _search.Type == SpawnSearchType.Creature ? 9021 : 9020;
        var coords = _search.Type == SpawnSearchType.Coords ? 9021 : 9020;
        var props = _search.Type == SpawnSearchType.Props ? 9021 : 9020;
        var name = _search.Type == SpawnSearchType.Name ? 9021 : 9020;

        builder.AddButton(col0X + 10, row0Y + 45, creature, creature, GetButtonID(1, 0));
        builder.AddHtml(col0X + 15, row0Y + 48, 100, 30, "Creature", GridColors.White, 4, align: TextAlignment.Center);

        builder.AddButton(col0X + 10 + deltaX - 40, row0Y + 45, coords, coords, GetButtonID(1, 1));
        builder.AddHtml(col0X + 40 + deltaX - 40, row0Y + 48, 150, 30, "Range", GridColors.White, 4);

        builder.AddButton(col0X + 10 + deltaX * 2 - 100, row0Y + 45, props, props, GetButtonID(1, 2));
        builder.AddHtml(col0X + 40 + deltaX * 2 - 100, row0Y + 48, 150, 30, "Creature Props", GridColors.White, 4);

        builder.AddButton(col0X + 10 + deltaX * 3 - 100, row0Y + 45, name, name, GetButtonID(1, 3));
        builder.AddHtml(col0X + 40 + deltaX * 3 - 100, row0Y + 48, 150, 30, "Spawner Name", GridColors.White, 4);

        // Search text entry
        builder.AddImageTiled(col1HCenter - 100, row0VCenter + 16, 200, 1, 9357);
        builder.AddTextEntry(col1HCenter - 100, row0VCenter, 200, 20, (int)GridHues.White, 0xFFFF, _search.SearchPattern);
        builder.AddButton(col1HCenter + 120, row0VCenter, 4023, 4024, GetButtonID(1, 4));

        // Search type description
        var description = (int)_search.Type switch
        {
            0 => "Search by creature name",
            1 => "Search by range (number)",
            2 => "Search by entry property field",
            3 => "Search by spawner name",
            _ => ""
        };
        builder.AddHtml(col1X, row0VCenter + 20, searchColWidths[1], 30, description, GridColors.White, 4, align: TextAlignment.Center);
    }

    private void SearchSpawner()
    {
        if (_search.SearchPattern.Length < 1)
        {
            return;
        }

        using var queue = PooledRefQueue<BaseSpawner>.Create();

        if (!(_search.Type == SpawnSearchType.Coords && int.TryParse(_search.SearchPattern, out var range)))
        {
            range = -1;
        }

        foreach (var item in World.Items.Values)
        {
            if (item is not BaseSpawner spawner)
            {
                continue;
            }

            var enqueue = _search.Type switch
            {
                SpawnSearchType.Creature => SearchSpawnerCreatures(spawner, _search.SearchPattern),
                SpawnSearchType.Coords   => _mobile.InRange(spawner.Location, range),
                SpawnSearchType.Props    => SearchSpawnerProperties(spawner, _search.SearchPattern),
                SpawnSearchType.Name     => spawner.Name?.InsensitiveContains(_search.SearchPattern) == true,
                _                        => false
            };

            if (enqueue)
            {
                queue.Enqueue(spawner);
            }
        }

        _spawners = queue.ToArray();
    }

    private static bool SearchSpawnerCreatures(BaseSpawner spawner, string searchPattern)
    {
        foreach (var entry in spawner.Entries)
        {
            if (entry.SpawnedName?.InsensitiveContains(searchPattern) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool SearchSpawnerProperties(BaseSpawner spawner, string searchPattern)
    {
        for (var i = 0; i < spawner.Entries.Count; i++)
        {
            var entry = spawner.Entries[i];
            if (entry.Properties?.InsensitiveContains(searchPattern) == true)
            {
                return true;
            }
        }

        return false;
    }

    public static void CopyProperty(BaseSpawner spawner, BaseSpawner target)
    {
        target.Name = spawner.Name;
        target.MinDelay = spawner.MinDelay;
        target.MaxDelay = spawner.MaxDelay;
        target.WalkingRange = spawner.WalkingRange;
        target.SpawnBounds = spawner.SpawnBounds;
        target.SpawnLocationIsHome = spawner.SpawnLocationIsHome;
        target.Group = spawner.Group;
        target.Count = spawner.Count;
    }

    public static void CopyEntry(BaseSpawner spawner, BaseSpawner target)
    {
        if (spawner.Entries?.Count > 0)
        {
            target.Entries?.Clear();

            for (var i = 0; i < spawner.Entries.Count; i++)
            {
                var item = spawner.Entries[i];
                var targetEntry = target.AddEntry(item.SpawnedName, item.SpawnedProbability, item.SpawnedMaxCount);
                targetEntry.Properties = item.Properties;
                targetEntry.Parameters = item.Parameters;
            }
        }
    }

    public void FullCopy(BaseSpawner spawner, BaseSpawner target)
    {
        CopyProperty(spawner, target);
        CopyEntry(spawner, target);
    }

    public void Refresh(Mobile mobile, bool reSearch = true)
    {
        if (reSearch)
        {
            SearchSpawner();
        }

        mobile.SendGump(this);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var mobile = sender.Mobile;
        var buttonID = info.ButtonID - 1;
        var type = buttonID % TypeCount;
        var index = buttonID / TypeCount;

        switch (type)
        {
            case -1:
                return;

            case 1:
            {
                if (index < 4)
                {
                    _search.Type = (SpawnSearchType)index;
                    var isNumeric = int.TryParse(_search.SearchPattern, out _);
                    if (index == 1 && !isNumeric || index != 1 && isNumeric)
                    {
                        _search.SearchPattern = "";
                    }
                }
                else if (index == 4)
                {
                    var entry = info.GetTextEntry(0xFFFF);
                    if (entry?.Length > 0)
                    {
                        _search.SearchPattern = entry;
                    }
                }
                else if (index == 5 && _copy != null && _spawners?.Length > 0)
                {
                    foreach (var spawner in _spawners)
                    {
                        CopyProperty(_copy, spawner);
                    }
                }
                else if (index == 6 && _copy != null && _spawners?.Length > 0)
                {
                    foreach (var spawner in _spawners)
                    {
                        CopyEntry(_copy, spawner);
                    }
                }
                else if (index == 7 && _copy != null)
                {
                    var newSpawner = new Spawner();
                    FullCopy(_copy, newSpawner);
                    newSpawner.Map = mobile.Map;
                    newSpawner.Location = mobile.Location;
                    newSpawner.Stop();
                    newSpawner.Start();
                    newSpawner.Respawn();
                }

                _page = 0;
                Refresh(mobile);
                return;
            }

            case 2:
            {
                if (index < _spawners.Length)
                {
                    _copy = _spawners[index];
                }
                break;
            }

            case 3:
            {
                if (index < _spawners.Length && _copy != null)
                {
                    CopyProperty(_copy, _spawners[index]);
                }
                break;
            }

            case 4:
            {
                if (index < _spawners.Length && _copy != null)
                {
                    CopyEntry(_copy, _spawners[index]);
                }
                break;
            }

            case 5:
            {
                if (index < _spawners.Length)
                {
                    var spawner = _spawners[index];
                    mobile.Map = spawner.Map;
                    mobile.Location = spawner.Location;
                }
                Refresh(mobile, reSearch: false);
                return;
            }

            case 6:
            case 7:
            {
                if (index < _spawners.Length)
                {
                    var spawner = _spawners[index];
                    Refresh(mobile);

                    if (type == 7)
                    {
                        mobile.SendGump(new SpawnerGump(spawner));
                    }
                    else
                    {
                        mobile.SendGump(new PropertiesGump(mobile, spawner));
                    }
                    return;
                }
                break;
            }

            case 8:
            {
                if (index < _spawners.Length)
                {
                    var spawner = _spawners[index];
                    var entryIndex = index >= _lineCount ? (index - _lineCount * _page) * EntryCount : index * EntryCount;

                    var name = info.GetTextEntry(entryIndex);
                    if (name?.Length > 0)
                    {
                        spawner.Name = name;
                    }

                    if (int.TryParse(info.GetTextEntry(entryIndex + 1), out var walkRange))
                    {
                        spawner.WalkingRange = walkRange;
                    }

                    if (int.TryParse(info.GetTextEntry(entryIndex + 2), out var homeRange))
                    {
                        spawner.HomeRange = homeRange;
                    }

                    if (TimeSpan.TryParse(info.GetTextEntry(entryIndex + 3), out var minDelay))
                    {
                        spawner.MinDelay = minDelay;
                    }

                    if (TimeSpan.TryParse(info.GetTextEntry(entryIndex + 4), out var maxDelay))
                    {
                        spawner.MaxDelay = maxDelay;
                    }
                }
                break;
            }

            case 9:
            {
                if (index < _spawners.Length)
                {
                    _spawners[index].Delete();
                }
                break;
            }

            case 10:
            {
                _page++;
                Refresh(mobile, reSearch: false);
                return;
            }

            case 11:
            {
                if (_page > 0)
                {
                    _page--;
                }
                Refresh(mobile, reSearch: false);
                return;
            }
        }

        Refresh(mobile);
    }
}
