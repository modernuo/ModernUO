using System;
using Server;
using Server.Items;

namespace Server.Factions
{
	public class FactionNecromancer : BaseFactionGuard
	{
		public override GuardAI GuardAI{ get{ return GuardAI.Magic | GuardAI.Smart | GuardAI.Bless | GuardAI.Curse; } }

		[Constructable]
		public FactionNecromancer() : base( "the necromancer" )
		{
			GenerateBody( false, false );
			Hue = 1;

			SetStr( 151, 175 );
			SetDex( 61, 85 );
			SetInt( 151, 175 );

			SetResistance( ResistanceType.Physical, 40, 60 );
			SetResistance( ResistanceType.Fire, 40, 60 );
			SetResistance( ResistanceType.Cold, 40, 60 );
			SetResistance( ResistanceType.Energy, 40, 60 );
			SetResistance( ResistanceType.Poison, 40, 60 );

			VirtualArmor = 32;

			SetSkill( SkillName.Macing, 110.0, 120.0 );
			SetSkill( SkillName.Wrestling, 110.0, 120.0 );
			SetSkill( SkillName.Tactics, 110.0, 120.0 );
			SetSkill( SkillName.MagicResist, 110.0, 120.0 );
			SetSkill( SkillName.Healing, 110.0, 120.0 );
			SetSkill( SkillName.Anatomy, 110.0, 120.0 );

			SetSkill( SkillName.Magery, 110.0, 120.0 );
			SetSkill( SkillName.EvalInt, 110.0, 120.0 );
			SetSkill( SkillName.Meditation, 110.0, 120.0 );

			Item shroud = new Item( 0x204E );
			shroud.Layer = Layer.OuterTorso;

			AddItem( Immovable( Rehued( shroud, 1109 ) ) );
			AddItem( Newbied( Rehued( new GnarledStaff(), 2211 ) ) );

			PackItem( new Bandage( Utility.RandomMinMax( 30, 40 ) ) );
			PackStrongPotions( 6, 12 );
		}

		public FactionNecromancer( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}