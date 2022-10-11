/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DungeonChampionSpawn.cs                                         *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Engines.CannedEvil
{
    public class DungeonChampionSpawn : ChampionSpawn
    {
        [Constructible]
        public DungeonChampionSpawn()
        {
            CannedEvilTimer.AddSpawn(this);
        }

        public DungeonChampionSpawn(Serial serial) : base(serial)
        {
            CannedEvilTimer.AddSpawn(this);
        }

        public override bool ProximitySpawn => true;
        public override bool AlwaysActive => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); //version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            CannedEvilTimer.RemoveSpawn(this);
        }
    }
}
