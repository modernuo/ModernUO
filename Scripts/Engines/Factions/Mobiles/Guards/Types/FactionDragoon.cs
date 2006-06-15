using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Factions
{
	public class FactionDragoon : BaseFactionGuard
	{
		public override GuardAI GuardAI{ get{ return GuardAI.Magic | GuardAI.Melee | GuardAI.Smart | GuardAI.Bless | GuardAI.Curse; } }

		[Constructable]
		public FactionDragoon() : base( "the dragoon" )
		{
			GenerateBody( false, false );

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

			AddItem( Immovable( Rehued( new Cloak(), 1645 ) ) );

			AddItem( Immovable( Rehued( new PlateChest(), 1645 ) ) );
			AddItem( Immovable( Rehued( new PlateLegs(), 1109 ) ) );
			AddItem( Immovable( Rehued( new PlateArms(), 1109 ) ) );
			AddItem( Immovable( Rehued( new PlateGloves(), 1109 ) ) );
			AddItem( Immovable( Rehued( new PlateGorget(), 1109 ) ) );
			AddItem( Immovable( Rehued( new PlateHelm(), 1109 ) ) );

			AddItem( Newbied( new WarHammer() ) );

			AddItem( Immovable( Rehued( new VirtualMountItem( this ), 1109 ) ) );

			PackItem( new Bandage( Utility.RandomMinMax( 30, 40 ) ) );
			PackStrongPotions( 6, 12 );
		}

		public FactionDragoon( Serial serial ) : base( serial )
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