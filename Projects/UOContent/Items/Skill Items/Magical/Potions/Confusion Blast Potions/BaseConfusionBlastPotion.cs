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
    private readonly List<Mobile> m_Users = new List<Mobile>();

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

      int delay = GetDelay(from);

      if (delay > 0)
      {
        from.SendLocalizedMessage(1072529,
          $"{delay}\t{(delay > 1 ? "seconds." : "second.")}"); // You cannot use that for another ~1_NUM~ ~2_TIMEUNITS~
        return;
      }

      if (from.Target is ThrowTarget targ && targ.Potion == this)
        return;

      from.RevealingAction();

      if (!m_Users.Contains(from))
        m_Users.Add(from);

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

      int version = reader.ReadInt();
    }

    public virtual void Explode(Mobile from, Point3D loc, Map map)
    {
      if (Deleted || map == null)
        return;

      Consume();

      // Check if any other players are using this potion
      for (int i = 0; i < m_Users.Count; i++)
        if (m_Users[i].Target is ThrowTarget targ && targ.Potion == this)
          Target.Cancel(from);

      // Effects
      Effects.PlaySound(loc, map, 0x207);

      Geometry.Circle2D(loc, map, Radius, BlastEffect, 270, 90);

      Timer.DelayCall(TimeSpan.FromSeconds(0.3), CircleEffect2, loc, map);

      foreach (Mobile mobile in map.GetMobilesInRange(loc, Radius))
        if (mobile is BaseCreature mon)
        {
          if (mon.Controlled || mon.Summoned)
            continue;

          mon.Pacify(from, DateTime.UtcNow + TimeSpan.FromSeconds(5.0)); // TODO check
        }
    }

    private class ThrowTarget : Target
    {
      public ThrowTarget(BaseConfusionBlastPotion potion) : base(12, true, TargetFlags.None) => Potion = potion;

      public BaseConfusionBlastPotion Potion { get; }

      protected override void OnTarget(Mobile from, object targeted)
      {
        if (Potion.Deleted || Potion.Map == Map.Internal)
          return;

        if (!(targeted is IPoint3D p) || from.Map == null)
          return;

        // Add delay
        AddDelay(from);

        SpellHelper.GetSurfaceTop(ref p);

        from.RevealingAction();

        IEntity to;

        if (p is Mobile mobile)
          to = mobile;
        else
          to = new Entity(Serial.Zero, new Point3D(p), from.Map);

        Effects.SendMovingEffect(from, to, 0xF0D, 7, 0, false, false, Potion.Hue);
        Timer.DelayCall(TimeSpan.FromSeconds(1.0), Potion.Explode, from, new Point3D(p), from.Map);
      }
    }

    public virtual void BlastEffect(Point3D p, Map map)
    {
      if (map.CanFit(p, 12, true, false))
        Effects.SendLocationEffect(p, map, 0x376A, 4, 9);
    }

    public void CircleEffect2(Point3D p, Map m)
    {
      Geometry.Circle2D(p, m, Radius, BlastEffect, 90, 270);
    }

    private static readonly Dictionary<Mobile, Timer> m_Delay = new Dictionary<Mobile, Timer>();

    public static void AddDelay(Mobile m)
    {
      m_Delay.TryGetValue(m, out Timer timer);
      timer?.Stop();
      m_Delay[m] = Timer.DelayCall(TimeSpan.FromSeconds(60), EndDelay, m);
    }

    public static int GetDelay(Mobile m)
    {
      if (m_Delay.TryGetValue(m, out Timer timer) && timer.Next > DateTime.UtcNow)
        return (int)(timer.Next - DateTime.UtcNow).TotalSeconds;

      return 0;
    }

    public static void EndDelay(Mobile m)
    {
      if (m_Delay.TryGetValue(m, out Timer timer))
      {
        timer.Stop();
        m_Delay.Remove(m);
      }
    }
  }
}
