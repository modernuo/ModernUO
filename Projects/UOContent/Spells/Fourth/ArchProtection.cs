using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.PartySystem;
using Server.Spells.Second;
using Server.Targeting;

namespace Server.Spells.Fourth
{
  public class ArchProtectionSpell : MagerySpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Arch Protection", "Vas Uus Sanct",
      Core.AOS ? 239 : 215,
      9011,
      Reagent.Garlic,
      Reagent.Ginseng,
      Reagent.MandrakeRoot,
      Reagent.SulfurousAsh);

    private static readonly Dictionary<Mobile, int> _Table = new Dictionary<Mobile, int>();

    public ArchProtectionSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fourth;

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
      else if (CheckSequence())
      {
        SpellHelper.Turn(Caster, p);

        SpellHelper.GetSurfaceTop(ref p);

        if (!Core.AOS)
          Effects.PlaySound(p, Caster.Map, 0x299);

        if (Caster.Map == null)
        {
          FinishSequence();
          return;
        }

        IEnumerable<Mobile> targets = Caster.Map.GetMobilesInRange(new Point3D(p), Core.AOS ? 2 : 3)
          .Where(m => Caster.CanBeBeneficial(m, false));

        if (Core.AOS)
        {
          Party party = Party.Get(Caster);

          foreach (Mobile m in targets)
            if (m == Caster || party?.Contains(m) == true)
            {
              Caster.DoBeneficial(m);
              ProtectionSpell.Toggle(Caster, m);
            }
        }
        else
        {
          int val = (int)(Caster.Skills.Magery.Value / 10.0 + 1);

          foreach (Mobile m in targets)
            if (m.BeginAction<ArchProtectionSpell>())
            {
              Caster.DoBeneficial(m);
              m.VirtualArmorMod += val;

              AddEntry(m, val);
              new InternalTimer(m, Caster).Start();

              m.FixedParticles(0x375A, 9, 20, 5027, EffectLayer.Waist);
              m.PlaySound(0x1F7);
            }
        }
      }

      FinishSequence();
    }

    private static void AddEntry(Mobile m, int v)
    {
      _Table[m] = v;
    }

    public static void RemoveEntry(Mobile m)
    {
      if (_Table.TryGetValue(m, out int v))
      {
        _Table.Remove(m);
        m.EndAction<ArchProtectionSpell>();
        m.VirtualArmorMod -= Math.Min(v, m.VirtualArmorMod);
      }
    }

    private class InternalTimer : Timer
    {
      private readonly Mobile m_Owner;

      public InternalTimer(Mobile target, Mobile caster) : base(TimeSpan.FromSeconds(0))
      {
        double time = caster.Skills.Magery.Value * 1.2;
        if (time > 144)
          time = 144;
        Delay = TimeSpan.FromSeconds(time);
        Priority = TimerPriority.OneSecond;

        m_Owner = target;
      }

      protected override void OnTick()
      {
        RemoveEntry(m_Owner);
      }
    }
  }
}
