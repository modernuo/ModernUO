using System;
using System.Collections.Generic;
using Server.Misc;
using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseConfusionBlastPotion : BasePotion
    {
        private static readonly Dictionary<Mobile, TimerExecutionToken> m_Delay = new();
        private readonly List<Mobile> m_Users = new();

        public BaseConfusionBlastPotion(PotionEffect effect) : base(0xF06, effect) => Hue = 0x48D;

        public BaseConfusionBlastPotion(Serial serial) : base(serial)
        {
        }

        public abstract int Radius { get; }

        public override bool RequireFreeHand => false;

        public override void Drink(Mobile from)
        {
            if (Core.AOS && (from.Paralyzed || from.Frozen || from.Spell?.IsCasting == true))
            {
                from.SendLocalizedMessage(1062725); // You can not use that potion while paralyzed.
                return;
            }

            var delay = GetDelay(from);

            if (delay > 0)
            {
                from.SendLocalizedMessage(
                    1072529,
                    $"{delay}\t{(delay > 1 ? "seconds." : "second.")}"
                ); // You cannot use that for another ~1_NUM~ ~2_TIMEUNITS~
                return;
            }

            if (from.Target is ThrowTarget targ && targ.Potion == this)
            {
                return;
            }

            from.RevealingAction();

            if (!m_Users.Contains(from))
            {
                m_Users.Add(from);
            }

            from.Target = new ThrowTarget(this);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }

        public virtual void Explode(Mobile from, Point3D loc, Map map)
        {
            if (Deleted || map == null)
            {
                return;
            }

            Consume();

            // Check if any other players are using this potion
            for (var i = 0; i < m_Users.Count; i++)
            {
                if (m_Users[i].Target is ThrowTarget targ && targ.Potion == this)
                {
                    Target.Cancel(from);
                }
            }

            // Effects
            Effects.PlaySound(loc, map, 0x207);

            Geometry.Circle2D(loc, map, Radius, BlastEffect, 270, 90);

            Timer.StartTimer(TimeSpan.FromSeconds(0.3), () => CircleEffect2(loc, map));

            foreach (var mobile in map.GetMobilesInRange(loc, Radius))
            {
                if (mobile is BaseCreature mon)
                {
                    if (mon.Controlled || mon.Summoned)
                    {
                        continue;
                    }

                    mon.Pacify(from, Core.Now + TimeSpan.FromSeconds(5.0)); // TODO check
                }
            }
        }

        public virtual void BlastEffect(Point3D p, Map map)
        {
            if (map.CanFit(p, 12, true, false))
            {
                Effects.SendLocationEffect(p, map, 0x376A, 4, 9);
            }
        }

        public void CircleEffect2(Point3D p, Map m)
        {
            Geometry.Circle2D(p, m, Radius, BlastEffect, 90, 270);
        }

        public static void AddDelay(Mobile m)
        {
            m_Delay.TryGetValue(m, out var timer);
            timer.Cancel();

            Timer.StartTimer(TimeSpan.FromSeconds(60), () => EndDelay(m), out timer);
            m_Delay[m] = timer;
        }

        public static int GetDelay(Mobile m)
        {
            if (m_Delay.TryGetValue(m, out var timer) && timer.Next > Core.Now)
            {
                return (int)(timer.Next - Core.Now).TotalSeconds;
            }

            return 0;
        }

        public static void EndDelay(Mobile m)
        {
            if (m_Delay.Remove(m, out var timer))
            {
                timer.Cancel();
            }
        }

        private class ThrowTarget : Target
        {
            public ThrowTarget(BaseConfusionBlastPotion potion) : base(12, true, TargetFlags.None) => Potion = potion;

            public BaseConfusionBlastPotion Potion { get; }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (Potion.Deleted || Potion.Map == Map.Internal)
                {
                    return;
                }

                if (!(targeted is IPoint3D p) || from.Map == null)
                {
                    return;
                }

                // Add delay
                AddDelay(from);

                SpellHelper.GetSurfaceTop(ref p);
                var loc = new Point3D(p);
                var map = from.Map;

                from.RevealingAction();

                IEntity to;

                if (p is Mobile mobile)
                {
                    to = mobile;
                }
                else
                {
                    to = new Entity(Serial.Zero, loc, map);
                }

                Effects.SendMovingEffect(from, to, 0xF0D, 7, 0, false, false, Potion.Hue);
                Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Potion.Explode(from, loc, map));
            }
        }
    }
}
