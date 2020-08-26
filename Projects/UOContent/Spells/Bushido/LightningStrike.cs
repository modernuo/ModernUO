using Server.Mobiles;

namespace Server.Spells.Bushido
{
  public class LightningStrike : SamuraiMove
  {
    public override int BaseMana => 5;
    public override double RequiredSkill => 50.0;

    public override TextDefinition AbilityMessage => new TextDefinition(1063167); // You prepare to strike quickly.

    public override bool DelayedContext => true;

    public override bool ValidatesDuringHit => false;

    public override int GetAccuracyBonus(Mobile attacker) => 50;

    public override bool Validate(Mobile from)
    {
      bool isValid = base.Validate(from);
      if (isValid)
      {
        PlayerMobile ThePlayer = from as PlayerMobile;
        ThePlayer.ExecutesLightningStrike = BaseMana;
      }

      return isValid;
    }

    public override bool IgnoreArmor(Mobile attacker)
    {
      double bushido = attacker.Skills.Bushido.Value;
      double criticalChance = bushido * bushido / 72000.0;
      return criticalChance >= Utility.RandomDouble();
    }

    public override bool OnBeforeSwing(Mobile attacker, Mobile defender)
    {
      /* no mana drain before actual hit */
      bool enoughMana = CheckMana(attacker, false);
      return Validate(attacker);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
      ClearCurrentMove(attacker);
      if (CheckMana(attacker, true))
      {
        attacker.SendLocalizedMessage(1063168); // You attack with lightning precision!
        defender.SendLocalizedMessage(1063169); // Your opponent's quick strike causes extra damage!
        defender.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
        defender.PlaySound(0x51D);
        CheckGain(attacker);
        SetContext(attacker);
      }
    }

    public override void OnClearMove(Mobile attacker)
    {
      PlayerMobile
        ThePlayer =
          attacker as PlayerMobile; // this can be deletet if the PlayerMobile parts are moved to Server.Mobile
      ThePlayer.ExecutesLightningStrike = 0;
    }
  }
}