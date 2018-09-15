using System;

namespace Server.Spells
{
  public class FlySpell : Spell
  {
    private static readonly SpellInfo m_Info = new SpellInfo("Gargoyle Flight", null, -1, 9002);
    private bool m_Stop;

    public FlySpell(Mobile caster)
      : base(caster, null, m_Info)
    {
    }

    public override bool ClearHandsOnCast => false;

    public override bool RevealOnCast => false;

    public override double CastDelayFastScalar => 0;

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(.25);

    public override TimeSpan GetCastRecovery()
    {
      return TimeSpan.Zero;
    }

    public override int GetMana()
    {
      return 0;
    }

    public override bool ConsumeReagents()
    {
      return true;
    }

    public override bool CheckFizzle()
    {
      return true;
    }

    public void Stop()
    {
      m_Stop = true;
      Disturb(DisturbType.Hurt, false, false);
    }

    public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable)
    {
      if (type == DisturbType.EquipRequest || type == DisturbType.UseRequest /* || type == DisturbType.Hurt*/)
        return false;

      return true;
    }

    public override void DoHurtFizzle()
    {
    }

    public override void DoFizzle()
    {
    }

    public override void OnDisturb(DisturbType type, bool message)
    {
      if (message && !m_Stop)
        Caster.SendLocalizedMessage(1113192); // You have been disrupted while attempting to fly!
    }

    public override void OnCast()
    {
      Caster.Flying = false;
      BuffInfo.RemoveBuff(Caster, BuffIcon.Fly);
      Caster.Animate(60, 10, 1, true, false, 0);
      Caster.SendLocalizedMessage(1112567); // You are flying.
      Caster.Flying = true;
      BuffInfo.AddBuff(Caster, new BuffInfo(BuffIcon.Fly, 1112567));
      FinishSequence();
    }
  }
}