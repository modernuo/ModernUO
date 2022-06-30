using System;
using System.Linq;
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

public class SpawnerControllerGump : Gump
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
    public int GetButtonID(int type, int index) => 1 + type + index * TypeCount;

    public static void Initialize()
    {
        CommandSystem.Register("spawn", AccessLevel.GameMaster, Open);
    }
    public static void Open(CommandEventArgs e)
    {
        e.Mobile.SendGump(new SpawnerControllerGump(e.Mobile));
    }

    public void DrawBorder(GumpList gumpList)
    {
        AddBackground(0, 0, _main.Width, _main.Height, 1755);

        AddImageTiled(0, _main.Rows[1].Y, _main.Width, 3, 1756);
        AddAlphaRegion(0, _main.Rows[1].Y, _main.Width, 3);

        AddImageTiled(0, _main.Rows[1].Y + 21, _main.Width, 3, 1756);
        AddAlphaRegion(0, _main.Rows[1].Y + 21, _main.Width, 3);
        for (int i = 0; i < gumpList.ColsCount - 1; i++)
        {
            var x = gumpList.Header.Cols[i].H_End - 8;
            var y = gumpList.Header.Y + 10;
            var height = 20;
            AddImageTiled(x, y, 3, height, 1758);
            AddAlphaRegion(x, y, 3, height);
        }
        AddImageTiled(0, _main.Rows[2].Y-11, _main.Width, 6, 1756);
        AddImageTiled(0, _main.Rows[2].Y + 55, _main.Width, 6, 1756);
    }

    private string GetCoordinates(Point3D p) => $"X {p.X} Y {p.Y}";

    public void DrawSearch()
    {
        const int deltaX = 140;

        var search = SubGrid("search", _main.Name, 0, 0, 2, 1);
        var column0 = search.Columns[0];
        var row0 = search.Rows[0];
        var column1 = search.Columns[1];

        //AddImageTiled(0, search.Rows[1].Y, _main.Width, 6, 1756);
        AddLabelHtml(column0.X, row0.Y + 8, column1.X, 30, "Search type", ColorCode.Gold);
        AddLabelHtml(column1.X, row0.Y + 8, column1.X, 30, "Search Match", ColorCode.Gold);

        AddImageTiled(0, row0.Y + 30, _main.Width, 3, 1756);
        AddImageTiled(column1.X, row0.Y, 3, search.Height, 1758);

        var creature = _search.Type == SpawnSearchType.Creature ? 9021 : 9020;
        var cords = _search.Type == SpawnSearchType.Coords ? 9021 : 9020;
        var props = _search.Type == SpawnSearchType.Props ? 9021 : 9020;
        var args = _search.Type == SpawnSearchType.Name ? 9021 : 9020;

        AddButton(column0.X + 10, row0.Y + 45, creature, creature, GetButtonID(1,0));
        AddLabelHtml(column0.X + 15, row0.Y + 48, 100, 30, "Creature", ColorCode.White);


        AddButton(column0.X + 10 + deltaX - 40, row0.Y + 45, cords, cords, GetButtonID(1, 1));
        AddLabelHtml(column0.X + 40 + deltaX - 40, row0.Y + 48, 150, 30, "Cords", ColorCode.White, 4, false);

        AddButton(column0.X + 10 + deltaX * 2-100, row0.Y + 45, props, props, GetButtonID(1, 2));
        AddLabelHtml(column0.X + 40 + deltaX * 2- 100, row0.Y + 48, 150, 30, "Creature Props", ColorCode.White, 4, false);

        AddButton(column0.X + 10 + deltaX * 3 - 100, row0.Y + 45, args, args, GetButtonID(1, 3));
        AddLabelHtml(column0.X + 40 + deltaX * 3 - 100, row0.Y + 48, 150, 30, "Spawner Name", ColorCode.White, 4, false);


        AddImageTiled(column1.H_Center - 100, row0.VCenter + 16, 200, 1, 9357);
        AddTextEntry(column1.H_Center - 100, row0.VCenter, 200, 20, 2394, 65535, _search.SearchPattern, GetButtonID(1, 4));
        AddButton(column1.H_Center + 120, row0.VCenter, 4023, 4024, GetButtonID(1, 4));

        var type = (int)_search.Type;
        if (type == 0)
        {
            AddLabelHtml(column1.X, row0.VCenter + 20, column1.Width, 30, "search spanwer by entry creature name", ColorCode.White);
        }
        else if (type == 1)
        {
            AddLabelHtml(column1.X, row0.VCenter + 20, column1.Width, 30, "single int - range around", ColorCode.White);
        }
        else if (type == 2)
        {
            AddLabelHtml(column1.X, row0.VCenter + 20, column1.Width, 30, "search spanwer by entry property field", ColorCode.White);
        }
        else if (type == 3)
        {
            AddLabelHtml(column1.X, row0.VCenter + 20, column1.Width, 30, "search spanwer by name", ColorCode.White);
        }
    }
    private void SearchSpawner()
    {
        if (_search.SearchPattern.Length < 1)
        {
            return;
        }

        var copyArray = World.Items.Where(_ => _.Value is BaseSpawner).Select(_ => _.Value as BaseSpawner);

        if (_search.Type == SpawnSearchType.Creature)
        {
            _spawners = copyArray.Where(_ => _.Entries.Any(_ => _.SpawnedName?.Contains(_search.SearchPattern, StringComparison.InvariantCultureIgnoreCase) ?? false))?.ToArray();
        }
        else if (_search.Type == SpawnSearchType.Coords)
        {
            int range = -1;
            if (int.TryParse(_search.SearchPattern, out range))
            {
                _spawners = _mobile.GetItemsInRange<BaseSpawner>(range).ToArray();
            }
        }
        else if (_search.Type == SpawnSearchType.Props)
        {
            _spawners = copyArray.Where(_ => _.Entries.Any(_ => _.Properties?.Contains(_search.SearchPattern, StringComparison.InvariantCultureIgnoreCase) ?? false))?.ToArray();
        }
        else if (_search.Type == SpawnSearchType.Name)
        {
            _spawners = copyArray.Where(_ => _.Name.Contains(_search.SearchPattern, StringComparison.InvariantCultureIgnoreCase))?.ToArray();
        }
    }
    private void DrawFooter(GumpList gumpList)
    {
        if (gumpList.CanNext)
        {
            AddButton(_main.Columns[0].H_Center+60, _main.Rows[2].VCenter+16, 4005, 4006, GetButtonID(10, 0));
        }

        if (gumpList.CanBack)
        {
            AddButton(_main.Columns[0].H_Center+-60, _main.Rows[2].VCenter+16, 4014, 4015, GetButtonID(11, 0));
        }

        //past to all spawners
        AddButton(_main.Columns[0].X + 15, _main.Rows[2].Y, 4029, 4031, GetButtonID(1, 5));
        AddLabelHtml(_main.Columns[0].X + 55, _main.Rows[2].Y + 4, _main.Columns[0].Width, 20, $"Paste props for {_spawners.Length} spawners", ColorCode.Orange, 4, false);

        //paste target
        AddButton(_main.Columns[0].X + 15, _main.Rows[2].Y+28, 4029, 4031, GetButtonID(1, 6));
        AddLabelHtml(_main.Columns[0].X + 55, _main.Rows[2].Y+32, _main.Columns[0].Width, 20, $"Paste entry for {_spawners.Length} spawners", ColorCode.Orange, 4, false);

        //paste ground
        AddButton(_main.Columns[0].X + 15+260, _main.Rows[2].Y, 4029, 4031, GetButtonID(1, 7));
        AddLabelHtml(_main.Columns[0].X + 55+260, _main.Rows[2].Y + 4, _main.Columns[0].Width, 20, $"Paste copy to ground", ColorCode.Yellow, 4, false);

        AddLabelHtml(_main.Columns[0].X+16, _main.Rows[2].VCenter + 17, _main.Columns[0].Width, 20, $"Pages {gumpList.Page + 1}/{gumpList.TotalPages + 1}", ColorCode.White);
        AddLabelHtml(_main.Columns[0].X+20, _main.Rows[2].VCenter + 17, _main.Columns[0].Width, 20, $"Total Spawner {_spawners.Length}", ColorCode.White,4,false);
    }


    public void DrawSpawner(GumpList gumpList)
    {

        for (int i = 0; i < gumpList.Items.Count; i++)
        {
            const int vCenter = 15;
            const int perItem = 45;
            var spawner = _spawners[gumpList.Items[i].Index];
            var item = gumpList.Items[i];
            //AddLabelHtml(item.Cols[0].X, list.Items[i].Y + 10, list.Header.Cols[0].Width, 30, "priva", ColorCode.White);
            AddButton(item.Cols[0].X + 15, gumpList.Items[i].Y + 5, 4011, 4012, GetButtonID(2, item.Index));
            AddLabelHtml(item.Cols[0].X, gumpList.Items[i].Y + 26, item.Cols[0].Width, 30, "copy", ColorCode.White, 3);

            AddButton(item.Cols[1].X + 2, gumpList.Items[i].Y + 5, 4029, 4031, GetButtonID(3, item.Index));
            AddLabelHtml(item.Cols[1].X + 3, gumpList.Items[i].Y + 26, item.Cols[1].Width, 30, "props", ColorCode.White, 3, false);

            AddButton(item.Cols[1].H_Center - 2, gumpList.Items[i].Y + 5, 4029, 4031, GetButtonID(4, item.Index));
            AddLabelHtml(item.Cols[1].H_Center - 3, gumpList.Items[i].Y + 26, 55, 30, "entry", ColorCode.White, 3, false);

            //props
            AddLabelHtml(item.Cols[2].X, gumpList.Items[i].Y + vCenter + 10, gumpList.Header.Cols[2].Width, 30, spawner.Serial.ToString(), ColorCode.White);


            //map
            AddLabelHtml(item.Cols[3].X, gumpList.Items[i].Y + vCenter, gumpList.Header.Cols[3].Width, 30, spawner.Map.ToString(), ColorCode.White);

            //teleport button
            AddButton(item.Cols[4].X, gumpList.Items[i].Y + vCenter-4, 0x10, 0x10, GetButtonID(5, item.Index));
            AddButton(item.Cols[4].X+20, gumpList.Items[i].Y + vCenter-4, 0x10, 0x10, GetButtonID(5, item.Index));
            AddImage(item.Cols[4].X, gumpList.Items[i].Y + vCenter-5, 0x638, 936);
            AddImage(item.Cols[4].X+50, gumpList.Items[i].Y + vCenter-5, 0x638, 936);
            AddLabelHtml(item.Cols[4].X, gumpList.Items[i].Y + vCenter, gumpList.Header.Cols[4].Width, 30, GetCoordinates(spawner.Location), ColorCode.White);

            //open entry button
            AddButton(item.Cols[5].X+6, gumpList.Items[i].Y + vCenter - 4, 0x2635, 0x2635, GetButtonID(7, item.Index));
            AddImage(item.Cols[5].X+6, gumpList.Items[i].Y + vCenter - 7, 0x2635, 827);
            AddLabelHtml(item.Cols[5].X, gumpList.Items[i].Y + vCenter, gumpList.Header.Cols[5].Width, 30, spawner.Entries?.Count.ToString(), ColorCode.White);


            //entry
            var entry = i * EntryCount;

            AddImageTiled(item.Cols[2].X - 2, gumpList.Items[i].Y + 18, item.Cols[2].Width, 1, 9357);
            AddTextEntry(item.Cols[2].X, gumpList.Items[i].Y + 2, gumpList.Header.Cols[2].Width, 80, 2394, entry, spawner.Name, 20);
            AddButton(item.Cols[2].X, gumpList.Items[i].Y + vCenter +9, 5837, 5838, GetButtonID(6, item.Index));

            AddImageTiled(item.Cols[6].X+8, gumpList.Items[i].Y + 29, item.Cols[6].Width/2, 1, 9357);
            AddTextEntry(item.Cols[6].X+12, gumpList.Items[i].Y  + 13, gumpList.Header.Cols[6].Width, 80, 2394, entry + 1, spawner.WalkingRange.ToString(), 2);

            AddImageTiled(item.Cols[7].X + 8, gumpList.Items[i].Y + 29, item.Cols[7].Width / 2, 1, 9357);
            AddTextEntry(item.Cols[7].X + 12, gumpList.Items[i].Y + 13, gumpList.Header.Cols[7].Width, 80, 2394, entry + 2, spawner.HomeRange.ToString(), 2);

            AddImageTiled(item.Cols[8].X + 6, gumpList.Items[i].Y + 29, item.Cols[8].Width-20, 1, 9357);
            AddTextEntry(item.Cols[8].X + 8, gumpList.Items[i].Y + 13, gumpList.Header.Cols[8].Width, 80, 2394, entry + 3, spawner.MinDelay.ToString(), 8);

            AddImageTiled(item.Cols[9].X + 6, gumpList.Items[i].Y + 29, item.Cols[9].Width - 20, 1, 9357);
            AddTextEntry(item.Cols[9].X + 8, gumpList.Items[i].Y + 13, gumpList.Header.Cols[9].Width, 80, 2394, entry + 4, spawner.MaxDelay.ToString(), 8);
            //end entry

            AddLabelHtml(item.Cols[10].X, gumpList.Items[i].Y + vCenter, gumpList.Header.Cols[10].Width, 30, spawner.NextSpawn.ToString(@"hh\:mm\:ss"), ColorCode.White);


            AddButton(item.Cols[11].X, gumpList.Items[i].Y + 5, 4023, 4025, GetButtonID(8, item.Index));
            AddLabelHtml(item.Cols[11].X + 4, gumpList.Items[i].Y + 26, 55, 30, "save", ColorCode.White, 3, false);

            AddButton(item.Cols[11].X + perItem, gumpList.Items[i].Y + 5, 4017, 4018, GetButtonID(9, item.Index));
            AddLabelHtml(item.Cols[11].X + perItem, gumpList.Items[i].Y + 26, 55, 30, "delete", ColorCode.White, 3, false);

            AddImageTiled(0, gumpList.Items[i].Y + gumpList.ColHeight, _main.Width, 1, 1756);
            AddAlphaRegion(0, gumpList.Items[i].Y + gumpList.ColHeight, _main.Width, 1);
        }

    }

    public void DrawColumnName(GumpList gumpList)
    {
        AddLabelHtml(gumpList.Header.Cols[0].X, gumpList.Header.Y + 10, gumpList.Header.Cols[0].Width, 30, "Copy", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[1].X, gumpList.Header.Y + 10, gumpList.Header.Cols[1].Width, 30, "Paste", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[2].X, gumpList.Header.Y + 10, gumpList.Header.Cols[2].Width, 30, "Name/Serial", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[3].X, gumpList.Header.Y + 10, gumpList.Header.Cols[3].Width, 30, "Map", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[4].X, gumpList.Header.Y + 10, gumpList.Header.Cols[4].Width, 30, "Coords", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[5].X, gumpList.Header.Y + 10, gumpList.Header.Cols[5].Width, 30, "Entry", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[6].X, gumpList.Header.Y + 10, gumpList.Header.Cols[6].Width, 30, "Walk", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[7].X, gumpList.Header.Y + 10, gumpList.Header.Cols[7].Width, 30, "Home", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[8].X, gumpList.Header.Y + 10, gumpList.Header.Cols[8].Width, 30, "Min Delay", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[9].X, gumpList.Header.Y + 10, gumpList.Header.Cols[9].Width, 30, "Max Delay", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[10].X, gumpList.Header.Y + 10, gumpList.Header.Cols[10].Width, 30, "Next Spawn", ColorCode.Gold);
        AddLabelHtml(gumpList.Header.Cols[11].X, gumpList.Header.Y + 10, gumpList.Header.Cols[11].Width, 30, "Actions", ColorCode.Gold);
    }

    public SpawnerControllerGump(Mobile mobile, int page = 0, BaseSpawner copy = null, SpawnSearch search = null) : base(20, 30)
    {
        _main = Grid("main", 1000, 800, 1, 3, rowSize: "10* * 100");
        _mobile = mobile;
        _page = page;
        _copy = copy;
        _search = search ?? new SpawnSearch();

        SearchSpawner();

        var _list = AddListView(
            _main.Name,
            0,
            1,
            _spawners.Length,
            page,
            45,
            12,
            colSize: "6* 8* 18* 6* 12* 6* 5* 5* 8* 8* 9* *",
            headerHeight: 30,
            marginY: -7
        );

        _lineCount = _list.LineCount;

        DrawBorder(_list);
        DrawSearch();
        DrawSpawner(_list);
        DrawColumnName(_list);
        DrawFooter(_list);

    }
    public static void CopyProperty(BaseSpawner spawner, BaseSpawner target)
    {
        target.MinDelay = spawner.MinDelay;
        target.MaxDelay = spawner.MaxDelay;
        target.WalkingRange = spawner.WalkingRange;
        target.HomeRange = spawner.HomeRange;
        target.Group = spawner.Group;
    }

    public static void CopyEntry(BaseSpawner spawner, BaseSpawner target)
    {
        if (spawner.Entries?.Count > 0)
        {
            target.UpdateEntries(spawner.Entries);
        }
    }

    public static void FullCopy(BaseSpawner spawner, BaseSpawner target)
    {
        CopyProperty(spawner, target);
        CopyEntry(spawner, target);
    }
    public void Refresh(bool update = true)
    {
        if (update)
        {
            _mobile.SendGump(new SpawnerControllerGump(_mobile, _page, _copy, _search));
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
                        var isNumeric = int.TryParse(_search.SearchPattern, out var _);
                        if (index == 1 && !isNumeric || index != 1 && isNumeric)
                        {
                            _search.SearchPattern = "";
                        }
                    }
                    else if (index == 4)
                    {
                        var entry = info.GetTextEntry(0xFFFF);
                        if (entry?.Text.Length > 0)
                        {
                            _search.SearchPattern = entry.Text;
                        }
                    }
                    else if (_copy != null)
                    {
                        if (index == 5)
                        {
                            foreach (var spawner in _spawners)
                            {
                                CopyProperty(_copy, spawner);
                            }
                        }
                        else if (index == 6)
                        {
                            foreach (var spawner in _spawners)
                            {
                                CopyEntry(_copy, spawner);
                            }
                        }
                        else if (index == 7)
                        {
                            var newSpawner = new Spawner();
                            FullCopy(_copy, newSpawner);
                            newSpawner.Map = _mobile.Map;
                            newSpawner.Location = _mobile.Location;
                        }
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

                        if (int.TryParse(info.GetTextEntry(indexEntry + 1)?.Text,out var walkRange))
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
