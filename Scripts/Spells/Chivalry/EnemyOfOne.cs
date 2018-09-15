using System;
using System.Collections;
using Server.Mobiles;

namespace Server.Spells.Chivalry
{
  public class EnemyOfOneSpell : PaladinSpell
  {
    private static SpellInfo m_Info = new SpellInfo(
      "Enemy of One", "Forul Solum",
      -1,
      9002
    );

    private static Hashtable m_Table = new Hashtable();

    public EnemyOfOneSpell(Mobile caster, Item scroll) : base(caster, scroll, m_Info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5);

    public override double RequiredSkill => 45.0;
    public override int RequiredMana => 20;
    public override int RequiredTithing => 10;
    public override int MantraNumber => 1060723; // Forul Solum
    public override bool BlocksMovement => false;

    public override void OnCast()
    {
      if (CheckSequence())
      {
        Caster.PlaySound(0x0F5);
        Caster.PlaySound(0x1ED);
        Caster.FixedParticles(0x375A, 1, 30, 9966, 33, 2, EffectLayer.Head);
        Caster.FixedParticles(0x37B9, 1, 30, 9502, 43, 3, EffectLayer.Head);

        Timer t = (Timer)m_Table[Caster];

        t?.Stop();

        double delay = (double)ComputePowerValue(1) / 60;

        // TODO: Should caps be applied?
        if (delay < 1.5)
          delay = 1.5;
        else if (delay > 3.5)
          delay = 3.5;

        m_Table[Caster] = Timer.DelayCall(TimeSpan.FromMinutes(delay), new TimerStateCallback(Expire_Callback),
          Caster);

        if (Caster is PlayerMobile)
        {
          ((PlayerMobile)Caster).EnemyOfOneType = null;
          ((PlayerMobile)Caster).WaitingForEnemy = true;

          BuffInfo.AddBuff(Caster,
            new BuffInfo(BuffIcon.EnemyOfOne, 1075653, 1044111, TimeSpan.FromMinutes(delay), Caster));
        }
      }

      FinishSequence();
    }

    private static void Expire_Callback(object state)
    {
      Mobile m = (Mobile)state;

      m_Table.Remove(m);

      m.PlaySound(0x1F8);

      if (m is PlayerMobile)
      {
        ((PlayerMobile)m).EnemyOfOneType = null;
        ((PlayerMobile)m).WaitingForEnemy = false;
      }
    }
  }
}