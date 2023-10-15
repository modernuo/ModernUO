using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseExplosionPotion : BasePotion
{
    private const int ExplosionRange = 2; // How long is the blast radius?

    private const bool LeveledExplosion = false; // Should explosion potions explode other nearby potions?
    private const bool InstantExplosion = false; // Should explosion potions explode on impact?
    private const bool RelativeLocation = false; // Is the explosion target location relative for mobiles?

    private Timer _timer;

    public BaseExplosionPotion(PotionEffect effect) : base(0xF0D, effect)
    {
    }

    public abstract int MinDamage { get; }
    public abstract int MaxDamage { get; }

    public override bool RequireFreeHand => false;

    private HashSet<Mobile> _users;

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

        _users ??= new HashSet<Mobile>();
        _users.Add(from);

        from.Target = new ThrowTarget(this);

        if (_timer?.Running != true)
        {
            from.SendLocalizedMessage(500236); // You should throw it now!

            if (Core.ML)
            {
                _timer = new DetonateTimer(this, from, TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.25), 5);
            }
            else
            {
                _timer = new DetonateTimer(this, from, TimeSpan.FromSeconds(0.75), TimeSpan.FromSeconds(1.0), 4);
            }

            _timer.Start();
        }
    }

    private void Reposition_OnTick(Mobile from, Point3D loc, Map map)
    {
        if (Deleted)
        {
            return;
        }

        if (InstantExplosion || _timer?.Running != true)
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

        if (_users != null)
        {
            foreach (var user in _users)
            {
                if ((user.Target as ThrowTarget)?.Potion == this)
                {
                    Target.Cancel(user);
                }
            }

            _users.Clear();
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

        var eable = map.GetObjectsInRange(loc, ExplosionRange);
        using var queue = PooledRefQueue<IEntity>.Create();

        var toDamage = 0;
        foreach (var entity in eable)
        {
            if (entity == this)
            {
                continue;
            }

            if (entity is Mobile mobile)
            {
                if (from == null || SpellHelper.ValidIndirectTarget(from, mobile) && from.CanBeHarmful(mobile, false))
                {
                    ++toDamage;
                    queue.Enqueue(entity);
                }
            }
            else if (LeveledExplosion && entity is BaseExplosionPotion)
            {
                queue.Enqueue(entity);
            }
        }

        var min = Scale(from, MinDamage);
        var max = Scale(from, MaxDamage);

        while (queue.Count > 0)
        {
            var entity = queue.Dequeue();

            if (entity is Mobile m)
            {
                from?.DoHarmful(m);

                var damage = Utility.RandomMinMax(min, max) + alchemyBonus;

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
            else if (entity is BaseExplosionPotion pot)
            {
                pot.Explode(from, false, pot.GetWorldLocation(), pot.Map);
            }
        }
    }

    private class ThrowTarget : Target
    {
        public ThrowTarget(BaseExplosionPotion potion) : base(Core.ML ? 12 : 10, true, TargetFlags.None) => Potion = potion;

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

            var delay = TimeSpan.FromSeconds(0.1 * from.GetDistanceToSqrt(loc));

            // If the potion is about to explode, stop the timer so it doesn't explode on you, while it is mid-air
            if (Potion._timer.RemainingCount <= 1 && Potion._timer.Next <= Core.Now + delay)
            {
                Potion._timer.Stop();
            }

            Timer.StartTimer(delay, () => Potion.Reposition_OnTick(from, loc, map));
        }
    }

    private class DetonateTimer : Timer
    {
        private BaseExplosionPotion _potion;
        private Mobile _from;

        public DetonateTimer(BaseExplosionPotion potion, Mobile from, TimeSpan delay, TimeSpan interval, int count) : base(delay, interval, count)
        {
            _from = from;
            _potion = potion;
        }

        protected override void OnTick()
        {
            if (_potion.Deleted)
            {
                return;
            }

            var parent = _potion.FindParent(_from);

            if (RemainingCount == 0)
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

                _potion.Explode(_from, true, loc, map);
            }
            else if (RemainingCount <= 3)
            {
                if (parent is Item item)
                {
                    item.PublicOverheadMessage(MessageType.Regular, 0x22, false, RemainingCount.ToString());
                }
                else if (parent is Mobile mobile)
                {
                    mobile.PublicOverheadMessage(MessageType.Regular, 0x22, false, RemainingCount.ToString());
                }
            }
        }
    }
}
