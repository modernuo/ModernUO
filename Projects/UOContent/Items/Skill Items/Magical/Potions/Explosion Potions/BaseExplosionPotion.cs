using System;
using System.Collections.Generic;
using System.Linq;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseExplosionPotion : BasePotion
    {
        private const int ExplosionRange = 2; // How long is the blast radius?

        private static readonly bool LeveledExplosion = false; // Should explosion potions explode other nearby potions?
        private static readonly bool InstantExplosion = false; // Should explosion potions explode on impact?
        private static readonly bool RelativeLocation = false; // Is the explosion target location relative for mobiles?

        private TimerExecutionToken _timerToken;

        public BaseExplosionPotion(PotionEffect effect) : base(0xF0D, effect)
        {
        }

        public BaseExplosionPotion(Serial serial) : base(serial)
        {
        }

        public abstract int MinDamage { get; }
        public abstract int MaxDamage { get; }

        public override bool RequireFreeHand => false;

        public List<Mobile> Users { get; private set; }

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

        public virtual IEntity FindParent(Mobile from)
        {
            if (HeldBy?.Holding == this)
            {
                return HeldBy;
            }

            if (RootParent != null)
            {
                return RootParent;
            }

            if (Map == Map.Internal)
            {
                return from;
            }

            return this;
        }

        public override void Drink(Mobile from)
        {
            if (Core.AOS && (from.Paralyzed || from.Frozen || from.Spell?.IsCasting == true))
            {
                from.SendLocalizedMessage(1062725); // You can not use a purple potion while paralyzed.
                return;
            }

            var targ = from.Target as ThrowTarget;
            Stackable = false; // Scavenged explosion potions won't stack with those ones in backpack, and still will explode.

            if (targ?.Potion == this)
            {
                return;
            }

            from.RevealingAction();

            Users ??= new List<Mobile>();

            if (!Users.Contains(from))
            {
                Users.Add(from);
            }

            from.Target = new ThrowTarget(this);

            if (!_timerToken.Running)
            {
                from.SendLocalizedMessage(500236); // You should throw it now!

                var timer = 3;

                if (Core.ML)
                {
                    // 3.6 seconds explosion delay
                    Timer.StartTimer(
                        TimeSpan.FromSeconds(1.0),
                        TimeSpan.FromSeconds(1.25),
                        5, // TODO: Should this be 4?
                        () => Detonate_OnTick(from, timer--),
                        out _timerToken
                    );
                }
                else
                {
                    // 2.6 seconds explosion delay
                    Timer.StartTimer(
                        TimeSpan.FromSeconds(0.75),
                        TimeSpan.FromSeconds(1.0),
                        4,
                        () => Detonate_OnTick(from, timer--),
                        out _timerToken
                    );
                }
            }
        }

        private void Detonate_OnTick(Mobile from, int timer)
        {
            if (Deleted)
            {
                return;
            }

            var parent = FindParent(from);

            if (timer == 0)
            {
                Point3D loc;
                Map map;

                if (parent is Item item)
                {
                    loc = item.GetWorldLocation();
                    map = item.Map;
                }
                else if (parent is Mobile m)
                {
                    loc = m.Location;
                    map = m.Map;
                }
                else
                {
                    return;
                }

                Explode(from, true, loc, map);
                _timerToken.Cancel();
            }
            else
            {
                if (parent is Item item)
                {
                    item.PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                }
                else if (parent is Mobile mobile)
                {
                    mobile.PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                }
            }
        }

        private void Reposition_OnTick(Mobile from, Point3D loc, Map map)
        {
            if (Deleted)
            {
                return;
            }

            if (InstantExplosion)
            {
                Explode(from, true, loc, map);
            }
            else
            {
                MoveToWorld(loc, map);
            }
        }

        public void Explode(Mobile from, bool direct, Point3D loc, Map map)
        {
            if (Deleted)
            {
                return;
            }

            Consume();

            for (var i = 0; i < Users?.Count; ++i)
            {
                var m = Users[i];

                if (m.Target is ThrowTarget targ && targ.Potion == this)
                {
                    Target.Cancel(m);
                }
            }

            if (map == null)
            {
                return;
            }

            Effects.PlaySound(loc, map, 0x307);

            Effects.SendLocationEffect(loc, map, 0x36B0, 9);
            var alchemyBonus = 0;

            if (direct)
            {
                alchemyBonus = (int)(from.Skills.Alchemy.Value / (Core.AOS ? 5 : 10));
            }

            var eable = map.GetObjectsInRange(loc, ExplosionRange, LeveledExplosion);
            var toDamage = 0;

            var toExplode = eable.Where(
                    o =>
                    {
                        if (!(o is Mobile mobile) || from != null &&
                            (!SpellHelper.ValidIndirectTarget(from, mobile) || !from.CanBeHarmful(mobile, false)))
                        {
                            return o is BaseExplosionPotion && o != this;
                        }

                        ++toDamage;
                        return true;
                    }
                )
                .ToList();

            eable.Free();

            var min = Scale(from, MinDamage);
            var max = Scale(from, MaxDamage);

            for (var i = 0; i < toExplode.Count; ++i)
            {
                var o = toExplode[i];

                if (o is Mobile m)
                {
                    from?.DoHarmful(m);

                    var damage = Utility.RandomMinMax(min, max);

                    damage += alchemyBonus;

                    if (!Core.AOS && damage > 40)
                    {
                        damage = 40;
                    }
                    else if (Core.AOS && toDamage > 2)
                    {
                        damage /= toDamage - 1;
                    }

                    AOS.Damage(m, from, damage, 0, 100, 0, 0, 0);
                }
                else if (o is BaseExplosionPotion pot)
                {
                    pot.Explode(from, false, pot.GetWorldLocation(), pot.Map);
                }
            }
        }

        private class ThrowTarget : Target
        {
            public ThrowTarget(BaseExplosionPotion potion) : base(12, true, TargetFlags.None) => Potion = potion;

            public BaseExplosionPotion Potion { get; }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (Potion.Deleted || Potion.Map == Map.Internal)
                {
                    return;
                }

                if (targeted is not IPoint3D p)
                {
                    return;
                }

                var map = from.Map;

                if (map == null)
                {
                    return;
                }

                SpellHelper.GetSurfaceTop(ref p);
                var loc = new Point3D(p);

                from.RevealingAction();

                IEntity to = new Entity(Serial.Zero, loc, map);

                if (p is Mobile m)
                {
                    if (!RelativeLocation) // explosion location = current mob location.
                    {
                        loc = m.Location;
                    }
                    else
                    {
                        to = m;
                    }
                }

                Effects.SendMovingEffect(from, to, Potion.ItemID, 7, 0, false, false, Potion.Hue);

                if (Potion.Amount > 1)
                {
                    Mobile.LiftItemDupe(Potion, 1);
                }

                Potion.Internalize();
                Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Potion.Reposition_OnTick(from, loc, map));
            }
        }
    }
}
