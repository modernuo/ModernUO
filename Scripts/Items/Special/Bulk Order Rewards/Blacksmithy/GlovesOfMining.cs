using System;

namespace Server.Items
{
	[FlipableAttribute( 0x13c6, 0x13ce )]
	public class LeatherGlovesOfMining : BaseGlovesOfMining
	{
		public override int BasePhysicalResistance{ get{ return 2; } }
		public override int BaseFireResistance{ get{ return 4; } }
		public override int BaseColdResistance{ get{ return 3; } }
		public override int BasePoisonResistance{ get{ return 3; } }
		public override int BaseEnergyResistance{ get{ return 3; } }

		public override int InitMinHits{ get{ return 30; } }
		public override int InitMaxHits{ get{ return 40; } }

		public override int AosStrReq{ get{ return 20; } }
		public override int OldStrReq{ get{ return 10; } }

		public override int ArmorBase{ get{ return 13; } }

		public override ArmorMaterialType MaterialType{ get{ return ArmorMaterialType.Leather; } }
		public override CraftResource DefaultResource{ get{ return CraftResource.RegularLeather; } }

		public override ArmorMeditationAllowance DefMedAllowance{ get{ return ArmorMeditationAllowance.All; } }

		public override int LabelNumber{ get{ return 1045122; } } // leather blacksmith gloves of mining

		[Constructable]
		public LeatherGlovesOfMining( int bonus ) : base( bonus, 0x13C6 )
		{
			Weight = 1;
		}

		public LeatherGlovesOfMining( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	[FlipableAttribute( 0x13d5, 0x13dd )]
	public class StuddedGlovesOfMining : BaseGlovesOfMining
	{
		public override int BasePhysicalResistance{ get{ return 2; } }
		public override int BaseFireResistance{ get{ return 4; } }
		public override int BaseColdResistance{ get{ return 3; } }
		public override int BasePoisonResistance{ get{ return 3; } }
		public override int BaseEnergyResistance{ get{ return 4; } }

		public override int InitMinHits{ get{ return 35; } }
		public override int InitMaxHits{ get{ return 45; } }

		public override int AosStrReq{ get{ return 25; } }
		public override int OldStrReq{ get{ return 25; } }

		public override int ArmorBase{ get{ return 16; } }

		public override ArmorMaterialType MaterialType{ get{ return ArmorMaterialType.Studded; } }
		public override CraftResource DefaultResource{ get{ return CraftResource.RegularLeather; } }

		public override int LabelNumber{ get{ return 1045123; } } // studded leather blacksmith gloves of mining

		[Constructable]
		public StuddedGlovesOfMining( int bonus ) : base( bonus, 0x13D5 )
		{
			Weight = 2;
		}

		public StuddedGlovesOfMining( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	[FlipableAttribute( 0x13eb, 0x13f2 )]
	public class RingmailGlovesOfMining : BaseGlovesOfMining
	{
		public override int BasePhysicalResistance{ get{ return 3; } }
		public override int BaseFireResistance{ get{ return 3; } }
		public override int BaseColdResistance{ get{ return 1; } }
		public override int BasePoisonResistance{ get{ return 5; } }
		public override int BaseEnergyResistance{ get{ return 3; } }

		public override int InitMinHits{ get{ return 40; } }
		public override int InitMaxHits{ get{ return 50; } }

		public override int AosStrReq{ get{ return 40; } }
		public override int OldStrReq{ get{ return 20; } }

		public override int OldDexBonus{ get{ return -1; } }

		public override int ArmorBase{ get{ return 22; } }

		public override ArmorMaterialType MaterialType{ get{ return ArmorMaterialType.Ringmail; } }

		public override int LabelNumber{ get{ return 1045124; } } // ringmail blacksmith gloves of mining

		[Constructable]
		public RingmailGlovesOfMining( int bonus ) : base( bonus, 0x13EB )
		{
			Weight = 1;
		}

		public RingmailGlovesOfMining( Serial serial ) : base( serial )
		{
		}
		
		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int) 0 );
		}
		
		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();
		}
	}

	public abstract class BaseGlovesOfMining : BaseArmor
	{
		private int m_Bonus;
		private SkillMod m_SkillMod;

		[CommandProperty( AccessLevel.GameMaster )]
		public int Bonus
		{
			get
			{
				return m_Bonus;
			}
			set
			{
				m_Bonus = value;
				InvalidateProperties();

				if ( m_Bonus == 0 )
				{
					if ( m_SkillMod != null )
						m_SkillMod.Remove();

					m_SkillMod = null;
				}
				else if ( m_SkillMod == null && Parent is Mobile )
				{
					m_SkillMod = new DefaultSkillMod( SkillName.Mining, true, m_Bonus );
					((Mobile)Parent).AddSkillMod( m_SkillMod );
				}
				else if ( m_SkillMod != null )
				{
					m_SkillMod.Value = m_Bonus;
				}
			}
		}

		public override void OnAdded( object parent )
		{
			base.OnAdded( parent );

			if ( m_Bonus != 0 && parent is Mobile )
			{
				if ( m_SkillMod != null )
					m_SkillMod.Remove();

				m_SkillMod = new DefaultSkillMod( SkillName.Mining, true, m_Bonus );
				((Mobile)parent).AddSkillMod( m_SkillMod );
			}
		}

		public override void OnRemoved( object parent )
		{
			base.OnRemoved( parent );

			if ( m_SkillMod != null )
				m_SkillMod.Remove();

			m_SkillMod = null;
		}

		public BaseGlovesOfMining( int bonus, int itemID ) : base( itemID )
		{
			m_Bonus = bonus;

			this.Hue = CraftResources.GetHue( (CraftResource)Utility.RandomMinMax( (int)CraftResource.DullCopper, (int)CraftResource.Valorite ) );
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Bonus != 0 )
				list.Add( 1062005, m_Bonus.ToString() ); // mining bonus +~1_val~
		}

		public BaseGlovesOfMining( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (int) m_Bonus );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Bonus = reader.ReadInt();
					break;
				}
			}

			if ( m_Bonus != 0 && Parent is Mobile )
			{
				if ( m_SkillMod != null )
					m_SkillMod.Remove();

				m_SkillMod = new DefaultSkillMod( SkillName.Mining, true, m_Bonus );
				((Mobile)Parent).AddSkillMod( m_SkillMod );
			}
		}
	}
}