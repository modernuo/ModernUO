using System;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Seventh
{
  public class EnergyFieldSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Energy Field", "In Sanct Grav",
      221,
      9022,
      false,
      Reagent.BlackPearl,
      Reagent.MandrakeRoot,
      Reagent.SpidersSilk,
      Reagent.SulfurousAsh);

    public EnergyFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Seventh;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this, TargetFlags.None, Core.ML ? 10 : 12);
    }

    public void Target(IPoint3D p)
    {
      if (!Caster.CanSee(p))
      {
        Caster.SendLocalizedMessage(500237); // Target can not be seen.
      }
      else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        SpellHelper.GetSurfaceTop(ref p);

        int dx = Caster.Location.X - p.X;
        int dy = Caster.Location.Y - p.Y;
        int rx = (dx - dy) * 44;
        int ry = (dx + dy) * 44;

        bool eastToWest;

        if (rx >= 0 && ry >= 0)
          eastToWest = false;
        else if (rx >= 0)
          eastToWest = true;
        else if (ry >= 0)
          eastToWest = true;
        else
          eastToWest = false;

        Effects.PlaySound(p, Caster.Map, 0x20B);

        TimeSpan duration;

        if (Core.AOS)
          duration = TimeSpan.FromSeconds((15 + Caster.Skills.Magery.Fixed / 5) / 7.0);
        else
          duration = TimeSpan.FromSeconds(Caster.Skills.Magery.Value * 0.28 +
                                          2.0); // (28% of magery) + 2.0 seconds

        int itemID = eastToWest ? 0x3946 : 0x3956;

        for (int i = -2; i <= 2; ++i)
        {
          Point3D loc = new Point3D(eastToWest ? p.X + i : p.X, eastToWest ? p.Y : p.Y + i, p.Z);
          bool canFit = SpellHelper.AdjustField(ref loc, Caster.Map, 12, false);

          if (!canFit)
            continue;

          Item item = new InternalItem(loc, Caster.Map, duration, itemID, Caster);
          item.ProcessDelta();

          Effects.SendLocationParticles(EffectItem.Create(loc, Caster.Map, EffectItem.DefaultDuration), 0x376A, 9,
            10, 5051);
        }
      }

      FinishSequence();
    }

    [DispellableField]
    private class InternalItem : Item
    {
      private readonly Mobile m_Caster;
      private readonly Timer m_Timer;

      public InternalItem(Point3D loc, Map map, TimeSpan duration, int itemID, Mobile caster) : base(itemID)
      {
        Visible = false;
        Movable = false;
        Light = LightType.Circle300;

        MoveToWorld(loc, map);

        m_Caster = caster;

        if (caster.InLOS(this))
          Visible = true;
        else
          Delete();

        if (Deleted)
          return;

        m_Timer = new InternalTimer(this, duration);
        m_Timer.Start();
      }

      public InternalItem(Serial serial) : base(serial)
      {
        m_Timer = new InternalTimer(this, TimeSpan.FromSeconds(5.0));
        m_Timer.Start();
      }

      public override bool BlocksFit => true;

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

      public override bool OnMoveOver(Mobile m)
      {
        if (!(m is PlayerMobile))
          return base.OnMoveOver(m);

        int noto = Notoriety.Compute(m_Caster, m);
        return noto != Notoriety.Enemy && noto != Notoriety.Ally && base.OnMoveOver(m);
      }

      public override void OnAfterDelete()
      {
        base.OnAfterDelete();

        m_Timer?.Stop();
      }

      private class InternalTimer : Timer
      {
        private readonly InternalItem m_Item;

        public InternalTimer(InternalItem item, TimeSpan duration) : base(duration)
        {
          Priority = TimerPriority.OneSecond;
          m_Item = item;
        }

        protected override void OnTick()
        {
          m_Item.Delete();
        }
      }
    }
  }
}
