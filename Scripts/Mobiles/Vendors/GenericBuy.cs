using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
	public class GenericBuyInfo : IBuyItemInfo
	{
		private class DisplayCache : Container
		{
			private static DisplayCache m_Cache;

			public static DisplayCache Cache {
				get {
					if ( m_Cache == null || m_Cache.Deleted )
						m_Cache = new DisplayCache();

					return m_Cache;
				}
			}

			private Dictionary<Type, IEntity> m_Table;
			private List<Mobile> m_Mobiles;

			public DisplayCache() : base( 0 )
			{
				m_Table = new Dictionary<Type, IEntity>();
				m_Mobiles = new List<Mobile>();
			}

			public IEntity Lookup( Type key )
			{
				m_Table.TryGetValue( key, out IEntity e );
				return e;
			}

			public void Store( Type key, IEntity obj, bool cache )
			{
				if ( cache )
					m_Table[key] = obj;

				if ( obj is Item )
					AddItem( (Item)obj );
				else if ( obj is Mobile )
					m_Mobiles.Add( (Mobile)obj );
			}

			public DisplayCache( Serial serial ) : base( serial )
			{
			}

			public override void OnAfterDelete()
			{
				base.OnAfterDelete();

				for ( int i = 0; i < m_Mobiles.Count; ++i )
					m_Mobiles[i].Delete();

				m_Mobiles.Clear();

				for ( int i = Items.Count - 1; i >= 0; --i )
					if ( i < Items.Count )
						Items[i].Delete();

				if ( m_Cache == this )
					m_Cache = null;
			}

			public override void Serialize( GenericWriter writer )
			{
				base.Serialize( writer );

				writer.Write( (int) 0 ); // version

				writer.Write( m_Mobiles );
			}

			public override void Deserialize( GenericReader reader )
			{
				base.Deserialize( reader );

				int version = reader.ReadInt();

				m_Mobiles = reader.ReadStrongMobileList();

				for ( int i = 0; i < m_Mobiles.Count; ++i )
					m_Mobiles[i].Delete();

				m_Mobiles.Clear();

				for ( int i = Items.Count - 1; i >= 0; --i )
					if ( i < Items.Count )
						Items[i].Delete();

				if ( m_Cache == null )
					m_Cache = this;
				else
					Delete();

				m_Table = new Dictionary<Type, IEntity>();
			}
		}

		private int m_Price;
		private int m_Amount;
		private IEntity m_DisplayEntity;

		public virtual int ControlSlots => 0;

		public virtual bool CanCacheDisplay //return ( m_Args == null || m_Args.Length == 0 ); }
			=> false;

		private bool IsDeleted( IEntity obj )
		{
			return obj.Deleted;
		}

		public void DeleteDisplayEntity()
		{
			if ( m_DisplayEntity == null )
				return;

			m_DisplayEntity.Delete();
			m_DisplayEntity = null;
		}

		public IEntity GetDisplayEntity()
		{
			if ( m_DisplayEntity != null && !IsDeleted( m_DisplayEntity ) )
				return m_DisplayEntity;

			bool canCache = CanCacheDisplay;

			if ( canCache )
				m_DisplayEntity = DisplayCache.Cache.Lookup( Type );

			if ( m_DisplayEntity == null || IsDeleted( m_DisplayEntity ) )
				m_DisplayEntity = GetEntity();

			DisplayCache.Cache.Store( Type, m_DisplayEntity, canCache );

			return m_DisplayEntity;
		}

		public Type Type { get; set; }

		public string Name { get; set; }

		public int DefaultPrice { get; private set; }

		public int PriceScalar
		{
			get => DefaultPrice;
			set => DefaultPrice = value;
		}

		public int Price
		{
			get
			{
				if ( DefaultPrice != 0 )
				{
					if ( m_Price > 5000000 )
					{
						long price = m_Price;

						price *= DefaultPrice;
						price += 50;
						price /= 100;

						if ( price > int.MaxValue )
							price = int.MaxValue;

						return (int)price;
					}

					return ( ((m_Price * DefaultPrice) + 50) / 100 );
				}

				return m_Price;
			}
			set => m_Price = value;
		}

		public int ItemID { get; set; }

		public int Hue { get; set; }

		public int Amount
		{
			get => m_Amount;
			set{ if ( value < 0 ) value = 0; m_Amount = value; }
		}

		public int MaxAmount { get; set; }

		public object[] Args { get; set; }

		public GenericBuyInfo( Type type, int price, int amount, int itemID, int hue ) : this( null, type, price, amount, itemID, hue, null )
		{
		}

		public GenericBuyInfo( string name, Type type, int price, int amount, int itemID, int hue ) : this( name, type, price, amount, itemID, hue, null )
		{
		}

		public GenericBuyInfo( Type type, int price, int amount, int itemID, int hue, object[] args ) : this( null, type, price, amount, itemID, hue, args )
		{
		}

		public GenericBuyInfo( string name, Type type, int price, int amount, int itemID, int hue, object[] args )
		{
			Type = type;
			m_Price = price;
			MaxAmount = m_Amount = amount;
			ItemID = itemID;
			Hue = hue;
			Args = args;

			if ( name == null )
				Name = itemID < 0x4000 ? (1020000 + itemID).ToString() : (1078872 + itemID).ToString();
			else
				Name = name;
		}

		//get a new instance of an object (we just bought it)
		public virtual IEntity GetEntity()
		{
			if ( Args == null || Args.Length == 0 )
				return (IEntity)Activator.CreateInstance( Type );

			return (IEntity)Activator.CreateInstance( Type, Args );
			//return (Item)Activator.CreateInstance( m_Type );
		}

		//Attempt to restock with item, (return true if restock successful)
		public bool Restock( Item item, int amount )
		{
			return false;
			/*if ( item.GetType() == m_Type )
			{
				if ( item is BaseWeapon )
				{
					BaseWeapon weapon = (BaseWeapon)item;

					if ( weapon.Quality == WeaponQuality.Low || weapon.Quality == WeaponQuality.Exceptional || (int)weapon.DurabilityLevel > 0 || (int)weapon.DamageLevel > 0 || (int)weapon.AccuracyLevel > 0 )
						return false;
				}

				if ( item is BaseArmor )
				{
					BaseArmor armor = (BaseArmor)item;

					if ( armor.Quality == ArmorQuality.Low || armor.Quality == ArmorQuality.Exceptional || (int)armor.Durability > 0 || (int)armor.ProtectionLevel > 0 )
						return false;
				}

				m_Amount += amount;

				return true;
			}
			else
			{
				return false;
			}*/
		}

		public void OnRestock()
		{
			if ( m_Amount <= 0 )
			{
				/*
					Core.ML using this vendor system is undefined behavior, so being
					as it lends itself to an abusable exploit to cause ingame havok
					and the stackable items are not found to be over 20 items, this is
					changed until there is a better solution.
				*/

				object Obj_Disp = GetDisplayEntity();

				if ( Core.ML && Obj_Disp is Item && !( Obj_Disp as Item ).Stackable )
				{
					MaxAmount = Math.Min( 20, MaxAmount );
				}
				else
				{
					MaxAmount = Math.Min( 999, MaxAmount * 2 );
				}
			}
			else
			{
				/* NOTE: According to UO.com, the quantity is halved if the item does not reach 0
				 * Here we implement differently: the quantity is halved only if less than half
				 * of the maximum quantity was bought. That is, if more than half is sold, then
				 * there's clearly a demand and we should not cut down on the stock.
				 */

				int halfQuantity = MaxAmount;

				if ( halfQuantity >= 999 )
					halfQuantity = 640;
				else if ( halfQuantity > 20 )
					halfQuantity /= 2;

				if ( m_Amount >= halfQuantity )
					MaxAmount = halfQuantity;
			}

			m_Amount = MaxAmount;
		}
	}
}
