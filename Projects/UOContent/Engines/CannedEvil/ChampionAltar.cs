/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionAltar.cs                                                *
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
public partial class ChampionAltar : PentagramAddon
{
    [SerializableField(0, setter: "private")]
    private ChampionSpawn _spawn;

    public ChampionAltar(ChampionSpawn spawn) => _spawn = spawn;

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
