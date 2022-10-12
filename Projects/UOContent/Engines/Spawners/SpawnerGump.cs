using Server.Collections;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Spawners;

public class SpawnerGump : Gump
{
    private readonly BaseSpawner _spawner;
    private SpawnerEntry _entry;
    private int _page;

    public SpawnerGump(BaseSpawner spawner, SpawnerEntry focusentry = null, int page = 0) : base(50, 50)
    {
        _spawner = spawner;
        _entry = focusentry;
        _page = page;

        AddPage(0);

        AddBackground(0, 0, 346, 400 + (_entry != null ? 44 : 0), 5054);
        AddAlphaRegion(0, 0, 346, 400 + (_entry != null ? 44 : 0));

        AddHtml(240, 1, 250, 20, "<BASEFONT COLOR=#F4F4F4>#</BASEFONT>");
        AddHtml(271, 1, 250, 20, "<BASEFONT COLOR=#F4F4F4>Max</BASEFONT>");
        AddHtml(311, 1, 250, 20, "<BASEFONT COLOR=#F4F4F4>Prb</BASEFONT>");

        // AddLabel( 95, 1, 0, "Creatures List" );

        var offset = 0;

        for (var i = 0; i < 13; i++)
        {
            var textIndex = i * 5;
            var entryIndex = _page * 13 + i;

            SpawnerEntry entry = null;

            if (entryIndex < spawner.Entries.Count)
            {
                entry = _spawner.Entries[entryIndex];
            }

            if (entry == null || _entry != entry)
            {
                AddButton(
                    5,
                    22 * i + 21 + offset,
                    entry != null ? 0xFBA : 0xFA5,
                    entry != null ? 0xFBC : 0xFA7,
                    GetButtonID(2, i * 2)
                ); // Expand
            }
            else
            {
                AddButton(
                    5,
                    22 * i + 21 + offset,
                    0xFBB,
                    0xFBC,
                    GetButtonID(2, i * 2)
                ); // Unexpand
            }

            AddButton(38, 22 * i + 21 + offset, 0xFA2, 0xFA4, GetButtonID(2, 1 + i * 2)); // Delete

            AddImageTiled(71, 22 * i + 20 + offset, 161, 23, 0xA40); // creature text box
            AddImageTiled(72, 22 * i + 21 + offset, 159, 21, 0xBBC); // creature text box

            AddImageTiled(235, 22 * i + 20 + offset, 35, 23, 0xA40); // count html label
            AddImageTiled(236, 22 * i + 21 + offset, 33, 21, 0xE14); // count html label

            AddImageTiled(267, 22 * i + 20 + offset, 35, 23, 0xA40); // maxcount text box
            AddImageTiled(268, 22 * i + 21 + offset, 33, 21, 0xBBC); // maxcount text box

            AddImageTiled(305, 22 * i + 20 + offset, 35, 23, 0xA40); // probability text box
            AddImageTiled(306, 22 * i + 21 + offset, 33, 21, 0xBBC); // probability text box

            string name;
            string probability;
            string maxCount;
            var flags = EntryFlags.None;

            if (entry != null)
            {
                name = entry.SpawnedName;
                probability = entry.SpawnedProbability.ToString();
                maxCount = entry.SpawnedMaxCount.ToString();
                flags = entry.Valid;

                var count = spawner.CountSpawns(entry);

                AddHtml(235, 22 * i + 20 + offset + 1, 35, 15, $"<BASEFONT COLOR={GetCountColor(count, entry.SpawnedMaxCount)}><div align=RIGHT>{count}<BASEFONT COLOR=#F4F4F4>/</BASEFONT></div></BASEFONT>");
            }
            else
            {
                name = "";
                probability = "";
                maxCount = "";
            }

            // creature
            AddTextEntry(
                75,
                22 * i + 21 + offset,
                156,
                21,
                (flags & EntryFlags.InvalidType) != 0 ? 33 : 0,
                textIndex,
                name
            );
            AddTextEntry(270, 22 * i + 21 + offset, 30, 21, 0, textIndex + 1, maxCount);    // max count
            AddTextEntry(308, 22 * i + 21 + offset, 30, 21, 0, textIndex + 2, probability); // probability

            if (entry != null && _entry == entry)
            {
                AddLabel(5, 22 * i + 42, 0x384, "Params");
                AddImageTiled(55, 22 * i + 42, 253, 23, 0xA40); // Parameters
                AddImageTiled(56, 22 * i + 43, 251, 21, 0xBBC); // Parameters

                AddLabel(5, 22 * i + 64, 0x384, "Props");
                AddImageTiled(55, 22 * i + 64, 253, 23, 0xA40); // Properties
                AddImageTiled(56, 22 * i + 65, 251, 21, 0xBBC); // Properties

                AddTextEntry(
                    59,
                    22 * i + 42,
                    248,
                    21,
                    (flags & EntryFlags.InvalidParams) != 0 ? 33 : 0,
                    textIndex + 3,
                    entry.Parameters
                ); // parameters
                AddTextEntry(
                    59,
                    22 * i + 62,
                    248,
                    21,
                    (flags & EntryFlags.InvalidProps) != 0 ? 33 : 0,
                    textIndex + 4,
                    entry.Properties
                ); // properties

                offset += 44;
            }
        }

        if (spawner.Running)
        {
            AddButton(5, 312 + offset, 0x2A4E, 0x2A3A, GetButtonID(1, 6));
            AddLabel(38, 317 + offset, 0x384, "On");
        }
        else
        {
            AddButton(5, 312 + offset, 0x2A62, 0x2A3A, GetButtonID(1, 7));
            AddLabel(38, 317 + offset, 0x384, "Off");
        }

        var totalSpawned = 0;
        var totalSpawns = 0;
        var totalWeight = 0;

        foreach (SpawnerEntry spawnerEntry in _spawner.Entries)
        {
            totalSpawns += spawnerEntry.SpawnedMaxCount;
            totalSpawned += spawner.CountSpawns(spawnerEntry);
            totalWeight += spawnerEntry.SpawnedProbability;
        }

        AddHtml(270, 308 + offset, 35, 20, $"<BASEFONT COLOR=#F4F4F4><CENTER>{totalSpawns}</CENTER></BASEFONT>");
        AddHtml(308, 308 + offset, 35, 20, $"<BASEFONT COLOR=#F4F4F4><CENTER>{totalWeight}</CENTER></BASEFONT>");

        AddHtml(5, 1, 161, 20, $"<BASEFONT COLOR=#FFEA00>{spawner.Name}</BASEFONT><BASEFONT COLOR={GetCountColor(totalSpawned, spawner.Count)}> ({totalSpawned}/{spawner.Count})</BASEFONT>");

        AddButton(5, 347 + offset, 0xFAB, 0xFAD, GetButtonID(1, 2));
        AddLabel(38, 347 + offset, 0x384, "Props");

        AddButton(5, 369 + offset, 0xFAE, 0xFAF, GetButtonID(1, 8));
        AddLabel(38, 369 + offset, 0x384, "Goto");

        AddButton(90, 325 + offset, 0xFA2, 0xFA3, GetButtonID(1, 9));
        AddLabel(123, 325 + offset, 0x384, "Reset");

        AddButton(90, 347 + offset, 0xFB4, 0xFB6, GetButtonID(1, 3));
        AddLabel(123, 347 + offset, 0x384, "Bring Home");

        AddButton(90, 369 + offset, 0xFA8, 0xFAA, GetButtonID(1, 4));
        AddLabel(123, 369 + offset, 0x384, "Total Respawn");

        AddButton(260, 347 + offset, 0xFB7, 0xFB9, GetButtonID(1, 5));
        AddLabel(293, 347 + offset, 0x384, "Save");

        AddButton(260, 369 + offset, 0xFB1, 0xFB3, 0);
        AddLabel(293, 369 + offset, 0x384, "Cancel");

        if (_page > 0)
        {
            AddButton(200, 308 + offset, 0x15E3, 0x15E7, GetButtonID(1, 0));
        }
        else
        {
            AddImage(200, 308 + offset, 0x25EA);
        }

        if ((_page + 1) * 13 <= _spawner.Entries.Count)
        {
            AddButton(217, 308 + offset, 0x15E1, 0x15E5, GetButtonID(1, 1));
        }
        else
        {
            AddImage(217, 308 + offset, 0x25E6);
        }
    }

    public int GetButtonID(int type, int index) => 1 + index * 10 + type;

    private static string GetCountColor(int count, int maxCount) =>
        ((double) count / maxCount) switch
        {
            <= 0.25 => "#DE3163", // red
            <= 0.50 => "#FF7F50", // orange
            <= 0.75 => "#DFFF00", // yellow
            <= 1    => "#00FF00", // green
            _       => "#F4F4F4"  // white
        };

    public void CreateArray(RelayInfo info, Mobile from, BaseSpawner spawner)
    {
        var ocount = spawner.Entries.Count;

        using var queue = PooledRefQueue<SpawnerEntry>.Create();

        for (var i = 0; i < 13; i++)
        {
            var index = i * 5;
            var entryindex = _page * 13 + i;

            var cte = info.GetTextEntry(index);
            var mte = info.GetTextEntry(index + 1);
            var poste = info.GetTextEntry(index + 2);
            var parmte = info.GetTextEntry(index + 3);
            var propte = info.GetTextEntry(index + 4);

            if (cte == null)
            {
                continue;
            }

            var str = cte.Text.Trim().ToLower();

            if (str.Length > 0)
            {
                var type = AssemblyHandler.FindTypeByName(str);

                if (type == null)
                {
                    from.SendMessage("{0} is not a valid type name for entry #{1}.", str, i);
                    return;
                }

                SpawnerEntry entry;

                if (entryindex < ocount)
                {
                    entry = spawner.Entries[entryindex];
                    entry.SpawnedName = str;

                    if (mte != null)
                    {
                        entry.SpawnedMaxCount = Utility.ToInt32(mte.Text.Trim());
                    }

                    if (poste != null)
                    {
                        entry.SpawnedProbability = Utility.ToInt32(poste.Text.Trim());
                    }
                }
                else
                {
                    var maxcount = 1;
                    var probcount = 100;

                    if (mte != null)
                    {
                        maxcount = Utility.ToInt32(mte.Text.Trim());
                    }

                    if (poste != null)
                    {
                        probcount = Utility.ToInt32(poste.Text.Trim());
                    }

                    entry = spawner.AddEntry(str, probcount, maxcount);
                }

                if (parmte != null)
                {
                    entry.Parameters = parmte.Text.Trim();
                }

                if (propte != null)
                {
                    entry.Properties = propte.Text.Trim();
                }
            }
            else if (entryindex < ocount && spawner.Entries[entryindex] != null)
            {
                queue.Enqueue(spawner.Entries[entryindex]);
            }
        }

        while (queue.Count > 0)
        {
            spawner.RemoveEntry(queue.Dequeue());
        }

        if (ocount == 0 && spawner.Entries.Count > 0)
        {
            spawner.Start();
        }
    }

    public override void OnResponse(NetState state, RelayInfo info)
    {
        if (_spawner.Deleted)
        {
            return;
        }

        var val = info.ButtonID - 1;

        if (val < 0)
        {
            return;
        }

        var type = val % 10;
        var index = val / 10;

        switch (type)
        {
            case 0: // Cancel
                {
                    return;
                }
            case 1:
                {
                    switch (index)
                    {
                        case 0:
                            {
                                if (_spawner.Entries != null && _page > 0)
                                {
                                    _page--;
                                    _entry = null;
                                }

                                break;
                            }
                        case 1:
                            {
                                if ((_page + 1) * 13 <= _spawner.Entries?.Count)
                                {
                                    _page++;
                                    _entry = null;
                                }

                                break;
                            }
                        case 2: // Props
                            {
                                state.Mobile.SendGump(new PropertiesGump(state.Mobile, _spawner));
                                break;
                            }
                        case 3: // Bring Home
                            {
                                _spawner.BringToHome();
                                break;
                            }
                        case 4: // Complete respawn
                            {
                                _spawner.Respawn();
                                break;
                            }
                        case 5: // Save
                            {
                                CreateArray(info, state.Mobile, _spawner);
                                break;
                            }
                        case 6: // On button
                            {
                                _spawner.Running = false;

                                break;
                            }
                        case 7: // Off button
                            {
                                _spawner.Running = true;
                                break;
                            }
                        case 8: // Goto
                            {
                                state.Mobile.MoveToWorld(_spawner.Location, _spawner.Map);
                                break;
                            }
                        case 9: // Reset
                            {
                                _spawner.Reset();
                                break;
                            }
                    }

                    break;
                }
            case 2:
                {
                    var entryIndex = index / 2 + _page * 13;
                    var buttonType = index % 2;

                    if (entryIndex >= 0 && entryIndex < _spawner.Entries.Count)
                    {
                        var entry = _spawner.Entries[entryIndex];
                        if (buttonType == 0) // Spawn creature
                        {
                            _entry = _entry != entry ? entry : null;
                        }
                        else // Remove creatures
                        {
                            _spawner.RemoveSpawn(entryIndex);
                        }
                    }

                    CreateArray(info, state.Mobile, _spawner);
                    break;
                }
        }

        if (_entry != null && _spawner.Entries?.Contains(_entry) == true)
        {
            state.Mobile.SendGump(new SpawnerGump(_spawner, _entry, _page));
        }
        else
        {
            state.Mobile.SendGump(new SpawnerGump(_spawner, null, _page));
        }
    }
}
