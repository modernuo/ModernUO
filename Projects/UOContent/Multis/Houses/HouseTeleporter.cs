using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class HouseTeleporter : Item, ISecurable
    {
        public HouseTeleporter(int itemID, Item target = null) : base(itemID)
        {
            Movable = false;

            Level = SecureLevel.Anyone;

            Target = target;
        }

        public HouseTeleporter(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Target { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public bool CheckAccess(Mobile m)
        {
            var house = BaseHouse.FindHouseAt(this);

            return (house == null || house.Public && !house.IsBanned(m) || house.HasAccess(m)) &&
                   house?.HasSecureAccess(m, Level) == true;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Target?.Deleted == false)
            {
                if (CheckAccess(m))
                {
                    if (!m.Hidden || m.AccessLevel == AccessLevel.Player)
                    {
                        new EffectTimer(Location, Map, 2023, 0x1F0, TimeSpan.FromSeconds(0.4)).Start();
                    }

                    new DelayTimer(this, m).Start();
                }
                else
                {
                    m.SendLocalizedMessage(1061637); // You are not allowed to access this.
                }
            }

            return true;
        }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write((int)Level);

            writer.Write(Target);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        Target = reader.ReadEntity<Item>();

                        if (version < 1)
                        {
                            Level = SecureLevel.Anyone;
                        }

                        break;
                    }
            }
        }

        private class EffectTimer : Timer
        {
            private readonly int m_EffectID;
            private readonly Point3D m_Location;
            private readonly Map m_Map;
            private readonly int m_SoundID;

            public EffectTimer(Point3D p, Map map, int effectID, int soundID, TimeSpan delay) : base(delay)
            {
                m_Location = p;
                m_Map = map;
                m_EffectID = effectID;
                m_SoundID = soundID;
            }

            protected override void OnTick()
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(m_Location, m_Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    m_EffectID,
                    0
                );

                if (m_SoundID != -1)
                {
                    Effects.PlaySound(m_Location, m_Map, m_SoundID);
                }
            }
        }

        private class DelayTimer : Timer
        {
            private readonly Mobile m_Mobile;
            private readonly HouseTeleporter m_Teleporter;

            public DelayTimer(HouseTeleporter tp, Mobile m) : base(TimeSpan.FromSeconds(1.0))
            {
                m_Teleporter = tp;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                var target = m_Teleporter.Target;

                if (target?.Deleted != false)
                {
                    return;
                }

                if (m_Mobile.Location != m_Teleporter.Location || m_Mobile.Map != m_Teleporter.Map)
                {
                    return;
                }

                var p = target.GetWorldTop();
                var map = target.Map;

                BaseCreature.TeleportPets(m_Mobile, p, map);

                m_Mobile.MoveToWorld(p, map);

                if (m_Mobile.Hidden && m_Mobile.AccessLevel != AccessLevel.Player)
                {
                    return;
                }

                Effects.PlaySound(target.Location, target.Map, 0x1FE);

                Effects.SendLocationParticles(
                    EffectItem.Create(m_Teleporter.Location, m_Teleporter.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    2023,
                    0
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                    0x3728,
                    10,
                    10,
                    5023,
                    0
                );

                new EffectTimer(target.Location, target.Map, 2023, -1, TimeSpan.FromSeconds(0.4)).Start();
            }
        }
    }
}
