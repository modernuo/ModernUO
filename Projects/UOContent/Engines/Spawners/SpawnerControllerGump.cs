using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Server.Commands;
using Server.Network;
using Server.Engines.Spawners;
using Server.Items;
using Server.Misc;
using Server.Multis;


namespace Server.Gumps
{
    public enum SpawnSearchType
    {
        Creature = 0,
        Cords,
        Props,
        Name
    }
    public class SpawnSearch
    {
        public SpawnSearchType Type { get; set; } = SpawnSearchType.Creature;
        public string SearchPatern { get; set; } = "";
    }
    public class SapwnerControllerGump : Gump
    {
        private BaseSpawner _copy;
        private int _page = 0;
        private const int _entryCount = 5;
        private readonly Mobile _mobile;
        private readonly SpawnSearch _search;
        private readonly int lineCount = 0;
        private BaseSpawner[] _spawners = new BaseSpawner[0];
        private readonly Grid _main;
        private const int _typeCount = 15;
        public int GetButtonID(int type, int index) => 1 + type + index * _typeCount;
        public static void Initialize()
        {
            CommandSystem.Register("spawn", AccessLevel.GameMaster, Open);
        }
        public static void Open(CommandEventArgs e)
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

        string extractCord(Point3D cord)
        {
            return $"X {cord.X} Y {cord.Y}";
        }

        public void DrawSearch()
        {
            var deltaX = 140;
            var search = SubGrid("search", _main.Name, 0, 0, 2, 1);
            //AddImageTiled(0, search.Rows[1].Y, _main.Width, 6, 1756);
            AddLabelHtml(search.Columns[0].X, search.Rows[0].Y + 8, search.Columns[1].X, 30, "Search type", ColorCode.Gold);
            AddLabelHtml(search.Columns[1].X, search.Rows[0].Y + 8, search.Columns[1].X, 30, "Search Match", ColorCode.Gold);

            AddImageTiled(0, search.Rows[0].Y + 30, _main.Width, 3, 1756);
            AddImageTiled(search.Columns[1].X, search.Rows[0].Y, 3, search.Height, 1758);



            var creature = _search.Type == SpawnSearchType.Creature ? 9021 : 9020;
            var cords = _search.Type == SpawnSearchType.Cords ? 9021 : 9020;
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
            AddTextEntry(search.Columns[1].HCenter - 100, search.Rows[0].VCenter, 200, 20, (int)GridColor.White, 65535, _search.SearchPatern, GetButtonID(1, 4));
            AddButton(search.Columns[1].HCenter + 120, search.Rows[0].VCenter, 4023, 4024, GetButtonID(1, 4));

            var type = (int)_search.Type;
            if (type == 0) AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spanwer by entrty creature name", ColorCode.White, 4);
            else if (type == 1) AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "single int - range around", ColorCode.White, 4);
            else if (type == 2) AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spanwer by entry property field", ColorCode.White, 4);
            else if (type == 3) AddLabelHtml(search.Columns[1].X, search.Rows[0].VCenter + 20, search.Columns[1].Width, 30, "search spanwer by name", ColorCode.White, 4);



        }
        private void searchSpawner()
        {
            if (_search.SearchPatern.Length < 1)
                return;

            var copyArray = World.Items.Where(_ => _.Value is BaseSpawner).Select(_ => (_.Value as BaseSpawner));

            if (_search.Type == SpawnSearchType.Creature)
            {
                _spawners = copyArray.Where(_ => _.Entries.Any(_ => _.SpawnedName?.Contains(_search.SearchPatern, StringComparison.InvariantCultureIgnoreCase) ?? false))?.ToArray();
            }
            else if (_search.Type == SpawnSearchType.Cords)
            {
                int range = -1;
                if (int.TryParse(_search.SearchPatern, out range))
                {
                    _spawners = _mobile.GetItemsInRange<BaseSpawner>(range).ToArray();
                }
            }
            else if (_search.Type == SpawnSearchType.Props)
            {
                _spawners = copyArray.Where(_ => _.Entries.Any(_ => _.Properties?.Contains(_search.SearchPatern, StringComparison.InvariantCultureIgnoreCase) ?? false))?.ToArray();
            }
            else if (_search.Type == SpawnSearchType.Name)
            {
                _spawners = copyArray.Where(_ => _.Name.Contains(_search.SearchPatern, StringComparison.InvariantCultureIgnoreCase))?.ToArray();
            }
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
            AddLabelHtml(_main.Columns[0].X + 55 + 260, _main.Rows[2].Y + 4, _main.Columns[0].Width, 20, $"Paste copy to ground", ColorCode.Yellow, 4, false);

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
                AddLabelHtml(item.Cols[2].X, list.Items[i].Y + vCenter + 10, list.Header.Cols[2].Width, 30, spawner.Serial.ToString(), ColorCode.White, 4);


                //map
                AddLabelHtml(item.Cols[3].X, list.Items[i].Y + vCenter, list.Header.Cols[3].Width, 30, spawner.Map.ToString(), ColorCode.White);

                //teleport button
                AddButton(item.Cols[4].X, list.Items[i].Y + vCenter - 4, 0x10, 0x10, GetButtonID(5, item.Index));
                AddButton(item.Cols[4].X + 20, list.Items[i].Y + vCenter - 4, 0x10, 0x10, GetButtonID(5, item.Index));
                AddImage(item.Cols[4].X, list.Items[i].Y + vCenter - 5, 0x638, 936);
                AddImage(item.Cols[4].X + 50, list.Items[i].Y + vCenter - 5, 0x638, 936);
                AddLabelHtml(item.Cols[4].X, list.Items[i].Y + vCenter, list.Header.Cols[4].Width, 30, extractCord(spawner.Location), ColorCode.White, 4);

                //open entry button
                AddButton(item.Cols[5].X + 6, list.Items[i].Y + vCenter - 4, 0x2635, 0x2635, GetButtonID(7, item.Index));
                AddImage(item.Cols[5].X + 6, list.Items[i].Y + vCenter - 7, 0x2635, 827);
                AddLabelHtml(item.Cols[5].X, list.Items[i].Y + vCenter, list.Header.Cols[5].Width, 30, spawner.Entries?.Count.ToString(), ColorCode.White, 4);


                //entry
                var entry = i * _entryCount;

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

                AddLabelHtml(item.Cols[10].X, list.Items[i].Y + vCenter, list.Header.Cols[10].Width, 30, spawner.NextSpawn.ToString(@"hh\:mm\:ss"), ColorCode.White, 4);


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

            searchSpawner();

            var _list = AddListView(_main.Name, 0, 1, _spawners.Length, page, 45, 12, colSize: "6* 8* 18* 6* 12* 6* 5* 5* 8* 8* 9* *", headerHeight: 30, marginY: -7);
            lineCount = _list.LineCount;
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
            if (update) _mobile.SendGump(new SapwnerControllerGump(_mobile, _page, _copy, _search));
            else _mobile.SendGump(this);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var buttonID = info.ButtonID - 1;
            var type = buttonID % _typeCount;
            var index = buttonID / _typeCount;

            if (type == -1)
                return;

            if (type == 1)
            {
                _page = 0;
                if (index < 4)
                {
                    _search.Type = (SpawnSearchType)index;
                    var isNumeric = int.TryParse(_search.SearchPatern, out var result);
                    if (index == 1 && !isNumeric || index != 1 && isNumeric) _search.SearchPatern = "";
                }
                else if (index == 4 && info.GetTextEntry(65535) is TextRelay entry && entry.Text.Length > 0)
                {
                    _search.SearchPatern = entry.Text;
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
                    var newSpawner = new Spawner() as BaseSpawner;
                    FullCopy(_copy, newSpawner);
                    newSpawner.Map = _mobile.Map;
                    newSpawner.Location = _mobile.Location;
                    newSpawner.Stop();
                    newSpawner.Start();
                    newSpawner.Respawn();
                }
            }
            //copy
            else if (type == 2)
            {
                if (index < _spawners.Length)
                {
                    _copy = _spawners[index];
                }
            }
            //paste props
            else if (type == 3)
            {
                if (index < _spawners.Length && _copy != null)
                {
                    CopyProperty(_copy, _spawners[index]);
                }
            }
            //paste entry
            else if (type == 4)
            {
                if (index < _spawners.Length && _copy != null)
                {
                    CopyEntry(_copy, _spawners[index]);
                }
            }
            //save
            else if (type == 8)
            {
                if (index < _spawners.Length)
                {
                    var spawner = _spawners[index];

                    var indexEntry = index >= lineCount ? (index - lineCount * _page) * _entryCount : index * _entryCount;

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
            }
            //go
            else if (type == 5)
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
            else if (type == 6 || type == 7)
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
            }
            //delete
            else if (type == 9)
            {
                if (index < _spawners.Length)
                    _spawners[index].Delete();
            }
            else if (type == 10 || type == 11)
            {
                if (type == 10)
                {
                    _page++;
                }
                else if (_page > 0)
                {
                    _page--;
                }
            }

            Refresh();
        }
    }
}
