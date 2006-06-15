using System;
using Server;

namespace Server.Spells.Necromancy
{
	public abstract class NecromancerSpell : Spell
	{
		public abstract double RequiredSkill{ get; }
		public abstract int RequiredMana{ get; }

		public override SkillName CastSkill{ get{ return SkillName.Necromancy; } }
		public override SkillName DamageSkill{ get{ return SkillName.SpiritSpeak; } }

		public override bool ClearHandsOnCast{ get{ return false; } }

		public override int CastDelayFastScalar{ get{ return 0; } } // Necromancer spells are not effected by fast cast items, though they are by fast cast recovery

		public NecromancerSpell( Mobile caster, Item scroll, SpellInfo info ) : base( caster, scroll, info )
		{
		}

		public override int ComputeKarmaAward()
		{
			return -(70 + (10 * (int)Circle));
		}

		public override void GetCastSkills( out double min, out double max )
		{
			min = RequiredSkill;
			max = RequiredSkill + 40.0;
		}

		public override int GetMana()
		{
			return RequiredMana;
		}
	}
}