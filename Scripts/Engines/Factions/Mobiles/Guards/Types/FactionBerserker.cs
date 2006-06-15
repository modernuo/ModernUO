using System;
using Server;
using Server.Items;

namespace Server.Factions
{
	public class FactionBerserker : BaseFactionGuard
	{
		public override GuardAI GuardAI{ get{ return GuardAI.Melee | GuardAI.Curse | GuardAI.Bless; } }

		[Constructable]
		public FactionBerserker() : base( "the berserker" )
		{
			GenerateBody( false, false );

			SetStr( 126, 150 );
			SetDex( 61, 85 );
			SetInt( 81, 95 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 30, 50 );
			SetResistance( ResistanceType.Fire, 30, 50 );
			SetResistance( ResistanceType.Cold, 30, 50 );
			SetResistance( ResistanceType.Energy, 30, 50 );
			SetResistance( ResistanceType.Poison, 30, 50 );

			VirtualArmor = 24;

			SetSkill( SkillName.Swords, 100.0, 110.0 );
			SetSkill( SkillName.Wrestling, 100.0, 110.0 );
			SetSkill( SkillName.Tactics, 100.0, 110.0 );
			SetSkill( SkillName.MagicResist, 100.0, 110.0 );
			SetSkill( SkillName.Healing, 100.0, 110.0 );
			SetSkill( SkillName.Anatomy, 100.0, 110.0 );

			SetSkill( SkillName.Magery, 100.0, 110.0 );
			SetSkill( SkillName.EvalInt, 100.0, 110.0 );
			SetSkill( SkillName.Meditation, 100.0, 110.0 );

			AddItem( Immovable( Rehued( new BodySash(), 1645 ) ) );
			AddItem( Immovable( Rehued( new Kilt(), 1645 ) ) );
			AddItem( Immovable( Rehued( new Sandals(), 1645 ) ) );
			AddItem( Newbied( new DoubleAxe() ) );

			HairItemID = 0x2047; // Afro
			HairHue = 0x29;

			FacialHairItemID = 0x204B; // Medium Short Beard
			FacialHairHue = 0x29;

			PackItem( new Bandage( Utility.RandomMinMax( 30, 40 ) ) );
			PackStrongPotions( 6, 12 );
		}

		public FactionBerserker( Serial serial ) : base( serial )
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