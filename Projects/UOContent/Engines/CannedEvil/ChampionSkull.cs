/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSkull.cs                                                *
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
using Server.Engines.CannedEvil;

namespace Server.Items;

[SerializationGenerator(2, false)]
public partial class ChampionSkull : Item
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ChampionSkullType _type;

    public override int LabelNumber => 1049479 + (int)_type;

    [Constructible]
    public ChampionSkull(ChampionSkullType type) : base(0x1AE1)
    {
        _type = type;
        LootType = LootType.Cursed;

        // TODO: All hue values
        Hue = type switch
        {
            ChampionSkullType.Power => 0x159,
            ChampionSkullType.Venom => 0x172,
            ChampionSkullType.Greed => 0x1EE,
            ChampionSkullType.Death => 0x025,
            ChampionSkullType.Pain  => 0x035,
            _                       => Hue
        };
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _type = (ChampionSkullType)reader.ReadInt();
    }
}
