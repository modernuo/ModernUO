using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class WraithFormSpell : TransformationSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Wraith Form", "Rel Xen Um",
				203,
				9031,
				Reagent.NoxCrystal,
				Reagent.PigIron
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public override double RequiredSkill{ get{ return 20.0; } }
		public override int RequiredMana{ get{ return 17; } }

		public override int Body{ get{ return Caster.Female ? 747 : 748; } }
		public override int Hue{ get{ return Caster.Female ? 0 : 0x4001; } }

		public override int PhysResistOffset{ get{ return +15; } }
		public override int FireResistOffset{ get{ return -5; } }
		public override int ColdResistOffset{ get{ return  0; } }
		public override int PoisResistOffset{ get{ return  0; } }
		public override int NrgyResistOffset{ get{ return -5; } }

		public WraithFormSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void DoEffect( Mobile m )
		{
			m.PlaySound( 0x17F );
			m.FixedParticles( 0x374A, 1, 15, 9902, 1108, 4, EffectLayer.Waist );
		}
	}
}