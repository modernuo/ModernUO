using System;
using System.Collections;
using Server.Network;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.Necromancy
{
	public class LichFormSpell : TransformationSpell
	{
		private static SpellInfo m_Info = new SpellInfo(
				"Lich Form", "Rel Xen Corp Ort",
				203,
				9031,
				Reagent.GraveDust,
				Reagent.DaemonBlood,
				Reagent.NoxCrystal
			);

		public override TimeSpan CastDelayBase => TimeSpan.FromSeconds( 2.0 );

		public override double RequiredSkill => 70.0;
		public override int RequiredMana => 23;

		public override int Body => 749;

		public override int FireResistOffset => -10;
		public override int ColdResistOffset => +10;
		public override int PoisResistOffset => +10;

		public override double TickRate => 2.5;

		public LichFormSpell( Mobile caster, Item scroll ) : base( caster, scroll, m_Info )
		{
		}

		public override void DoEffect( Mobile m )
		{
			m.PlaySound( 0x19C );
			m.FixedParticles( 0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot );
		}

		public override void OnTick( Mobile m )
		{
			--m.Hits;
		}
	}
}
