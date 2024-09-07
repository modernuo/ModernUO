using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using CommunityToolkit.HighPerformance;
using Server.Commands;
using Server.Commands.Generic;
using Server.Engines.Spawners;
using Server.Gumps;
using Server.Network;
using Server.Saves;

namespace Server.Engines.AdvancedSearch;

[Flags]
public enum AdvancedSearchGumpOptions : long
{
    None,
    SortDescending = 0x00000001,
    SortByType = 0x00000002,
    SortByName = 0x00000004,
    SortByRange = 0x00000008,
    SortByMap = 0x00000010,
    SortBySelected = 0x00000020,
    AllSelected = 0x00000040
}

public class AdvancedSearchGump : Gump
{
    private const int MaxEntries = 18;

    private static int _threadId;
    private static AdvancedSearchThreadWorker[] _threadWorkers;

    private static void Configure()
    {
        EventSink.Shutdown += Shutdown;
        EventSink.ServerCrashed += OnCrashed;
    }

    private static void OnCrashed(ServerCrashedEventArgs obj)
    {
        Shutdown();
    }

    private static void Shutdown()
    {
        if (_threadWorkers == null)
        {
            return;
        }

        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            _threadWorkers[i].Exit();
        }
    }

    public static readonly AdvancedSearchFilter DefaultFilter = new()
    {
        FilterType = true,
        FilterFelucca = true,
        HideValidInternalMap = true,
        Type = typeof(Spawner),
    };

    private AdvancedSearchGumpOptions _options;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GetOptionsFlag(AdvancedSearchGumpOptions option) => (_options & option) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetOptionsFlag(AdvancedSearchGumpOptions option, bool value) =>
        _options = value ? _options | option : _options & ~option;

    public bool SortDescending
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortDescending);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortDescending, value);
    }

    public bool SortByType
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortByType);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortByType, value);
    }

    public bool SortByName
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortByName);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortByName, value);
    }

    public bool SortByRange
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortByRange);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortByRange, value);
    }

    public bool SortByMap
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortByMap);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortByMap, value);
    }

    public bool SortBySelected
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.SortBySelected);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.SortBySelected, value);
    }

    public bool AllSelected
    {
        get => GetOptionsFlag(AdvancedSearchGumpOptions.AllSelected);
        set => SetOptionsFlag(AdvancedSearchGumpOptions.AllSelected, value);
    }

    public AdvancedSearchFilter Filter { get; set; } = DefaultFilter with {};

    public AdvancedSearchResult[] SearchResults { get; set; }
    public int DisplayFrom { get; set; }
    public string CommandString { get; set; }

    public AdvancedSearchGump() : base(50, 50) => Build();

    private void Build()
    {
        const int height = 500;
        var haveResults = SearchResults?.Length > 0;
        var width = haveResults ? 755 : 170;

        AddBackground(0, 0, width, height, 5054);
        AddAlphaRegion(0, 0, width, height);

        /// Sorting

        var y = 5;

        AddButton(5, y, 0xFAB, 0xFAD, 700);
        AddLabel(38, y, 0x384, "Sort");

        if (SortDescending)
        {
            AddButton(75, y + 3, 0x15E2, 0x15E6, 701);
            AddLabel(95, y, 0x384, "Desc");
        }
        else
        {
            AddButton(75, y + 3, 0x15E0, 0x15E4, 701);
            AddLabel(95, y, 0x384, "Asc");
        }

        y += 22;
        AddRadio(5, y, 0xD2, 0xD3, SortByType, 0);
        AddLabel(28, y, 0x384, "Type");

        AddRadio(75, y, 0xD2, 0xD3, SortByName, 1);
        AddLabel(98, y, 0x384, "Name");

        y += 20;
        AddRadio(5, y, 0xD2, 0xD3, SortByRange, 2);
        AddLabel(28, y, 0x384, "Range");

        AddRadio(75, y, 0xD2, 0xD3, SortByMap, 3);
        AddLabel(98, y, 0x384, "Map");

        y += 20;
        AddRadio(5, y, 0xD2, 0xD3, SortBySelected, 4);
        AddLabel(28, y, 0x384, "Select");

        /// Searching

        y = 85;
        AddButton(5, y, 0xFA8, 0xFAA, 3);
        AddLabel(38, y, 0x384, "Search");

        y += 20;
        AddCheck(5, y, 0xD2, 0xD3, Filter!.FilterInternalMap, 312);
        AddLabel(28, y, 0x384, "Internal");
        AddCheck(75, y, 0xD2, 0xD3, Filter.FilterNullMap, 314);
        AddLabel(98, y, 0x384, "Null");

        y += 20;
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterFelucca, 308);
        AddLabel(28, y, 0x384, "Fel");
        AddCheck(75, y, 0xD2, 0xD3, Filter.FilterTrammel, 309);
        AddLabel(98, y, 0x384, "Tram");

        y += 20;
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterMalas, 310);
        AddLabel(28, y, 0x384, "Mal");
        AddCheck(75, y, 0xD2, 0xD3, Filter.FilterIlshenar, 311);
        AddLabel(98, y, 0x384, "Ilsh");

        y += 20;
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterTokuno, 318);
        AddLabel(28, y, 0x384, "Tok");
        AddCheck(75, y, 0xD2, 0xD3, Filter.FilterTerMur, 320);
        AddLabel(98, y, 0x384, "Ter");

        y += 20;
        AddCheck(5, y, 0xD2, 0xD3, Filter.HideValidInternalMap, 316);
        AddLabel(28, y, 0x384, "Hide valid internal");

        /// Filter

        y = height - 295;

        AddLabel(28, y, 0x384, "Region");
        AddImageTiled(70, y, 68, 19, 0xBBC);
        AddTextEntry(70, y, 250, 19, 0, 106, Filter.RegionName);
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterRegion, 319);

        y += 20;
        AddLabel(28, y, 0x384, "Age");
        AddImageTiled(70, y, 68, 19, 0xBBC);
        AddTextEntry(70, y, 250, 19, 0, 105, Filter.Age.ToString());
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterAge, 303);
        AddCheck(50, y + 2, 0x1467, 0x1468, Filter.FilterAgeDirection, 302);

        y += 20;
        AddLabel(28, y, 0x384, "Range");
        AddImageTiled(70, y, 45, 19, 0xBBC);
        AddTextEntry(70, y, 45, 19, 0, 100, Filter.Range.ToString());
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterRange, 304);

        y += 20;
        AddLabel(28, y, 0x384, "Type");
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterType, 305);
        AddImageTiled(6, y + 20, 132, 19, 0xBBC);
        AddTextEntry(6, y + 20, 250, 19, 0, 101, Filter.Type?.Name);

        y += 41;
        AddLabel(28, y, 0x384, "Property Test");
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterPropertyTest, 315);
        AddImageTiled(6, y + 20, 132, 19, 0xBBC);
        AddTextEntry(6, y + 20, 500, 19, 0, 104, Filter.PropertyTest);

        y += 41;
        AddLabel(28, y, 0x384, "Name");
        AddCheck(5, y, 0xD2, 0xD3, Filter.FilterName, 306);
        AddImageTiled(6, y + 20, 132, 19, 0xBBC);
        AddTextEntry(6, y + 20, 250, 19, 0, 102, Filter.Name);

        if (!haveResults)
        {
            return;
        }

        /// Control Buttons

        y = height - 25;

        AddButton(150, y, 0xFB1, 0xFB3, 156);
        AddLabel(183, y, 0x384, "Delete");

        // For spawners in the list
        AddButton(230, y, 0xFA2, 0xFA3, 157);
        AddLabel(263, y, 0x384, "Reset");

        AddButton(310, y, 0xFA8, 0xFAA, 158);
        AddLabel(343, y, 0x384, "Respawn");

        AddButton(5, y, 0xFAE, 0xFAF, 154);
        AddLabel(38, y, 0x384, "Bring");

        AddButton(470, y - 25, 0xFA8, 0xFAA, 160);
        AddLabel(503, y - 25, 0x384, "Command:");

        AddImageTiled(560, y - 25, 180, 19, 0xBBC);
        AddTextEntry(560, y - 25, 180, 19, 0, 301, CommandString);

        if (DisplayFrom > 0)
        {
            AddButton(395, height - 25, 0x15E3, 0x15E7, 202); // backward
        }

        if (DisplayFrom + MaxEntries < SearchResults.Length)
        {
            AddButton(415 + 25, height - 25, 0x15E1, 0x15E5, 201); // forward
        }

        if (SearchResults?.Length > 0)
        {
            var maxEntryIndex = Math.Min(DisplayFrom + MaxEntries, SearchResults.Length - 1);

            /// Headers
            AddLabel(143, 5, 0x384, "Gump");
            AddLabel(178, 5, 0x384, "Prop");
            AddLabel(210, 5, 0x384, "Goto");
            AddLabel(250, 5, 0x384, "Name");
            AddLabel(365, 5, 0x384, "Type");
            AddLabel(460, 5, 0x384, "Location");
            AddLabel(578, 5, 0x384, "Map");
            AddLabel(650, 5, 0x384, "Owner");

            AddLabel(180, y - 50, 68, $"Found {SearchResults.Length} items/mobiles");
            AddLabel(400, y - 50, 68,
                $"Displaying {DisplayFrom + 1}-{maxEntryIndex + 1}"
            );

            // Count the number of selected objects
            int count = 0;
            foreach (AdvancedSearchResult e in SearchResults)
            {
                if (e.Selected)
                {
                    count++;
                }
            }

            AddLabel(600, y - 50, 33, $"Selected {count}");

            AddLabel(610, y, 0x384, "Select All");

            // display the select-all toggle
            AddButton(670, y, AllSelected ? 0xD3 : 0xD2, AllSelected ? 0xD2 : 0xD3, 1);

            var allDisplayedSelected = true;

            for (int i = 0; i < MaxEntries; i++)
            {
                var offset = SortDescending ? MaxEntries - 1 - i : i;
                var index = offset + DisplayFrom;

                if (index >= SearchResults.Length)
                {
                    break;
                }

                var entry = SearchResults[index];

                if (!entry.Selected)
                {
                    allDisplayedSelected = false;
                }

                AddImageTiled(235, 22 * i + 30, 386, 23, 0x52);
                AddImageTiled(236, 22 * i + 31, 384, 21, 0xBBC);

                if (entry.Entity is BaseSpawner)
                {
                    AddButton(145, 22 * i + 30, 0xFBD, 0xFBE, 2000 + offset);
                }

                // Goto button
                AddButton(205, 22 * i + 30, 0xFAE, 0xFAF, 1000 + offset);

                // Interface button
                AddButton(175, 22 * i + 30, 0xFAB, 0xFAD, 3000 + offset);

                var textHue = 0;
                var parentName = "";
                var deleted = entry.Entity?.Deleted != false;
                if (deleted)
                {
                    entry.Entity = null; // Release this so we don't have hanging references
                }

                var loc = entry.GetLocation();
                var map = entry.GetMap();

                if (entry.Parent is Mobile parentMob)
                {
                    textHue = parentMob.Player ? 44 : 24;
                    parentName = parentMob.Name;
                }
                else if (entry.Parent is Item parentItem)
                {
                    textHue = 5;
                    parentName = parentItem.Name ?? parentItem.ItemData.Name;
                }

                // Name
                AddLabelCropped(248, 22 * i + 31, 110, 21, deleted ? 5 : 0, entry.Name);

                // Type
                AddImageTiled(360, 22 * i + 31, 90, 21, 0xBBC);
                AddLabelCropped(360, 22 * i + 31, 90, 21, 0, entry.Type.Name);

                // Location
                AddImageTiled(450, 22 * i + 31, 137, 21, 0xBBC);
                AddLabel(450, 22 * i + 31, 0, loc.ToString());

                // Map
                AddImageTiled(571, 22 * i + 31, 70, 21, 0xBBC);
                AddLabel(571, 22 * i + 31, 0, map?.Name ?? "(-null-)");

                // Parent
                AddImageTiled(640, 22 * i + 31, 90, 21, 0xBBC);
                AddLabelCropped(640, 22 * i + 31, 90, 21, textHue, parentName);

                // display the selection button
                AddButton(730, 22 * i + 32, entry.Selected ? 0xD3 : 0xD2, entry.Selected ? 0xD2 : 0xD3, 4000 + offset);
            }

            AddButton(730, 5, allDisplayedSelected ? 0xD3 : 0xD2, allDisplayedSelected ? 0xD2 : 0xD3, 2); // Select all displayed
        }
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (from == null)
        {
            return;
        }

        SetSortSwitches(info.Switches.Length > 0 ? info.Switches[0] : -1);

        Filter ??= new AdvancedSearchFilter();

        Filter.FilterAgeDirection = info.IsSwitched(302);
        Filter.FilterAge = info.IsSwitched(303);
        Filter.FilterRange = info.IsSwitched(304);
        Filter.FilterType = info.IsSwitched(305);
        Filter.FilterName = info.IsSwitched(306);
        Filter.FilterFelucca = info.IsSwitched(308);
        Filter.FilterTrammel = info.IsSwitched(309);
        Filter.FilterMalas = info.IsSwitched(310);
        Filter.FilterIlshenar = info.IsSwitched(311);
        Filter.FilterInternalMap = info.IsSwitched(312);
        Filter.FilterNullMap = info.IsSwitched(314);
        Filter.FilterPropertyTest = info.IsSwitched(315);
        Filter.HideValidInternalMap = info.IsSwitched(316);
        Filter.FilterTokuno = info.IsSwitched(318);
        Filter.FilterRegion = info.IsSwitched(319);
        Filter.FilterTerMur = info.IsSwitched(320);

        var rangeText = info.GetTextEntry(100);
        Filter.Range = rangeText != null ? Utility.ToInt32(rangeText) : null;

        var filterText = info.GetTextEntry(101);
        Filter.Type = filterText != null ? AssemblyHandler.FindTypeByName(filterText) : null;

        Filter.Name = info.GetTextEntry(102);
        Filter.PropertyTest = info.GetTextEntry(104);

        var ageText = info.GetTextEntry(105);
        Filter.Age = ageText != null ? Utility.ToTimeSpan(ageText) : null;

        Filter.RegionName = info.GetTextEntry(106);

        CommandString = info.GetTextEntry(301);

        var buttonId = info.ButtonID;

        switch (buttonId)
        {
            case 0: // Close
                {
                    return;
                }
            case 1: // Select all toggle
                {
                    AllSelected = !AllSelected;

                    if (SearchResults?.Length > 0)
                    {
                        for (var i = 0; i < SearchResults.Length; i++)
                        {
                            SearchResults[i].Selected = AllSelected;
                        }
                    }

                    break;
                }
            case 2: // Select all displayed
                {
                    if (SearchResults?.Length > 0)
                    {
                        var allSelected = true;
                        var max = DisplayFrom + MaxEntries;
                        for (var i = DisplayFrom; i < max; i++)
                        {
                            if (i >= SearchResults.Length)
                            {
                                break;
                            }

                            var entry = SearchResults[i];
                            if (!entry.Selected)
                            {
                                allSelected = false;
                                break;
                            }
                        }

                        for (var i = DisplayFrom; i < max; i++)
                        {
                            if (i >= SearchResults.Length)
                            {
                                break;
                            }

                            var entry = SearchResults[i];
                            entry.Selected = !allSelected;
                        }
                    }

                    break;
                }
            case 3: // Search
                {
                    DoSearch(from);
                    return; // Don't resend the gump, it's sent async
                }
            case 154: // Bring
                {
                    if (SearchResults?.Length > 0)
                    {
                        var count = 0;
                        for (var i = 0; i < SearchResults.Length; i++)
                        {
                            if (SearchResults[i].Selected)
                            {
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            from.SendGump(new AdvancedSearchConfirmBringGump(this, count));
                        }
                    }

                    return;
                }
            case 156: // Delete
                {
                    if (SearchResults?.Length > 0)
                    {
                        var count = 0;
                        for (var i = 0; i < SearchResults.Length; i++)
                        {
                            if (SearchResults[i].Selected)
                            {
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            from.SendGump(new AdvancedSearchConfirmDeleteGump(this, count));
                        }
                    }

                    return;
                }
            case 157: // Reset
                {
                    if (SearchResults?.Length > 0)
                    {
                        for (var i = 0; i < SearchResults.Length; i++)
                        {
                            var entry = SearchResults[i];

                            if (entry.Selected)
                            {
                                (entry.Entity as Spawner)?.Reset();
                            }
                        }
                    }

                    break;
                }
            case 158: // Respawn
                {
                    if (SearchResults?.Length > 0)
                    {
                        for (var i = 0; i < SearchResults.Length; i++)
                        {
                            var entry = SearchResults[i];

                            if (entry.Selected && entry.Entity is ISpawner { Running: true } spawner)
                            {
                                spawner.Respawn();
                            }
                        }
                    }

                    break;
                }
            case 160: // Command
                {
                    ExecuteCommand(from, CommandString);
                    break;
                }
            case 201: // Forward
                {
                    if (SearchResults?.Length > 0)
                    {
                        var maxEntryIndex = Math.Min(DisplayFrom + MaxEntries, SearchResults.Length - 1);

                        if (maxEntryIndex < SearchResults.Length - 1)
                        {
                            DisplayFrom = maxEntryIndex;
                        }
                    }

                    break;
                }
            case 202: // Backward
                {
                    if (SearchResults?.Length > 0)
                    {
                        DisplayFrom = Math.Clamp(DisplayFrom - MaxEntries, 0, SearchResults.Length - 1);
                    }

                    break;
                }
            case 700: // Sort
                {
                    Sort(from);
                    break;
                }
            case 701: // Change sort order
                {
                    SortDescending = !SortDescending;

                    var radioSwitch = info.Switches.Length > 0 ? info.Switches[0] : -1;
                    if (radioSwitch is >= 0 and <= 4)
                    {
                        Sort(from);
                    }

                    break;
                }
            case >= 1000 and <= 1999: // Goto
                {
                    var index = buttonId - 1000 + DisplayFrom;

                    if (!(SearchResults?.Length > 0) || index >= SearchResults.Length)
                    {
                        break;
                    }

                    var entry = SearchResults[index];
                    var loc = entry.GetLocation();
                    var map = entry.GetMap();

                    if (map == null || map == Map.Internal)
                    {
                        break;
                    }

                    from.MoveToWorld(loc, map);
                    break;
                }
            case <= 2999: // Open Spawner
                {
                    var index = buttonId - 2000 + DisplayFrom;

                    if (!(SearchResults?.Length > 0) || index >= SearchResults.Length)
                    {
                        break;
                    }

                    var entry = SearchResults[index];
                    if (entry.Entity?.Deleted != false)
                    {
                        break;
                    }

                    if (entry.Entity.Map == null || entry.Entity.Map == Map.Internal)
                    {
                        break;
                    }

                    Resend(from);
                    // Open the spawner
                    (entry.Entity as BaseSpawner)?.OnDoubleClick(from);

                    return;
                }
            case <= 3999: // Props
                {
                    var index = buttonId - 3000 + DisplayFrom;

                    if (!(SearchResults?.Length > 0) || index >= SearchResults.Length)
                    {
                        break;
                    }

                    var entry = SearchResults[index];
                    var entity = entry.Entity;

                    if (entity?.Deleted != false)
                    {
                        break;
                    }

                    if (!BaseCommand.IsAccessible(from, entity))
                    {
                        from.SendLocalizedMessage(500447); // That is not accessible.
                        break;
                    }

                    Resend(from);
                    from.SendGump(new PropertiesGump(from, entry.Entity));

                    return;
                }
            case <= 4999: // Select
                {
                    var index = buttonId - 4000 + DisplayFrom;

                    if (!(SearchResults?.Length > 0) || index >= SearchResults.Length)
                    {
                        break;
                    }

                    var entry = SearchResults[index];
                    entry.Selected = !entry.Selected;

                    break;
                }
        }

        Resend(from);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PushToWorkers(IEntity e)
    {
        _threadWorkers[_threadId++].Push(e);
        if (_threadId == _threadWorkers.Length)
        {
            _threadId = 0;
        }
    }

    private void DoSearch(Mobile from)
    {
        if (!World.Running)
        {
            from.SendMessage("You cannot search while the world is saving.");
            return;
        }

        var autoSave = AutoSave.SavesEnabled;
        if (autoSave)
        {
            AutoSave.SavesEnabled = false;
        }

        _threadWorkers ??= new AdvancedSearchThreadWorker[Math.Max(Environment.ProcessorCount - 1, 1)];

        var ignoreQueue = new ConcurrentQueue<IEntity>();
        var results = new ConcurrentQueue<AdvancedSearchResult>();
        var worldLocation = new WorldLocation(from.Location, from.Map);

        for (var i = 0; i < _threadWorkers.Length; i++)
        {
            (_threadWorkers[i] ??= new AdvancedSearchThreadWorker()).Wake(worldLocation, Filter, results, ignoreQueue);
        }

        var type = Filter.FilterType ? Filter.Type : null;

        // Push the entities
        foreach (var item in World.Items.Values)
        {
            if (type == null || type.IsInstanceOfType(item))
            {
                PushToWorkers(item);
            }
        }

        foreach (var m in World.Mobiles.Values)
        {
            if (type == null || type.IsInstanceOfType(m))
            {
                PushToWorkers(m);
            }
        }

        ThreadPool.QueueUserWorkItem(state =>
        {
            // Block until everything is processed
            for (var i = 0; i < _threadWorkers.Length; i++)
            {
                _threadWorkers[i].Sleep();
            }

            var ignoredEntities = new HashSet<IEntity>(ignoreQueue);

            // Force the GC to collect the ignored entities
            ignoreQueue.Clear();

            var resultsList = new List<AdvancedSearchResult>(results.Count);
            foreach (var result in results)
            {
                if (!ignoredEntities.Contains(result.Entity))
                {
                    resultsList.Add(result);
                }
            }

            SearchResults = resultsList.ToArray();

            // Force the GC to collect the results
            resultsList.Clear();
            resultsList.TrimExcess();

            // Send the gump on the main thread
            Core.LoopContext.Post(
                autoSaveState =>
                {
                    AutoSave.SavesEnabled = (bool)autoSaveState!;
                    Resend(from);
                }, state);
        }, autoSave);
    }

    private void SetSortSwitches(int radioSwitch)
    {
        SortByType = radioSwitch == 0;
        SortByName = radioSwitch == 1;
        SortByRange = radioSwitch == 2;
        SortByMap = radioSwitch == 3;
        SortBySelected = radioSwitch == 4;
    }

    public void Sort(Mobile from)
    {
        if (SearchResults == null || SearchResults.Length == 0)
        {
            return;
        }

        IComparer<AdvancedSearchResult> comparer;
        if (SortByType)
        {
            comparer = SortDescending
                ? AdvancedSearchResultTypeComparer.InstanceReverse
                : AdvancedSearchResultTypeComparer.Instance;
        }
        else if (SortByName)
        {
            comparer = SortDescending
                ? AdvancedSearchResultNameComparer.InstanceReverse
                : AdvancedSearchResultNameComparer.Instance;
        }
        else if (SortByRange)
        {
            comparer = new AdvancedSearchRangeComparer(from, SortDescending);
        }
        else if (SortByMap)
        {
            comparer = SortDescending
                ? AdvancedSearchResultMapComparer.InstanceReverse
                : AdvancedSearchResultMapComparer.Instance;
        }
        else if (SortBySelected)
        {
            comparer = SortDescending
                ? AdvancedSearchResultSelectedComparer.InstanceReverse
                : AdvancedSearchResultSelectedComparer.Instance;
        }
        else
        {
            return;
        }

        Array.Sort(SearchResults, comparer);
    }

    private void ExecuteCommand(Mobile from, string commandString)
    {
        if (string.IsNullOrWhiteSpace(commandString))
        {
            return;
        }

        var list = new List<object>();

        if (SearchResults?.Length > 0)
        {
            for (var i = 0; i < SearchResults.Length; i++)
            {
                var entry = SearchResults[i];

                if (entry.Selected && entry.Entity != null)
                {
                    list.Add(entry.Entity);
                }
            }
        }

        if (list.Count == 0)
        {
            return;
        }

        var commandSpan = commandString.AsSpan().Trim();

        string command = null;
        string[] commandArgs = new string[commandSpan.Count(" ")];

        var index = 0;
        foreach (var part in commandSpan.Tokenize(' '))
        {
            if (index == 0)
            {
                command = part.ToString();
            }
            else
            {
                commandArgs[index - 1] = part.ToString();
            }

            index++;
        }

        BaseCommand cmd = null;

        foreach (var c in TargetCommands.AllCommands)
        {
            for (var i = 0; i < c.Commands.Length; i++)
            {
                if (command.InsensitiveEquals(c.Commands[i]))
                {
                    cmd = c;
                    break;
                }
            }
        }

        if (cmd == null)
        {
            from.SendMessage($"Invalid command: {commandSpan}");
            return;
        }

        CommandEventArgs cmdEventArgs = new CommandEventArgs(from, command, commandString, commandArgs);

        bool flushToLog = false;

        // execute the command on the objects in the list

        if (list.Count > 20)
        {
            CommandLogging.Enabled = false;
        }

        cmd.ExecuteList(cmdEventArgs, list);

        if (list.Count > 20)
        {
            flushToLog = true;
            CommandLogging.Enabled = true;
        }

        cmd.Flush(from, flushToLog);
    }

    public void Resend(Mobile m)
    {
        Reset();
        Build();
        m.SendGump(this);
    }
}
