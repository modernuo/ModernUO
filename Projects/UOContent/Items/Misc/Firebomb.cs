using System;
using System.Collections.Generic;
using System.Linq;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.Items
{
  public class Firebomb : Item
  {
    private Mobile m_LitBy;
    private int m_Ticks;
    private Timer m_Timer;
    private List<Mobile> m_Users;

    [Constructible]
    public Firebomb(int itemID = 0x99B) : base(itemID)
    {
      // Name = "a firebomb";
      Weight = 2.0;
      Hue = 1260;
    }

    public Firebomb(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (!IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        return;
      }

      if (Core.AOS && (from.Paralyzed || from.Frozen || from.Spell?.IsCasting == true))
      {
        // to prevent exploiting for pvp
        from.SendLocalizedMessage(1075857); // You cannot use that while paralyzed.
        return;
      }

      if (m_Timer == null)
      {
        m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnFirebombTimerTick);
        m_LitBy = from;
        from.SendLocalizedMessage(1060582); // You light the firebomb.  Throw it now!
      }
      else
      {
        from.SendLocalizedMessage(1060581); // You've already lit it!  Better throw it now!
      }

      m_Users ??= new List<Mobile>();

      if (!m_Users.Contains(from))
        m_Users.Add(from);

      from.Target = new ThrowTarget(this);
    }

    private void OnFirebombTimerTick()
    {
      if (Deleted)
      {
        m_Timer.Stop();
        return;
      }

      if (Map == Map.Internal && HeldBy == null)
        return;

      switch (m_Ticks)
      {
        case 0:
        case 1:
        case 2:
          {
            ++m_Ticks;

            if (HeldBy != null)
              HeldBy.PublicOverheadMessage(MessageType.Regular, 957, false, m_Ticks.ToString());
            else if (RootParent == null)
              PublicOverheadMessage(MessageType.Regular, 957, false, m_Ticks.ToString());
            else if (RootParent is Mobile mobile)
              mobile.PublicOverheadMessage(MessageType.Regular, 957, false, m_Ticks.ToString());

            break;
          }
        default:
          {
            HeldBy?.DropHolding();

            if (m_Users != null)
            {
              foreach (Mobile m in m_Users)
                if (m.Target is ThrowTarget targ && targ.Bomb == this)
                  Target.Cancel(m);

              m_Users.Clear();
              m_Users = null;
            }

            if (RootParent is Mobile parent)
            {
              parent.SendLocalizedMessage(1060583); // The firebomb explodes in your hand!
              AOS.Damage(parent, Utility.Random(3) + 4, 0, 100, 0, 0, 0);
            }
            else if (RootParent == null)
            {
              IPooledEnumerable<Mobile> eable = Map.GetMobilesInRange(Location, 1);
              List<Mobile> toDamage = eable.ToList();

              eable.Free();

              for (int i = 0; i < toDamage.Count; ++i)
              {
                Mobile victim = toDamage[i];

                if (m_LitBy == null || (SpellHelper.ValidIndirectTarget(m_LitBy, victim) &&
                    m_LitBy.CanBeHarmful(victim, false)))
                {
                  m_LitBy?.DoHarmful(victim);

                  AOS.Damage(victim, m_LitBy, Utility.Random(3) + 4, 0, 100, 0, 0, 0);
                }
              }

              new FirebombField(m_LitBy, toDamage).MoveToWorld(Location, Map);
            }

            m_Timer.Stop();
            Delete();
            break;
          }
      }
    }

    private void OnFirebombTarget(Mobile from, object obj)
    {
      if (Deleted || Map == Map.Internal || !IsChildOf(from.Backpack))
        return;

      if (!(obj is IPoint3D p))
        return;

      SpellHelper.GetSurfaceTop(ref p);

      from.RevealingAction();

      IEntity to = p as IEntity ?? new Entity(Serial.Zero, new Point3D(p), Map);

      Effects.SendMovingEffect(from, to, ItemID, 7, 0, false, false, Hue);

      Timer.DelayCall(TimeSpan.FromSeconds(1.0), FirebombReposition_OnTick, p, Map);
      Internalize();
    }

    private void FirebombReposition_OnTick(IPoint3D p, Map map)
    {
      if (Deleted)
        return;

      MoveToWorld(new Point3D(p), map);
    }

    private class ThrowTarget : Target
    {
      public ThrowTarget(Firebomb bomb)
        : base(12, true, TargetFlags.None) =>
        Bomb = bomb;

      public Firebomb Bomb { get; }

      protected override void OnTarget(Mobile from, object targeted)
      {
        Bomb.OnFirebombTarget(from, targeted);
      }
    }
  }

  public class FirebombField : Item
  {
    private readonly List<Mobile> m_Burning;
    private readonly DateTime m_Expire;
    private readonly Mobile m_LitBy;
    private readonly Timer m_Timer;

    public FirebombField(Mobile litBy, List<Mobile> toDamage) : base(0x376A)
    {
      Movable = false;
      m_LitBy = litBy;
      m_Expire = DateTime.UtcNow + TimeSpan.FromSeconds(10);
      m_Burning = toDamage;
      m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnFirebombFieldTimerTick);
    }

    public FirebombField(Serial serial) : base(serial)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
      // Don't serialize these...
    }

    public override void Deserialize(IGenericReader reader)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
      if ((ItemID == 0x398C && m_LitBy == null) ||
          (SpellHelper.ValidIndirectTarget(m_LitBy, m) && m_LitBy.CanBeHarmful(m, false)))
      {
        m_LitBy?.DoHarmful(m);

        AOS.Damage(m, m_LitBy, 2, 0, 100, 0, 0, 0);
        m.PlaySound(0x208);

        if (!m_Burning.Contains(m))
          m_Burning.Add(m);
      }

      return true;
    }

    private void OnFirebombFieldTimerTick()
    {
      if (Deleted)
      {
        m_Timer.Stop();
        return;
      }

      if (ItemID == 0x376A)
      {
        ItemID = 0x398C;
        return;
      }

      for (int i = 0; i < m_Burning.Count;)
      {
        Mobile victim = m_Burning[i];

        if (victim.Location == Location && victim.Map == Map &&
            (m_LitBy == null || (SpellHelper.ValidIndirectTarget(m_LitBy, victim) &&
             m_LitBy.CanBeHarmful(victim, false))))
        {
          m_LitBy?.DoHarmful(victim);

          AOS.Damage(victim, m_LitBy, Utility.Random(3) + 4, 0, 100, 0, 0, 0);
          ++i;
        }
        else
        {
          m_Burning.RemoveAt(i);
        }
      }

      if (DateTime.UtcNow >= m_Expire)
      {
        m_Timer.Stop();
        Delete();
      }
    }
  }
}
