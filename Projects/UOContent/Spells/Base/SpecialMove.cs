using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Network;
using Server.Spells.Bushido;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server.Spells
{
  public abstract class SpecialMove
  {
    private static readonly Dictionary<Mobile, SpecialMoveContext> m_PlayersTable = new Dictionary<Mobile, SpecialMoveContext>();

    public virtual int BaseMana => 0;

    public virtual SkillName MoveSkill => SkillName.Bushido;
    public virtual double RequiredSkill => 0.0;

    public virtual TextDefinition AbilityMessage => 0;

    public virtual bool BlockedByAnimalForm => true;
    public virtual bool DelayedContext => false;

    public static Dictionary<Mobile, SpecialMove> Table { get; } = new Dictionary<Mobile, SpecialMove>();

    public virtual bool ValidatesDuringHit => true;

    public virtual int GetAccuracyBonus(Mobile attacker) => 0;

    public virtual double GetDamageScalar(Mobile attacker, Mobile defender) => 1.0;

    // Called before swinging, to make sure the accuracy scalar is to be computed.
    public virtual bool OnBeforeSwing(Mobile attacker, Mobile defender) => true;

    // Called when a hit connects, but before damage is calculated.
    public virtual bool OnBeforeDamage(Mobile attacker, Mobile defender) => true;

    // Called as soon as the ability is used.
    public virtual void OnUse(Mobile from)
    {
    }

    // Called when a hit connects, at the end of the weapon.OnHit() method.
    public virtual void OnHit(Mobile attacker, Mobile defender, int damage)
    {
    }

    // Called when a hit misses.
    public virtual void OnMiss(Mobile attacker, Mobile defender)
    {
    }

    // Called when the move is cleared.
    public virtual void OnClearMove(Mobile from)
    {
    }

    public virtual bool IgnoreArmor(Mobile attacker) => false;

    public virtual double GetPropertyBonus(Mobile attacker) => 1.0;

    public virtual bool CheckSkills(Mobile m)
    {
      if (m.Skills[MoveSkill].Value < RequiredSkill)
      {
        string args = $"{RequiredSkill:F1}\t{MoveSkill.ToString()}\t ";
        m.SendLocalizedMessage(1063013,
          args); // You need at least ~1_SKILL_REQUIREMENT~ ~2_SKILL_NAME~ skill to use that ability.
        return false;
      }

      return true;
    }

    public virtual int ScaleMana(Mobile m, int mana)
    {
      double scalar = 1.0;

      if (!MindRotSpell.GetMindRotScalar(m, ref scalar))
        scalar = 1.0;

      // Lower Mana Cost = 40%
      int lmc = Math.Min(AosAttributes.GetValue(m, AosAttribute.LowerManaCost), 40);

      scalar -= (double)lmc / 100;

      int total = (int)(mana * scalar);

      if (m.Skills[MoveSkill].Value < 50.0 && GetContext(m) != null)
        total *= 2;

      return total;
    }

    public virtual bool CheckMana(Mobile from, bool consume)
    {
      int mana = ScaleMana(from, BaseMana);

      if (from.Mana < mana)
      {
        from.SendLocalizedMessage(1060181,
          mana.ToString()); // You need ~1_MANA_REQUIREMENT~ mana to perform that attack
        return false;
      }

      if (consume)
      {
        if (!DelayedContext)
          SetContext(from);

        from.Mana -= mana;
      }

      return true;
    }

    public virtual void SetContext(Mobile from)
    {
      if (GetContext(from) == null)
        if (DelayedContext || from.Skills[MoveSkill].Value < 50.0)
        {
          Timer timer = new SpecialMoveTimer(from);
          timer.Start();

          AddContext(from, new SpecialMoveContext(timer, GetType()));
        }
    }

    public virtual bool Validate(Mobile from)
    {
      if (!from.Player)
        return true;

      if (HonorableExecution.IsUnderPenalty(from))
      {
        from.SendLocalizedMessage(1063024); // You cannot perform this special move right now.
        return false;
      }

      if (AnimalForm.UnderTransformation(from))
      {
        from.SendLocalizedMessage(1063024); // You cannot perform this special move right now.
        return false;
      }

      string option = null;

      if (this is Backstab)
        option = "Backstab";
      else if (this is DeathStrike)
        option = "Death Strike";
      else if (this is FocusAttack)
        option = "Focus Attack";
      else if (this is KiAttack)
        option = "Ki Attack";
      else if (this is SurpriseAttack)
        option = "Surprise Attack";
      else if (this is HonorableExecution)
        option = "Honorable Execution";
      else if (this is LightningStrike)
        option = "Lightning Strike";
      else if (this is MomentumStrike)
        option = "Momentum Strike";

      if (option != null && !DuelContext.AllowSpecialMove(from, option, this))
        return false;

      return CheckSkills(from) && CheckMana(from, false);
    }

    public virtual void CheckGain(Mobile m)
    {
      m.CheckSkill(MoveSkill, RequiredSkill, RequiredSkill + 37.5);
    }

    public static void ClearAllMoves(Mobile m)
    {
      foreach (KeyValuePair<int, SpecialMove> kvp in SpellRegistry.SpecialMoves)
      {
        int moveID = kvp.Key;

        if (moveID != -1)
          m.Send(new ToggleSpecialAbility(moveID + 1, false));
      }
    }

    public static SpecialMove GetCurrentMove(Mobile m)
    {
      if (m == null)
        return null;

      if (!Core.SE)
      {
        ClearCurrentMove(m);
        return null;
      }

      if (Table.TryGetValue(m, out SpecialMove move) && move.ValidatesDuringHit && !move.Validate(m))
      {
        ClearCurrentMove(m);
        return null;
      }

      return move;
    }

    public static bool SetCurrentMove(Mobile m, SpecialMove move)
    {
      if (!Core.SE)
      {
        ClearCurrentMove(m);
        return false;
      }

      if (move?.Validate(m) == false)
      {
        ClearCurrentMove(m);
        return false;
      }

      bool sameMove = move == GetCurrentMove(m);

      ClearCurrentMove(m);

      if (sameMove)
        return true;

      if (move != null)
      {
        WeaponAbility.ClearCurrentAbility(m);

        Table[m] = move;

        move.OnUse(m);

        int moveID = SpellRegistry.GetRegistryNumber(move);

        if (moveID > 0)
          m.Send(new ToggleSpecialAbility(moveID + 1, true));

        TextDefinition.SendMessageTo(m, move.AbilityMessage);
      }

      return true;
    }

    public static void ClearCurrentMove(Mobile m)
    {
      if (Table.TryGetValue(m, out SpecialMove move))
      {
        move.OnClearMove(m);

        int moveID = SpellRegistry.GetRegistryNumber(move);

        if (moveID > 0)
          m.Send(new ToggleSpecialAbility(moveID + 1, false));
      }

      Table.Remove(m);
    }

    private static void AddContext(Mobile m, SpecialMoveContext context)
    {
      m_PlayersTable[m] = context;
    }

    private static void RemoveContext(Mobile m)
    {
      SpecialMoveContext context = GetContext(m);

      if (context != null)
      {
        m_PlayersTable.Remove(m);

        context.Timer.Stop();
      }
    }

    private static SpecialMoveContext GetContext(Mobile m) => m_PlayersTable.TryGetValue(m, out SpecialMoveContext context) ? context : null;

    private class SpecialMoveTimer : Timer
    {
      private readonly Mobile m_Mobile;

      public SpecialMoveTimer(Mobile from) : base(TimeSpan.FromSeconds(3.0))
      {
        m_Mobile = from;

        Priority = TimerPriority.TwentyFiveMS;
      }

      protected override void OnTick()
      {
        RemoveContext(m_Mobile);
      }
    }

    public class SpecialMoveContext
    {
      public SpecialMoveContext(Timer timer, Type type)
      {
        Timer = timer;
        Type = type;
      }

      public Timer Timer { get; }

      public Type Type { get; }
    }
  }
}
