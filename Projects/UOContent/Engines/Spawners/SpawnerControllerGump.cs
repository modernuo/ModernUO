using System;
using Server.Collections;
using Server.Network;
using Server.Engines.Spawners;

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

public class SapwnerControllerGump : Gump
{
    private BaseSpawner _copy;
    private int _page;
    private const int EntryCount = 5;
    private Mobile _mobile;
    private SpawnSearch _search;
    private int _lineCount;
    private BaseSpawner[] _spawners = Array.Empty<BaseSpawner>();
    private Grid _main;
    private const int TypeCount = 15;

    public static int GetButtonID(int type, int index) => 1 + type + index * TypeCount;

    public static void Initialize()
    {
        CommandSystem.Register("Spawn", AccessLevel.GameMaster, OpenSpawnGump_OnCommand);
    }

    public static void OpenSpawnGump_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new SapwnerControllerGump(e.Mobile));
    }

    public void DrawBorder(ListView list)
    {
        AddBackground(0, 0, _main.Width, _main.Height, 1755);

        AddImageTiled(0, _main.Rows[1].Y, _main.Width, 3, 1756);
        AddAlphaRegion(0, _main.Rows[1].Y, _main.Width, 3);

        AddImageTiled(0, _main.Rows[1].Y + 21, _main.Width, 3, 1756);
        AddAlphaRegion(0, _main.Rows[1].Y + 21, _main.Width, 3);

        for (int i = 0; i < list.ColsCount - 1; i++)
        {
            var x = list.Header.Cols[i].HEnd - 8;
            var y = list.Header.Y + 10;
            var height = 20;
            AddImageTiled(x, y, 3, height, 1758);
            AddAlphaRegion(x, y, 3, height);
        }

        AddImageTiled(0, _main.Rows[2].Y - 11, _main.Width, 6, 1756);
        AddImageTiled(0, _main.Rows[2].Y + 55, _main.Width, 6, 1756);
    }

    private static string ExtractCoords(Point3D cord) => $"X {cord.X} Y {cord.Y}";

    public void DrawSearch()
    {
        const int deltaX = 140;
        var search = SubGrid("search", _main.Name, 0, 0, 2, 1);

        AddLabelHtml(search.Columns[0].X, search.Rows[0].Y + 8, search.Columns[1].X, 30, "Search type", ColorCode.Gold);
        AddLabelHtml(search.Columns[1].X, search.Rows[0].Y + 8, search.Columns[1].X, 30, "Search Match", ColorCode.Gold);

        AddImageTiled(0, search.Rows[0].Y + 30, _main.Width, 3, 1756);
        AddImageTiled(search.Columns[1].X, search.Rows[0].Y, 3, search.Height, 1758);

        var creature = _search.Type == SpawnSearchType.Creature ? 9021 : 9020;
        var cords = _search.Type == SpawnSearchType.Coords ? 9021 : 9020;
        var props = _search.Type == SpawnSearchType.Props ? 9021 : 9020;
        var args = _search.Type == SpawnSearchType.Name ? 9021 : 9020;

        AddButton(search.Columns[0].X + 10, search.Rows[0].Y + 45, creature, creature, GetButtonID(1, 0));
        AddLabelHtml(search.Columns[0].X + 15, search.Rows[0].Y + 48, 100, 30, "Creature", ColorCode.White);


        AddButton(search.Columns[0].X + 10 + deltaX - 40, search.Rows[0].Y + 45, cords, cords, GetButtonID(1, 1));
        AddLabelHtml(search.Columns[0].X + 40 + deltaX - 40, search.Rows[0].Y + 48, 150, 30, "Cords", ColorCode.White, 4, false);

        AddButton(search.Columns[0].X + 10 + deltaX * 2 - 100, search.Rows[0].Y + 45, props, props, GetButtonID(1, 2));
        AddLabelHtml(search.Columns[0].X + 40 + deltaX * 2 - 100, search.Rows[0].Y + 48, 150, 30, "Creature Props", ColorCode.White, 4, false);

        AddButton(search.Columns[0].X + 10 + deltaX * 3 - 100, search.Rows[0].Y + 45, args, args, GetButtonID(1, 3));
        AddLabelHtml(search.Columns[0].X + 40 + deltaX * 3 - 100, search.Rows[0].Y + 48, 150, 30, "Spawner Name", ColorCode.White, 4, false);

        AddImageTiled(search.Columns[1].HCenter - 100, search.Rows[0].VCenter + 16, 200, 1, 9357);
        AddTextEntry(search.Columns[1].HCenter - 100, search.Rows[0].VCenter, 200, 20, (int)GridColor.White, 65535, _search.SearchPattern, GetButtonID(1, 4));
        AddButton(search.Columns[1].HCenter + 120, search.Rows[0].VCenter, 4023, 4024, GetButtonID(1, 4));

        var type = (int)_search.Type;
        if (type == 0)
        {
            AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spawner by entry creature name", ColorCode.White);
        }
        else if (type == 1)
        {
            AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "single int - range around", ColorCode.White);
        }
        else if (type == 2)
        {
            AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spanwer by entry property field", ColorCode.White);
        }
        else if (type == 3)
        {
            AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spanwer by name", ColorCode.White);
        }
    }

    private void SearchSpawner()
    {
        if (_search.SearchPattern.Length < 1)
        {
            return;
        }

        using var queue = new PooledRefQueue<BaseSpawner>();

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

            bool enqueue = _search.Type switch
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
        foreach (var entry in spawner.Entries)
        {
            if (entry.Properties?.InsensitiveContains(searchPattern) == true)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawFooter(ListView list)
    {
        if (list.CanNext)
        {
            AddButton(_main.Columns[0].HCenter + 60, _main.Rows[2].VCenter + 16, 4005, 4006, GetButtonID(10, 0));
        }

        if (list.CanBack)
        {
            AddButton(_main.Columns[0].HCenter + -60, _main.Rows[2].VCenter + 16, 4014, 4015, GetButtonID(11, 0));
        }

        //past to all spawners
        AddButton(_main.Columns[0].X + 15, _main.Rows[2].Y, 4029, 4031, GetButtonID(1, 5));
        AddLabelHtml(_main.Columns[0].X + 55, _main.Rows[2].Y + 4, _main.Columns[0].Width, 20, $"Paste props for {_spawners.Length} spawners", ColorCode.Orange, 4, false);

        //paste target
        AddButton(_main.Columns[0].X + 15, _main.Rows[2].Y + 28, 4029, 4031, GetButtonID(1, 6));
        AddLabelHtml(_main.Columns[0].X + 55, _main.Rows[2].Y + 32, _main.Columns[0].Width, 20, $"Paste entry for {_spawners.Length} spawners", ColorCode.Orange, 4, false);

        //paste ground
        AddButton(_main.Columns[0].X + 15 + 260, _main.Rows[2].Y, 4029, 4031, GetButtonID(1, 7));
        AddLabelHtml(_main.Columns[0].X + 55 + 260, _main.Rows[2].Y + 4, _main.Columns[0].Width, 20, "Paste copy to ground", ColorCode.Yellow, 4, false);

        AddLabelHtml(_main.Columns[0].X + 16, _main.Rows[2].VCenter + 17, _main.Columns[0].Width, 20, $"Pages {list.Page + 1}/{list.TotalPages + 1}", ColorCode.White);
        AddLabelHtml(_main.Columns[0].X + 20, _main.Rows[2].VCenter + 17, _main.Columns[0].Width, 20, $"Total Spawner {_spawners.Length}", ColorCode.White, 4, false);
    }


    public void DrawSpawner(ListView list)
    {
        for (int i = 0; i < list.Items.Count; i++)
        {
            var vCenter = 15;
            var perItem = 45;
            var spawner = _spawners[list.Items[i].Index];
            var item = list.Items[i];
            //AddLabelHtml(item.Cols[0].X, list.Items[i].Y + 10, list.Header.Cols[0].Width, 30, "priva", ColorCode.White);
            AddButton(item.Cols[0].X + 15, list.Items[i].Y + 5, 4011, 4012, GetButtonID(2, item.Index));
            AddLabelHtml(item.Cols[0].X, list.Items[i].Y + 26, item.Cols[0].Width, 30, "copy", ColorCode.White, 3);

            AddButton(item.Cols[1].X + 2, list.Items[i].Y + 5, 4029, 4031, GetButtonID(3, item.Index));
            AddLabelHtml(item.Cols[1].X + 3, list.Items[i].Y + 26, item.Cols[1].Width, 30, "props", ColorCode.White, 3, false);

            AddButton(item.Cols[1].HCenter - 2, list.Items[i].Y + 5, 4029, 4031, GetButtonID(4, item.Index));
            AddLabelHtml(item.Cols[1].HCenter - 3, list.Items[i].Y + 26, 55, 30, "entry", ColorCode.White, 3, false);

            //props
            AddLabelHtml(item.Cols[2].X, list.Items[i].Y + vCenter + 10, list.Header.Cols[2].Width, 30, spawner.Serial.ToString(), ColorCode.White);

            //map
            AddLabelHtml(item.Cols[3].X, list.Items[i].Y + vCenter, list.Header.Cols[3].Width, 30, spawner.Map.ToString(), ColorCode.White);

            //teleport button
            AddButton(item.Cols[4].X, list.Items[i].Y + vCenter - 4, 0x10, 0x10, GetButtonID(5, item.Index));
            AddButton(item.Cols[4].X + 20, list.Items[i].Y + vCenter - 4, 0x10, 0x10, GetButtonID(5, item.Index));
            AddImage(item.Cols[4].X, list.Items[i].Y + vCenter - 5, 0x638, 936);
            AddImage(item.Cols[4].X + 50, list.Items[i].Y + vCenter - 5, 0x638, 936);
            AddLabelHtml(item.Cols[4].X, list.Items[i].Y + vCenter, list.Header.Cols[4].Width, 30, ExtractCoords(spawner.Location), ColorCode.White);

            //open entry button
            AddButton(item.Cols[5].X + 6, list.Items[i].Y + vCenter - 4, 0x2635, 0x2635, GetButtonID(7, item.Index));
            AddImage(item.Cols[5].X + 6, list.Items[i].Y + vCenter - 7, 0x2635, 827);
            AddLabelHtml(item.Cols[5].X, list.Items[i].Y + vCenter, list.Header.Cols[5].Width, 30, spawner.Entries?.Count.ToString(), ColorCode.White);


            //entry
            var entry = i * EntryCount;

            AddImageTiled(item.Cols[2].X - 2, list.Items[i].Y + 18, item.Cols[2].Width, 1, 9357);
            AddTextEntry(item.Cols[2].X, list.Items[i].Y + 2, list.Header.Cols[2].Width, 80, (int)GridColor.White, entry, spawner.Name, 20);
            AddButton(item.Cols[2].X, list.Items[i].Y + vCenter + 9, 5837, 5838, GetButtonID(6, item.Index));

            AddImageTiled(item.Cols[6].X + 8, list.Items[i].Y + 29, item.Cols[6].Width / 2, 1, 9357);
            AddTextEntry(item.Cols[6].X + 12, list.Items[i].Y + 13, list.Header.Cols[6].Width, 80, (int)GridColor.White, entry + 1, spawner.WalkingRange.ToString(), 2);

            AddImageTiled(item.Cols[7].X + 8, list.Items[i].Y + 29, item.Cols[7].Width / 2, 1, 9357);
            AddTextEntry(item.Cols[7].X + 12, list.Items[i].Y + 13, list.Header.Cols[7].Width, 80, (int)GridColor.White, entry + 2, spawner.HomeRange.ToString(), 2);

            AddImageTiled(item.Cols[8].X + 6, list.Items[i].Y + 29, item.Cols[8].Width - 20, 1, 9357);
            AddTextEntry(item.Cols[8].X + 8, list.Items[i].Y + 13, list.Header.Cols[8].Width, 80, (int)GridColor.White, entry + 3, spawner.MinDelay.ToString(), 8);

            AddImageTiled(item.Cols[9].X + 6, list.Items[i].Y + 29, item.Cols[9].Width - 20, 1, 9357);
            AddTextEntry(item.Cols[9].X + 8, list.Items[i].Y + 13, list.Header.Cols[9].Width, 80, (int)GridColor.White, entry + 4, spawner.MaxDelay.ToString(), 8);
            //end entry

            AddLabelHtml(item.Cols[10].X, list.Items[i].Y + vCenter, list.Header.Cols[10].Width, 30, spawner.NextSpawn.ToString(@"hh\:mm\:ss"), ColorCode.White);


            AddButton(item.Cols[11].X, list.Items[i].Y + 5, 4023, 4025, GetButtonID(8, item.Index));
            AddLabelHtml(item.Cols[11].X + 4, list.Items[i].Y + 26, 55, 30, "save", ColorCode.White, 3, false);

            AddButton(item.Cols[11].X + perItem, list.Items[i].Y + 5, 4017, 4018, GetButtonID(9, item.Index));
            AddLabelHtml(item.Cols[11].X + perItem, list.Items[i].Y + 26, 55, 30, "delete", ColorCode.White, 3, false);

            AddImageTiled(0, list.Items[i].Y + list.ColHeight, _main.Width, 1, 1756);
            AddAlphaRegion(0, list.Items[i].Y + list.ColHeight, _main.Width, 1);
        }
    }

    public void DrawColumnName(ListView list)
    {
        AddLabelHtml(list.Header.Cols[0].X, list.Header.Y + 10, list.Header.Cols[0].Width, 30, "Copy", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[1].X, list.Header.Y + 10, list.Header.Cols[1].Width, 30, "Paste", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[2].X, list.Header.Y + 10, list.Header.Cols[2].Width, 30, "Name/Serial", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[3].X, list.Header.Y + 10, list.Header.Cols[3].Width, 30, "Map", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[4].X, list.Header.Y + 10, list.Header.Cols[4].Width, 30, "Coords", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[5].X, list.Header.Y + 10, list.Header.Cols[5].Width, 30, "Entry", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[6].X, list.Header.Y + 10, list.Header.Cols[6].Width, 30, "Walk", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[7].X, list.Header.Y + 10, list.Header.Cols[7].Width, 30, "Home", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[8].X, list.Header.Y + 10, list.Header.Cols[8].Width, 30, "Min Delay", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[9].X, list.Header.Y + 10, list.Header.Cols[9].Width, 30, "Max Delay", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[10].X, list.Header.Y + 10, list.Header.Cols[10].Width, 30, "Next Spawn", ColorCode.Gold);
        AddLabelHtml(list.Header.Cols[11].X, list.Header.Y + 10, list.Header.Cols[11].Width, 30, "Actions", ColorCode.Gold);
    }

    public SapwnerControllerGump(Mobile mobile, int page = 0, BaseSpawner copy = null, SpawnSearch search = null) : base(20, 30)
    {
        _main = Grid("main", 1000, 800, 1, 3, rowSize: "10* * 100");
        _mobile = mobile;
        _page = page;
        _copy = copy;
        _search = search ?? new SpawnSearch();

        SearchSpawner();

        var _list = AddListView(_main.Name, 0, 1, _spawners.Length, page, 45, 12, colSize: "6* 8* 18* 6* 12* 6* 5* 5* 8* 8* 9* *", headerHeight: 30, marginY: -7);
        _lineCount = _list.LineCount;
        DrawBorder(_list);
        DrawSearch();
        DrawSpawner(_list);
        DrawColumnName(_list);
        DrawFooter(_list);

    }
    public void CopyProperty(BaseSpawner spawner, BaseSpawner target)
    {
        target.Name = spawner.Name;
        target.MinDelay = spawner.MinDelay;
        target.MaxDelay = spawner.MaxDelay;
        target.WalkingRange = spawner.WalkingRange;
        target.HomeRange = spawner.HomeRange;
        target.Group = spawner.Group;
        target.Count = spawner.Count;
    }
    public void CopyEntry(BaseSpawner spawner, BaseSpawner target)
    {
        if (spawner.Entries?.Count > 0)
        {
            target.Entries?.Clear();

            for (int i = 0; i < spawner.Entries.Count; i++)
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
    public void Refresh(bool update = true)
    {
        if (update)
        {
            _mobile.SendGump(new SapwnerControllerGump(_mobile, _page, _copy, _search));
        }
        else
        {
            _mobile.SendGump(this);
        }
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        var buttonID = info.ButtonID - 1;
        var type = buttonID % TypeCount;
        var index = buttonID / TypeCount;

        if (type == -1)
        {
            return;
        }

        switch (type)
        {
            case 1:
                {
                    _page = 0;
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

                        if (entry?.Text?.Length > 0)
                        {
                            _search.SearchPattern = entry.Text;
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
                        newSpawner.Map = _mobile.Map;
                        newSpawner.Location = _mobile.Location;
                        newSpawner.Stop();
                        newSpawner.Start();
                        newSpawner.Respawn();
                    }

                    break;
                }
            //copy
            case 2:
                {
                    if (index < _spawners.Length)
                    {
                        _copy = _spawners[index];
                    }

                    break;
                }
            //paste props
            case 3:
                {
                    if (index < _spawners.Length && _copy != null)
                    {
                        CopyProperty(_copy, _spawners[index]);
                    }

                    break;
                }
            //paste entry
            case 4:
                {
                    if (index < _spawners.Length && _copy != null)
                    {
                        CopyEntry(_copy, _spawners[index]);
                    }

                    break;
                }
            //save
            case 8:
                {
                    if (index < _spawners.Length)
                    {
                        var spawner = _spawners[index];

                        var indexEntry = index >= _lineCount ? (index - _lineCount * _page) * EntryCount : index * EntryCount;

                        var name = info.GetTextEntry(indexEntry);
                        if (name?.Text.Length > 0)
                        {
                            spawner.Name = name.Text;
                        }

                        if (int.TryParse(info.GetTextEntry(indexEntry + 1)?.Text, out var walkRange))
                        {
                            spawner.WalkingRange = walkRange;
                        }

                        if (int.TryParse(info.GetTextEntry(indexEntry + 2)?.Text, out var homeHange))
                        {
                            spawner.HomeRange = homeHange;
                        }

                        if (TimeSpan.TryParse(info.GetTextEntry(indexEntry + 3)?.Text, out var minDelay))
                        {
                            spawner.MinDelay = minDelay;
                        }

                        if (TimeSpan.TryParse(info.GetTextEntry(indexEntry + 4)?.Text, out var maxDelay))
                        {
                            spawner.MaxDelay = maxDelay;
                        }
                    }

                    break;
                }
            //go
            case 5:
                {
                    if (index < _spawners.Length)
                    {
                        var spawner = _spawners[index];
                        _mobile.Map = spawner.Map;
                        _mobile.Location = spawner.Location;
                    }
                    Refresh(false);
                    return;
                }
            //open entry
            case 6:
            case 7:
                {
                    if (index < _spawners.Length)
                    {
                        var spawner = _spawners[index];
                        Refresh();

                        if (type == 7)
                        {
                            _mobile.SendGump(new SpawnerGump(spawner));
                        }
                        else
                        {
                            _mobile.SendGump(new PropertiesGump(_mobile, spawner));
                        }

                        return;
                    }

                    break;
                }
            //delete
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
                    break;
                }
            case 11:
                {
                    if (_page > 0)
                    {
                        _page--;
                    }

                    break;
                }
        }

        Refresh();
    }
}
