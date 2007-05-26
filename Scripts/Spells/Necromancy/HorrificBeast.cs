using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class HorrificBeastSpell : TransformationSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Horrific Beast", "Rel Xen Vas Bal",
				203,
				9031,
				Reagent.BatWing,
				Reagent.DaemonBlood
			);

		public override TimeSpan CastDelayBase { get { return TimeSpan.FromSeconds( 2.0 ); } }

		public override double RequiredSkill{ get{ return 40.0; } }
		public override int RequiredMana{ get{ return 11; } }

		public override int Body{ get{ return 746; } }

		public HorrificBeastSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void DoEffect( Mobile m )
		{
			m.PlaySound( 0x165 );
			m.FixedParticles( 0x3728, 1, 13, 9918, 92, 3, EffectLayer.Head );

			m.Delta( MobileDelta.WeaponDamage );
		}

		public override void RemoveEffect( Mobile m )
		{
			m.Delta( MobileDelta.WeaponDamage );
		}
	}
}