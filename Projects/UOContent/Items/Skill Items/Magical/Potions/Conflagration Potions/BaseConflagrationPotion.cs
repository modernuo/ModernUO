using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseConflagrationPotion : BasePotion
{
    private static readonly Dictionary<Mobile, TimerExecutionToken> m_Delay = new();
    private readonly List<Mobile> m_Users = new();

    public BaseConflagrationPotion(PotionEffect effect) : base(0xF06, effect) => Hue = 0x489;

    public abstract int MinDamage { get; }
    public abstract int MaxDamage { get; }

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
            // You cannot use that for another ~1_NUM~ ~2_TIMEUNITS~
            from.SendLocalizedMessage(1072529, $"{delay}\t{(delay > 1 ? "seconds." : "second.")}");
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
        Effects.PlaySound(loc, map, 0x20C);

        for (var i = -2; i <= 2; i++)
        {
            for (var j = -2; j <= 2; j++)
            {
                var p = new Point3D(loc.X + i, loc.Y + j, loc.Z);

                if (map.CanFit(p, 12, true, false) && from.InLOS(p))
                {
                    new InternalItem(from, p, map, MinDamage, MaxDamage);
                }
            }
        }
    }

    public static void AddDelay(Mobile m)
    {
        m_Delay.TryGetValue(m, out var timer);
        timer.Cancel();

        Timer.StartTimer(TimeSpan.FromSeconds(30), () => EndDelay(m), out timer);
        m_Delay[m] = timer;
    }

    public static int GetDelay(Mobile m)
    {
        if (m_Delay.TryGetValue(m, out var timer) && timer.Next > Core.Now)
        {
            return (int)Math.Round((timer.Next - Core.Now).TotalSeconds);
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
        public ThrowTarget(BaseConflagrationPotion potion) : base(12, true, TargetFlags.None) => Potion = potion;

        public BaseConflagrationPotion Potion { get; }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (Potion.Deleted || Potion.Map == Map.Internal)
            {
                return;
            }

            if (targeted is not IPoint3D p || from.Map == null)
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
            Timer.StartTimer(TimeSpan.FromSeconds(1.5), () => Potion.Explode(from, loc, map));
        }
    }

    [SerializationGenerator(0, false)]
    public partial class InternalItem : Item
    {
        [SerializableField(0, setter: "private")]
        private Mobile _from;

        [SerializableField(1, getter: "private", setter: "private")]
        private DateTime _end;

        [SerializableField(2, getter: "private", setter: "private")]
        private int _minDamage;

        [SerializableField(3, getter: "private", setter: "private")]
        private int _maxDamage;

        private Timer _timer;

        public InternalItem(Mobile from, Point3D loc, Map map, int min, int max) : base(0x398C)
        {
            Movable = false;
            Light = LightType.Circle300;

            MoveToWorld(loc, map);

            From = from;
            _end = Core.Now + TimeSpan.FromSeconds(10);

            SetDamage(min, max);

            _timer = new InternalTimer(this, _end);
            _timer.Start();
        }

        public override bool BlocksFit => true;

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            _timer?.Stop();
        }

        public int GetDamage() => Utility.RandomMinMax(_minDamage, _maxDamage);

        private void SetDamage(int min, int max)
        {
            /**
             * new way to apply alchemy bonus according to Stratics' calculator.
             * this gives a mean to values 25, 50, 75 and 100. Stratics' calculator is outdated.
             * Those goals will give 2 to alchemy bonus. It's not really OSI-like but it's an approximation.
             */
            _minDamage = min;
            _maxDamage = max;

            if (From == null)
            {
                return;
            }

            var alchemySkill = From.Skills.Alchemy.Fixed;
            var alchemyBonus = alchemySkill / 125 + alchemySkill / 250;

            _minDamage = Scale(From, _minDamage + alchemyBonus);
            _maxDamage = Scale(From, _maxDamage + alchemyBonus);
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            _timer = new InternalTimer(this, _end);
            _timer.Start();
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (Visible && From != null && (!Core.AOS || m != From) && SpellHelper.ValidIndirectTarget(From, m) &&
                From.CanBeHarmful(m, false))
            {
                From.DoHarmful(m);

                AOS.Damage(m, From, GetDamage(), 0, 100, 0, 0, 0);
                m.PlaySound(0x208);
            }

            return true;
        }

        private class InternalTimer : Timer
        {
            private readonly DateTime _end;
            private readonly InternalItem _item;

            public InternalTimer(InternalItem item, DateTime end) : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {
                _item = item;
                _end = end;
            }

            protected override void OnTick()
            {
                if (_item.Deleted)
                {
                    return;
                }

                if (Core.Now > _end)
                {
                    _item.Delete();
                    Stop();
                    return;
                }

                var from = _item.From;

                if (_item.Map == null || from == null)
                {
                    return;
                }

                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in _item.GetMobilesAt())
                {
                    if (m.Z + 16 > _item.Z && _item.Z + 12 > m.Z && (!Core.AOS || m != from) &&
                        SpellHelper.ValidIndirectTarget(from, m) && from.CanBeHarmful(m, false))
                    {
                        queue.Enqueue(m);
                    }
                }

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();

                    from.DoHarmful(m);
                    AOS.Damage(m, from, _item.GetDamage(), 0, 100, 0, 0, 0);
                    m.PlaySound(0x208);
                }
            }
        }
    }
}
