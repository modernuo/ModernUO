using System;
using System.Collections;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;

namespace Server.Spells.Spellweaving
{
	public abstract class ArcaneForm : ArcanistSpell, ITransformationSpell
	{
		public abstract int Body { get; }
		public virtual int Hue  => 0;

		public virtual int PhysResistOffset  => 0;
		public virtual int FireResistOffset  => 0;
		public virtual int ColdResistOffset  => 0;
		public virtual int PoisResistOffset  => 0;
		public virtual int NrgyResistOffset  => 0;

		public ArcaneForm( Mobile caster, Item scroll, SpellInfo info )	: base( caster, scroll, info )
		{
		}

		public override bool CheckCast()
		{
			if ( !TransformationSpellHelper.CheckCast( Caster, this ) )
				return false;

			return base.CheckCast();
		}

		public override void OnCast()
		{
			TransformationSpellHelper.OnCast( Caster, this );

			FinishSequence();
		}

		public virtual double TickRate
		{
			get { return 1.0; }
		}

		public virtual void OnTick( Mobile m )
		{
		}

		public virtual void DoEffect( Mobile m )
		{
		}

		public virtual void RemoveEffect( Mobile m )
		{
		}
	}
}
