/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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

using Server.Engines.CannedEvil;

namespace Server.Items
{
    public class ChampionSkull : Item
    {
        private ChampionSkullType m_Type;

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSkullType Type
        {
            get => m_Type;
            set
            {
                m_Type = value;
                InvalidateProperties();
            }
        }

        public override int LabelNumber => 1049479 + (int)m_Type;

        [Constructible]
        public ChampionSkull(ChampionSkullType type) : base(0x1AE1)
        {
            m_Type = type;
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

        public ChampionSkull(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)m_Type);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Type = (ChampionSkullType)reader.ReadInt();
                        break;
                    }
            }

            if (version == 0)
            {
                if (LootType != LootType.Cursed)
                {
                    LootType = LootType.Cursed;
                }

                if (Insured)
                {
                    Insured = false;
                }
            }
        }
    }
}
