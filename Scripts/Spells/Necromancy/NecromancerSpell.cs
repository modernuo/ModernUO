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

		//public override int CastDelayBase{ get{ return base.CastDelayBase; } } // Reference, 3

		public override bool ClearHandsOnCast{ get{ return false; } }

		public override double CastDelayFastScalar{ get{ return (Core.SE? base.CastDelayFastScalar : 0); } } // Necromancer spells are not affected by fast cast items, though they are by fast cast recovery

		public NecromancerSpell( Mobile caster, Item scroll, SpellInfo info ) : base( caster, scroll, info )
		{
		}

		public override int ComputeKarmaAward()
		{
			//TODO: Verify this formula being that Necro spells don't HAVE a circle.

			//return -(70 + (10 * (int)Circle));

			return -(40 + (int)(10 * (CastDelayBase.TotalSeconds / CastDelaySecondsPerTick)));
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