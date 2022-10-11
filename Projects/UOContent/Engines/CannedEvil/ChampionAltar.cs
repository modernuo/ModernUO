/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

using Server.Items;

namespace Server.Engines.CannedEvil
{
    public class ChampionAltar : PentagramAddon
    {
        public ChampionSpawn Spawn { get; private set; }

        public ChampionAltar(ChampionSpawn spawn) => Spawn = spawn;

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Spawn?.Delete();
        }

        public ChampionAltar(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Spawn);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        Spawn = reader.ReadEntity<ChampionSpawn>();

                        if (Spawn == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }
        }
    }
}
