using System;
using System.Collections;
using Server.Items;
using Server.Targeting;
using Server.Misc;

namespace Server.Mobiles
{
	[CorpseName("a pestilent bandage corpse")]
	public class PestilentBandage : BaseCreature
	{
		// Neither Stratics nor UOGuide have much description 
		// beyond being a "Grey Mummy". BodyValue, Sound and 
		// Hue are all guessed until they can be verified.
		// Loot and Fame/Karma are also guesses at this point.
		//
		// They also apparently have a Poison Attack, which I've stolen from Yamandons.
		[Constructable]
		public PestilentBandage() : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 ) // NEED TO CHECK
		{
			Name = "a pestilent bandage";
			Body = 154;
			Hue = 0x515; 
			BaseSoundID = 471; 

			SetStr( 691, 740 );
			SetDex( 141, 180 );
			SetInt( 51, 80 );

			SetHits( 415, 445 );

			SetDamage( 13, 23 );

			SetDamageType( ResistanceType.Physical, 40 );
			SetDamageType( ResistanceType.Cold, 20 );
			SetDamageType( ResistanceType.Poison, 40 );

			SetResistance( ResistanceType.Physical, 45, 55 );
			SetResistance( ResistanceType.Fire, 10, 20 );
			SetResistance( ResistanceType.Cold, 50, 60 );
			SetResistance( ResistanceType.Poison, 20, 30 );
			SetResistance( ResistanceType.Energy, 20, 30 );

			SetSkill( SkillName.Poisoning, 0.0, 10.0 );
			SetSkill( SkillName.Anatomy, 0 );
			SetSkill( SkillName.MagicResist, 75.0, 80.0 );
			SetSkill( SkillName.Tactics, 80.0, 85.0 );
			SetSkill( SkillName.Wrestling, 70.0, 75.0 );

			Fame = 20000;
			Karma = -20000;

			// VirtualArmor = 28; // Don't know what it should be

			PackItem( new Bandage( 5 ) );  // How many?

		}

		public override void OnDamagedBySpell(Mobile attacker)
		{
			base.OnDamagedBySpell(attacker);

			DoCounter(attacker);
		}

		public override void OnGotMeleeAttack(Mobile attacker)
		{
			base.OnGotMeleeAttack(attacker);

			DoCounter(attacker);
		}

		private void DoCounter(Mobile attacker)
		{
			if (this.Map == null)
				return;

			if (attacker is BaseCreature && ((BaseCreature)attacker).BardProvoked)
				return;

			if (0.2 > Utility.RandomDouble())
			{
				/* Counterattack with Hit Poison Area
				 * 20-25 damage, unresistable
				 * Lethal poison, 100% of the time
				 * Particle effect: Type: "2" From: "0x4061A107" To: "0x0" ItemId: "0x36BD" ItemIdName: "explosion" FromLocation: "(296 615, 17)" ToLocation: "(296 615, 17)" Speed: "1" Duration: "10" FixedDirection: "True" Explode: "False" Hue: "0xA6" RenderMode: "0x0" Effect: "0x1F78" ExplodeEffect: "0x1" ExplodeSound: "0x0" Serial: "0x4061A107" Layer: "255" Unknown: "0x0"
				 * Doesn't work on provoked monsters
				 */

				Mobile target = null;

				if (attacker is BaseCreature)
				{
					Mobile m = ((BaseCreature)attacker).GetMaster();

					if (m != null)
						target = m;
				}

				if (target == null || !target.InRange(this, 18))
					target = attacker;

				this.Animate(10, 4, 1, true, false, 0);

				ArrayList targets = new ArrayList();

				foreach (Mobile m in target.GetMobilesInRange(8))
				{
					if (m == this || !CanBeHarmful(m))
						continue;

					if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned || ((BaseCreature)m).Team != this.Team))
						targets.Add(m);
					else if (m.Player && m.Alive)
						targets.Add(m);
				}

				for (int i = 0; i < targets.Count; ++i)
				{
					Mobile m = (Mobile)targets[i];

					DoHarmful(m);

					AOS.Damage(m, this, Utility.RandomMinMax(20, 25), true, 0, 0, 0, 100, 0);

					m.FixedParticles(0x36BD, 1, 10, 0x1F78, 0xA6, 0, (EffectLayer)255);
					m.ApplyPoison(this, Poison.Lethal);
				}
			}
		}
		public override bool CanHeal { get { return true; } }
		public override void GenerateLoot()
		{
			AddLoot( LootPack.Rich );  // Need to verify
		}

		public PestilentBandage( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
