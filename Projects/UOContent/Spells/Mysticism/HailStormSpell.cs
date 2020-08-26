using System;
using System.Collections.Generic;
using Server.Network;

namespace Server.Spells.Mysticism
{
  public class HailStormSpell : MysticSpell, ISpellTargetingPoint3D
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Hail Storm", "Kal Des Ylem",
      -1,
      9002,
      Reagent.DragonsBlood,
      Reagent.Bloodmoss,
      Reagent.BlackPearl,
      Reagent.MandrakeRoot);

    public HailStormSpell(Mobile caster, Item scroll = null)
      : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.25);

    public override double RequiredSkill => 70.0;
    public override int RequiredMana => 40;

    public override void OnCast()
    {
      Caster.Target = new SpellTargetPoint3D(this);
    }

    public void Target(IPoint3D p)
    {
      if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
      {
        /* Summons a storm of hailstones that strikes all Targets
         * within a radius around the Target's Location, dealing
         * cold damage.
         */

        SpellHelper.Turn(Caster, p);

        if (p is Item item)
          p = item.GetWorldLocation();

        List<Mobile> targets = new List<Mobile>();

        Map map = Caster.Map;

        bool pvp = false;

        if (map != null)
        {
          PlayEffect(p, Caster.Map);

          foreach (Mobile m in map.GetMobilesInRange(new Point3D(p), 2))
          {
            if (m == Caster)
              continue;

            if (SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false) && Caster.CanSee(m))
            {
              if (!Caster.InLOS(m))
                continue;

              targets.Add(m);

              if (m.Player)
                pvp = true;
            }
          }
        }

        double damage = GetNewAosDamage(51, 1, 5, pvp);

        foreach (Mobile m in targets)
        {
          Caster.DoHarmful(m);
          SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);
        }
      }

      FinishSequence();
    }

    private static void PlayEffect(IPoint3D p, Map map)
    {
      Effects.PlaySound(p, map, 0x64F);

      PlaySingleEffect(p, map, -1, 1, -1, 1);
      PlaySingleEffect(p, map, -2, 0, -3, -1);
      PlaySingleEffect(p, map, -3, -1, -1, 1);
      PlaySingleEffect(p, map, 1, 3, -1, 1);
      PlaySingleEffect(p, map, -1, 1, 1, 3);
    }

    private static void PlaySingleEffect(IPoint3D p, Map map, int a, int b, int c, int d)
    {
      int x = p.X, y = p.Y, z = p.Z + 18;

      SendEffectPacket(p, map, new Point3D(x + a, y + c, z), new Point3D(x + a, y + c, z));
      SendEffectPacket(p, map, new Point3D(x + b, y + c, z), new Point3D(x + b, y + c, z));
      SendEffectPacket(p, map, new Point3D(x + b, y + d, z), new Point3D(x + b, y + d, z));
      SendEffectPacket(p, map, new Point3D(x + a, y + d, z), new Point3D(x + a, y + d, z));

      SendEffectPacket(p, map, new Point3D(x + b, y + c, z), new Point3D(x + a, y + c, z));
      SendEffectPacket(p, map, new Point3D(x + b, y + d, z), new Point3D(x + b, y + c, z));
      SendEffectPacket(p, map, new Point3D(x + a, y + d, z), new Point3D(x + b, y + d, z));
      SendEffectPacket(p, map, new Point3D(x + a, y + c, z), new Point3D(x + a, y + d, z));
    }

    private static void SendEffectPacket(IPoint3D p, Map map, Point3D orig, Point3D dest)
    {
      Effects.SendPacket(p, map,
        new HuedEffect(EffectType.Moving, Serial.Zero, Serial.Zero, 0x36D4, orig, dest, 0, 0, false, false, 0x63,
          0x4));
    }
  }
}
