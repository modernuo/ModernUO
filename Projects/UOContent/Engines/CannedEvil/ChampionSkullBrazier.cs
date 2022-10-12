/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ChampionSkullBrazier.cs                                         *
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
using Server.Targeting;
using Server.Mobiles;

namespace Server.Engines.CannedEvil
{
    public class ChampionSkullBrazier : AddonComponent
    {
        private ChampionSkullType m_Type;
        private Item m_Skull;

        [CommandProperty(AccessLevel.GameMaster)]
        public ChampionSkullPlatform Platform { get; private set; }

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

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Skull
        {
            get => m_Skull;
            set
            {
                m_Skull = value;
                Platform?.Validate();
            }
        }

        public override int LabelNumber => 1049489 + (int)m_Type;

        public ChampionSkullBrazier(ChampionSkullPlatform platform, ChampionSkullType type) : base(0x19BB)
        {
            Hue = 0x455;
            Light = LightType.Circle300;

            Platform = platform;
            m_Type = type;
        }

        public ChampionSkullBrazier(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            Platform?.Validate();

            BeginSacrifice(from);
        }

        public void BeginSacrifice(Mobile from)
        {
            if (Deleted)
            {
                return;
            }

            if (m_Skull is { Deleted: true })
            {
                Skull = null;
            }

            if (from.Map != Map || !from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (!Harrower.CanSpawn)
            {
                from.SendMessage("The harrower has already been spawned.");
            }
            else if (m_Skull == null)
            {
                from.SendLocalizedMessage(1049485); // What would you like to sacrifice?
                from.Target = new SacrificeTarget(this);
            }
            else
            {
                SendLocalizedMessageTo(from, 1049487); // I already have my champions awakening skull!
            }
        }

        public void EndSacrifice(Mobile from, ChampionSkull skull)
        {
            if (Deleted)
            {
                return;
            }

            if (m_Skull is { Deleted: true })
            {
                Skull = null;
            }

            if (from.Map != Map || !from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (!Harrower.CanSpawn)
            {
                from.SendMessage("The harrower has already been spawned.");
            }
            else if (skull == null)
            {
                SendLocalizedMessageTo(from, 1049488); // That is not my champions awakening skull!
            }
            else if (m_Skull != null)
            {
                SendLocalizedMessageTo(from, 1049487); // I already have my champions awakening skull!
            }
            else if (!skull.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1049486); // You can only sacrifice items that are in your backpack!
            }
            else
            {
                if (skull.Type == Type)
                {
                    skull.Movable = false;
                    skull.MoveToWorld(GetWorldTop(), Map);

                    Skull = skull;
                }
                else
                {
                    SendLocalizedMessageTo(from, 1049488); // That is not my champions awakening skull!
                }
            }
        }

        private class SacrificeTarget : Target
        {
            private readonly ChampionSkullBrazier m_Brazier;

            public SacrificeTarget(ChampionSkullBrazier brazier) : base(12, false, TargetFlags.None) =>
                m_Brazier = brazier;

            protected override void OnTarget(Mobile from, object targeted) =>
                m_Brazier.EndSacrifice(from, targeted as ChampionSkull);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_Type);
            writer.Write(Platform);
            writer.Write(m_Skull);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (ChampionSkullType)reader.ReadInt();
                        Platform = reader.ReadEntity<ChampionSkullPlatform>();
                        m_Skull = reader.ReadEntity<Item>();

                        if (Platform == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }

            if (Hue == 0x497)
            {
                Hue = 0x455;
            }

            if (Light != LightType.Circle300)
            {
                Light = LightType.Circle300;
            }
        }
    }
}
