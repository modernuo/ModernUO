/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LLChampionSpawn.cs                                              *
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
    public class LLChampionSpawn : ChampionSpawn
    {
        public override bool HasStarRoomGate => false;

        [Constructible]
        public LLChampionSpawn()
        {
            CannedEvilTimer.AddSpawn(this);
        }

        public LLChampionSpawn(Serial serial) : base(serial)
        {
        }

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
            CannedEvilTimer.AddSpawn(this);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
            CannedEvilTimer.RemoveSpawn(this);
        }
    }
}
