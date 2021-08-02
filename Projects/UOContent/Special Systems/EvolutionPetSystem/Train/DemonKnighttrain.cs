using System;
using System.Collections;
using Server;
using Server.Items;


namespace Server.Mobiles
{
	[CorpseName( "a demon knight corpse" )]
	public class DemonKnighttrain : BaseCreature
	{
		
        	

		[Constructible]
		public DemonKnighttrain() : base( AIType.AI_Mage, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			Name = NameList.RandomName( "demon knight" );
			Title = "the Dark Father";
			Body = 318;
			BaseSoundID = 0x165;

			SetStr( 1400 );
			SetDex( 100 );
			SetInt( 800 );

			SetHits( 22000 );
			SetMana( 1000 );

			SetDamage( 25, 31 );

			SetDamageType( ResistanceType.Physical, 20 );
			SetDamageType( ResistanceType.Fire, 20 );
			SetDamageType( ResistanceType.Cold, 20 );
			SetDamageType( ResistanceType.Poison, 20 );
			SetDamageType( ResistanceType.Energy, 20 );

			SetResistance( ResistanceType.Physical, 30 );
			SetResistance( ResistanceType.Fire, 30 );
			SetResistance( ResistanceType.Cold, 30 );
			SetResistance( ResistanceType.Poison, 30 );
			SetResistance( ResistanceType.Energy, 30 );

			

			SetSkill( SkillName.DetectHidden, 80.0 );
			SetSkill( SkillName.EvalInt, 100.0 );
			SetSkill( SkillName.Magery, 100.0 );
			SetSkill( SkillName.Meditation, 150.0 );
			SetSkill( SkillName.MagicResist, 100.0 );
			SetSkill( SkillName.Tactics, 100.0 );
			SetSkill( SkillName.Wrestling, 100.0 );

			Fame = 28000;
			Karma = -28000;

			VirtualArmor = 64;
		}

        public override void GenerateLoot()
        {
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.Gems, 8);
        }



        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (0.10 > Utility.RandomDouble())
            {
                switch (Utility.Random(24))
                {
                    case 0:
                    default: c.DropItem(new MJBofInvisibility()); break;
                    case 1: c.DropItem(new MJBofAgility()); break;
                    case 2: c.DropItem(new MJBofCunning()); break;
                    case 3: c.DropItem(new MJBofStrength()); break;
                    case 4: c.DropItem(new MJBofTeleport()); break;
                    case 5: c.DropItem(new MJBofBless()); break;
                    case 6: c.DropItem(new MJRofInvisibility()); break;
                    case 7: c.DropItem(new MJRofAgility()); break;
                    case 8: c.DropItem(new MJRofCunning()); break;
                    case 9: c.DropItem(new MJRofStrength()); break;
                    case 10: c.DropItem(new MJRofTeleport()); break;
                    case 11: c.DropItem(new MJRofBless()); break;
                    case 12: c.DropItem(new MJNofInvisibility()); break;
                    case 13: c.DropItem(new MJNofAgility()); break;
                    case 14: c.DropItem(new MJNofCunning()); break;
                    case 15: c.DropItem(new MJNofStrength()); break;
                    case 16: c.DropItem(new MJNofTeleport()); break;
                    case 17: c.DropItem(new MJNofBless()); break;
                    case 18: c.DropItem(new MJEofInvisibility()); break;
                    case 19: c.DropItem(new MJEofAgility()); break;
                    case 20: c.DropItem(new MJEofCunning()); break;
                    case 21: c.DropItem(new MJEofStrength()); break;
                    case 22: c.DropItem(new MJEofTeleport()); break;
                    case 23: c.DropItem(new MJEofBless()); break;
                }
            }
        }
		

		public DemonKnighttrain( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}
}
