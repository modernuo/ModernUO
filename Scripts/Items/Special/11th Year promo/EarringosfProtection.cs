using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Items
{
	public class EarringosfProtection : BaseJewel
	{
		[CommandProperty( AccessLevel.GameMaster )]
		public virtual AosElementAttribute Attribute
		{
			get
			{
				return m_Attribute;
			}
		}

		public override int LabelNumber
		{
			get
			{
				return GetItemData( m_Attribute, true );
			}
		}

		public override int Hue
		{
			get
			{
				return GetItemData( m_Attribute, false );
			}
		}

		private AosElementAttribute m_Attribute;
		private LootType m_LootType;

		[Constructable]
		public EarringosfProtection()
			: this( RandomType() )
		{
		}

		[Constructable]
		public EarringosfProtection( AosElementAttribute element )
			: base( 0x1087, Layer.Earrings )
		{
			Resistances[ ( (AosElementAttribute)element ) ] = 2;

			m_Attribute = element;
			LootType = LootType.Blessed;
		}

		public EarringosfProtection( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
			writer.Write( (int)m_Attribute );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			m_Attribute = (AosElementAttribute)reader.ReadInt();
		}

		public static AosElementAttribute RandomType()
		{
			return GetTypes( Utility.Random( 5 ) );
		}

		public static AosElementAttribute GetTypes( int value )
		{
			switch( value )
			{
				case 0:  return AosElementAttribute.Physical;
				case 1:  return AosElementAttribute.Fire;
				case 2:  return AosElementAttribute.Cold;
				case 3:  return AosElementAttribute.Poison;
				default: return AosElementAttribute.Energy;
			}
		}

		public static int GetItemData( AosElementAttribute element, bool label )
		{
			switch( element )
			{
				case AosElementAttribute.Physical: return ( label ) ? 1071091 : 0;         // Earring of Protection (Physical)  1071091
				case AosElementAttribute.Fire:     return ( label ) ? 1071092 : 0x4ec;     // Earring of Protection (Fire)      1071092
				case AosElementAttribute.Cold:     return ( label ) ? 1071093 : 0x4f2;     // Earring of Protection (Cold)      1071093
				case AosElementAttribute.Poison:   return ( label ) ? 1071094 : 0x4f8;     // Earring of Protection (Poison)    1071094
				case AosElementAttribute.Energy:   return ( label ) ? 1071095 : 0x4fe;     // Earring of Protection (Energy)    1071095

				default: return -1;
			}
		}
	}

	public class EarringBoxSet : RedVelvetGiftBox
	{
		[Constructable]
		public EarringBoxSet()
			: base()
		{
			DropItem( new EarringosfProtection( AosElementAttribute.Physical ) );
			DropItem( new EarringosfProtection( AosElementAttribute.Fire ) );
			DropItem( new EarringosfProtection( AosElementAttribute.Cold ) );
			DropItem( new EarringosfProtection( AosElementAttribute.Poison ) );
			DropItem( new EarringosfProtection( AosElementAttribute.Energy ) );
		}

		public EarringBoxSet( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}