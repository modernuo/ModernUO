/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionPlatform.cs                                             *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.CannedEvil;

[SerializationGenerator(0, false)]
public partial class ChampionPlatform : BaseAddon
{
    [SerializableField(0, setter: "private")]
    private ChampionSpawn _spawn;

    public ChampionPlatform(ChampionSpawn spawn)
    {
        _spawn = spawn;

        for (var x = -2; x <= 2; ++x)
        {
            for (var y = -2; y <= 2; ++y)
            {
                AddComponent(0x750, x, y, -5);
            }
        }

        for (var x = -1; x <= 1; ++x)
        {
            for (var y = -1; y <= 1; ++y)
            {
                AddComponent(0x750, x, y, 0);
            }
        }

        for (var i = -1; i <= 1; ++i)
        {
            AddComponent(0x751, i, 2, 0);
            AddComponent(0x752, 2, i, 0);

            AddComponent(0x753, i, -2, 0);
            AddComponent(0x754, -2, i, 0);
        }

        AddComponent(0x759, -2, -2, 0);
        AddComponent(0x75A, 2, 2, 0);
        AddComponent(0x75B, -2, 2, 0);
        AddComponent(0x75C, 2, -2, 0);
    }

    public void AddComponent(int id, int x, int y, int z)
    {
        AddonComponent ac = new AddonComponent(id) { Hue = 0x497 };
        AddComponent(ac, x, y, z);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        Spawn?.Delete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (Spawn == null)
        {
            Delete();
        }
    }
}
