/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSkullPlatform.cs                                        *
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
using Server.Mobiles;

namespace Server.Engines.CannedEvil
{
    public class ChampionSkullPlatform : BaseAddon
    {
        private ChampionSkullBrazier m_Power, m_Enlightenment, m_Venom, m_Pain, m_Greed, m_Death;

        [Constructible]
        public ChampionSkullPlatform()
        {
            AddComponent(new AddonComponent(0x71A), -1, -1, -1);
            AddComponent(new AddonComponent(0x709), 0, -1, -1);
            AddComponent(new AddonComponent(0x709), 1, -1, -1);
            AddComponent(new AddonComponent(0x709), -1, 0, -1);
            AddComponent(new AddonComponent(0x709), 0, 0, -1);
            AddComponent(new AddonComponent(0x709), 1, 0, -1);
            AddComponent(new AddonComponent(0x709), -1, 1, -1);
            AddComponent(new AddonComponent(0x709), 0, 1, -1);
            AddComponent(new AddonComponent(0x71B), 1, 1, -1);

            AddComponent(new AddonComponent(0x50F), 0, -1, 4);
            AddComponent(m_Power = new ChampionSkullBrazier(this, ChampionSkullType.Power), 0, -1, 5);

            AddComponent(new AddonComponent(0x50F), 1, -1, 4);
            AddComponent(m_Enlightenment = new ChampionSkullBrazier(this, ChampionSkullType.Enlightenment), 1, -1, 5);

            AddComponent(new AddonComponent(0x50F), -1, 0, 4);
            AddComponent(m_Venom = new ChampionSkullBrazier(this, ChampionSkullType.Venom), -1, 0, 5);

            AddComponent(new AddonComponent(0x50F), 1, 0, 4);
            AddComponent(m_Pain = new ChampionSkullBrazier(this, ChampionSkullType.Pain), 1, 0, 5);

            AddComponent(new AddonComponent(0x50F), -1, 1, 4);
            AddComponent(m_Greed = new ChampionSkullBrazier(this, ChampionSkullType.Greed), -1, 1, 5);

            AddComponent(new AddonComponent(0x50F), 0, 1, 4);
            AddComponent(m_Death = new ChampionSkullBrazier(this, ChampionSkullType.Death), 0, 1, 5);

            AddonComponent comp = new LocalizedAddonComponent(0x20D2, 1049495) { Hue = 0x482 };
            AddComponent(comp, 0, 0, 5);

            comp = new LocalizedAddonComponent(0x0BCF, 1049496) { Hue = 0x482 };
            AddComponent(comp, 0, 2, -7);

            comp = new LocalizedAddonComponent(0x0BD0, 1049497) { Hue = 0x482 };
            AddComponent(comp, 2, 0, -7);
        }

        public void Validate()
        {
            if (Validate(m_Power) && Validate(m_Enlightenment) && Validate(m_Venom) && Validate(m_Pain) &&
                Validate(m_Greed) && Validate(m_Death))
            {
                Mobile harrower = Harrower.Spawn(new Point3D(X, Y, Z + 6), Map);

                if (harrower == null)
                {
                    return;
                }

                Clear(m_Power);
                Clear(m_Enlightenment);
                Clear(m_Venom);
                Clear(m_Pain);
                Clear(m_Greed);
                Clear(m_Death);
            }
        }

        public void Clear(ChampionSkullBrazier brazier)
        {
            if (brazier != null)
            {
                Effects.SendBoltEffect(brazier);

                brazier.Skull?.Delete();
            }
        }

        public bool Validate(ChampionSkullBrazier brazier) => brazier is { Skull: { Deleted: false } };

        public ChampionSkullPlatform(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Power);
            writer.Write(m_Enlightenment);
            writer.Write(m_Venom);
            writer.Write(m_Pain);
            writer.Write(m_Greed);
            writer.Write(m_Death);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Power = reader.ReadEntity<ChampionSkullBrazier>();
                        m_Enlightenment = reader.ReadEntity<ChampionSkullBrazier>();
                        m_Venom = reader.ReadEntity<ChampionSkullBrazier>();
                        m_Pain = reader.ReadEntity<ChampionSkullBrazier>();
                        m_Greed = reader.ReadEntity<ChampionSkullBrazier>();
                        m_Death = reader.ReadEntity<ChampionSkullBrazier>();

                        break;
                    }
            }
        }
    }
}
