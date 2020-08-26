using System.Collections.Generic;

namespace Server.Spells.Fifth
{
  public class MagicReflectSpell : MagerySpell
  {
    private static readonly SpellInfo m_Info = new SpellInfo(
      "Magic Reflection", "In Jux Sanct",
      242,
      9012,
      Reagent.Garlic,
      Reagent.MandrakeRoot,
      Reagent.SpidersSilk);

    private static readonly Dictionary<Mobile, ResistanceMod[]> m_Table = new Dictionary<Mobile, ResistanceMod[]>();

    public MagicReflectSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Fifth;

    public override bool CheckCast()
    {
      if (Core.AOS)
        return true;

      if (Caster.MagicDamageAbsorb > 0)
      {
        Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
        return false;
      }

      if (!Caster.CanBeginAction<DefensiveSpell>())
      {
        Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
        return false;
      }

      return true;
    }

    public override void OnCast()
    {
      if (Core.AOS)
      {
        /* The magic reflection spell decreases the caster's physical resistance, while increasing the caster's elemental resistances.
         * Physical decrease = 25 - (Inscription/20).
         * Elemental resistance = +10 (-20 physical, +10 elemental at GM Inscription)
         * The magic reflection spell has an indefinite duration, becoming active when cast, and deactivated when re-cast.
         * Reactive Armor, Protection, and Magic Reflection will stay on�even after logging out, even after dying�until you �turn them off� by casting them again.
         */

        if (CheckSequence())
        {
          Mobile targ = Caster;

          if (m_Table.TryGetValue(targ, out ResistanceMod[] mods))
          {
            targ.PlaySound(0x1ED);
            targ.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

            m_Table.Remove(targ);

            for (int i = 0; i < mods.Length; ++i)
              targ.RemoveResistanceMod(mods[i]);

            BuffInfo.RemoveBuff(targ, BuffIcon.MagicReflection);
          }
          else
          {
            targ.PlaySound(0x1E9);
            targ.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);

            int physiMod = -25 + (int)(targ.Skills.Inscribe.Value / 20);
            int otherMod = 10;

            mods = new[]
            {
              new ResistanceMod(ResistanceType.Physical, physiMod),
              new ResistanceMod(ResistanceType.Fire, otherMod),
              new ResistanceMod(ResistanceType.Cold, otherMod),
              new ResistanceMod(ResistanceType.Poison, otherMod),
              new ResistanceMod(ResistanceType.Energy, otherMod)
            };

            m_Table[targ] = mods;

            for (int i = 0; i < mods.Length; ++i)
              targ.AddResistanceMod(mods[i]);

            string buffFormat = $"{physiMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}\t+{otherMod}";

            BuffInfo.AddBuff(targ, new BuffInfo(BuffIcon.MagicReflection, 1075817, buffFormat, true));
          }
        }

        FinishSequence();
      }
      else
      {
        if (Caster.MagicDamageAbsorb > 0)
        {
          Caster.SendLocalizedMessage(1005559); // This spell is already in effect.
        }
        else if (!Caster.CanBeginAction<DefensiveSpell>())
        {
          Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
        }
        else if (CheckSequence())
        {
          if (Caster.BeginAction<DefensiveSpell>())
          {
            int value = (int)(Caster.Skills.Magery.Value + Caster.Skills.Inscribe.Value);
            value = (int)(8 + value / 200.0 * 7.0); // absorb from 8 to 15 "circles"

            Caster.MagicDamageAbsorb = value;

            Caster.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
            Caster.PlaySound(0x1E9);
          }
          else
          {
            Caster.SendLocalizedMessage(1005385); // The spell will not adhere to you at this time.
          }
        }

        FinishSequence();
      }
    }

    public static void EndReflect(Mobile m)
    {
      if (!m_Table.TryGetValue(m, out ResistanceMod[] mods))
        return;

      for (int i = 0; i < mods?.Length; ++i)
        m.RemoveResistanceMod(mods[i]);

      m_Table.Remove(m);
      BuffInfo.RemoveBuff(m, BuffIcon.MagicReflection);
    }
  }
}
