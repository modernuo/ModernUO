namespace Server.Items
{
	public abstract class BaseWaterContainer : Container, IHasQuantity
	{
		public abstract int voidItem_ID { get; }
		public abstract int fullItem_ID { get; }
		public abstract int MaxQuantity { get; }

		public override int DefaultGumpID { get { return 0x3e; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual bool IsEmpty { get { return ( m_Quantity == 0 ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual bool IsFull { get { return ( m_Quantity >= MaxQuantity ); } }

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int currItemID { get { return ( IsEmpty ) ? voidItem_ID : fullItem_ID; } }

		[CommandProperty( AccessLevel.GameMaster )]
		public int Quantity
		{
			get
			{
				return m_Quantity;
			}
			set
			{
				if( ( value <= 0 && IsEmpty ) || ( value >= MaxQuantity && IsFull ) )
				{
					if( value != m_Quantity )
					{
						UpdateContainer( value );

						InvalidateProperties();
					}
				}
			}
		}

		private int m_Quantity;

		public BaseWaterContainer( int Item_Id, bool filled )
			: base( Item_Id )
		{
			m_Quantity = ( filled ) ? MaxQuantity : 0;
		}

		public override void OnDoubleClick( Mobile from )
		{
			if( IsEmpty )
			{
				base.OnDoubleClick( from );
			}
		}

		public override void OnSingleClick( Mobile from )
		{
			if( IsEmpty )
			{
				base.OnDoubleClick( from );
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			if( IsEmpty )
			{
				base.GetProperties( list );
			}
		}

		public override bool OnDragDropInto( Mobile from, Item item, Point3D p )
		{
			if( !IsEmpty )
			{
				return false;
			}

			return base.OnDragDropInto( from, item, p );
		}

		public virtual void UpdateContainer( int amount )
		{
			if( amount <= 0 )
			{
				m_Quantity = 0;
			}
			else
			{
				m_Quantity = ( MaxQuantity > amount ) ? amount : MaxQuantity;
			}
			if( !IsLockedDown )
			{
				Movable = IsEmpty;
			}
			ItemID = currItemID;
		}

		public BaseWaterContainer( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int)0 ); // version
			writer.Write( (int)m_Quantity );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
			m_Quantity = reader.ReadInt();
		}
	}
}