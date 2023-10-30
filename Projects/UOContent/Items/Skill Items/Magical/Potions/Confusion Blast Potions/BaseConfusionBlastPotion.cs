using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Misc;
using Server.Mobiles;
using Server.Spells;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class BaseConfusionBlastPotion : BasePotion
{
    private static readonly Dictionary<Mobile, TimerExecutionToken> _delay = new();
    private HashSet<Mobile> _users;

    public BaseConfusionBlastPotion(PotionEffect effect) : base(0xF06, effect) => Hue = 0x48D;

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
            // You cannot use that for another ~1_NUM~ ~2_TIMEUNITS~
            from.SendLocalizedMessage(1072529, $"{delay}\t{(delay > 1 ? "seconds." : "second.")}");
            return;
        }

        if (from.Target is ThrowTarget targ && targ.Potion == this)
        {
            return;
        }

        from.RevealingAction();

        _users ??= new HashSet<Mobile>();
        _users.Add(from);

        from.Target = new ThrowTarget(this);
    }

    public virtual void Explode(Mobile from, Point3D loc, Map map)
    {
        if (Deleted || map == null)
        {
            return;
        }

        Consume();

        if (_users != null)
        {
            // Check if any other players are using this potion
            foreach (var user in _users)
            {
                if ((user.Target as ThrowTarget)?.Potion == this)
                {
                    Target.Cancel(from);
                }
            }

            _users.Clear();
        }

        // Effects
        Effects.PlaySound(loc, map, 0x207);

        Geometry.Circle2D(loc, map, Radius, BlastEffect, 270, 90);

        Timer.StartTimer(TimeSpan.FromSeconds(0.3), () => CircleEffect2(loc, map));

        foreach (var mobile in map.GetMobilesInRange(loc, Radius))
        {
            if (mobile is BaseCreature { Controlled: false, Summoned: false } mon)
            {
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
        _delay.TryGetValue(m, out var timer);
        timer.Cancel();

        Timer.StartTimer(TimeSpan.FromSeconds(60), () => EndDelay(m), out timer);
        _delay[m] = timer;
    }

    public static int GetDelay(Mobile m)
    {
        if (_delay.TryGetValue(m, out var timer) && timer.Next > Core.Now)
        {
            return (int)(timer.Next - Core.Now).TotalSeconds;
        }

        return 0;
    }

    public static void EndDelay(Mobile m)
    {
        if (_delay.Remove(m, out var timer))
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
            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Potion.Explode(from, loc, map));
        }
    }
}
