using System;
using Server.Spells;

namespace Server.Items
{
    public class FlameSpurtTrap : BaseTrap
    {
        private Item m_Spurt;
        private TimerExecutionToken _timerToken;

        [Constructible]
        public FlameSpurtTrap() : base(0x1B71) => Visible = false;

        public FlameSpurtTrap(Serial serial) : base(serial)
        {
        }

        public virtual void StartTimer()
        {
            if (!_timerToken.Running)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), Refresh, out _timerToken);
            }
        }

        public virtual void StopTimer()
        {
            _timerToken.Cancel();
        }

        public virtual void CheckTimer()
        {
            var map = Map;

            if (map?.GetSector(GetWorldLocation()).Active == true)
            {
                StartTimer();
            }
            else
            {
                StopTimer();
            }
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            CheckTimer();
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            CheckTimer();
        }

        public override void OnSectorActivate()
        {
            base.OnSectorActivate();

            StartTimer();
        }

        public override void OnSectorDeactivate()
        {
            base.OnSectorDeactivate();

            StopTimer();
        }

        public override void OnDelete()
        {
            base.OnDelete();

            m_Spurt?.Delete();
        }

        public virtual void Refresh()
        {
            if (Deleted)
            {
                return;
            }

            foreach (var mob in GetMobilesInRange(3))
            {
                if (mob.Player && mob.Alive && mob.AccessLevel <= AccessLevel.Player && Z + 8 >= mob.Z && mob.Z + 16 > Z)
                {
                    if (m_Spurt?.Deleted != false)
                    {
                        m_Spurt = new Static(0x3709);
                        m_Spurt.MoveToWorld(Location, Map);

                        Effects.PlaySound(GetWorldLocation(), Map, 0x309);
                    }

                    return;
                }
            }

            m_Spurt?.Delete();
            m_Spurt = null;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
            {
                return true;
            }

            if (!(m.Player && m.Alive))
            {
                return false;
            }

            CheckTimer();

            SpellHelper.Damage(TimeSpan.FromTicks(1), m, m, Utility.RandomMinMax(1, 30));
            m.PlaySound(m.Female ? 0x327 : 0x437);

            return false;
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m.Location == oldLocation || !m.Player || !m.Alive || m.AccessLevel > AccessLevel.Player
                || !CheckRange(m.Location, oldLocation, 1))
            {
                return;
            }

            CheckTimer();

            SpellHelper.Damage(TimeSpan.FromTicks(1), m, m, Utility.RandomMinMax(1, 10));
            m.PlaySound(m.Female ? 0x327 : 0x437);

            if (m.Body.IsHuman)
            {
                m.Animate(20, 1, 1, true, false, 0);
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Spurt);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        var item = reader.ReadEntity<Item>();

                        item?.Delete();

                        CheckTimer();

                        break;
                    }
            }
        }
    }
}
