using System;
using Server.Network;
using Server.Items;
using Server.Engines.Craft;

namespace Server.Items
{
	public abstract class LockableContainer : TrapableContainer, ILockable, ILockpickable, ICraftable, IShipwreckedItem
	{
		private bool m_Locked;
		private int m_LockLevel, m_MaxLockLevel, m_RequiredSkill;
		private uint m_KeyValue;
		private Mobile m_Picker;
		private bool m_TrapOnLockpick;
		private CraftQuality m_Quality;
		private Mobile m_Crafter;
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Picker
		{
			get
			{
				return m_Picker;
			}
			set
			{
				m_Picker = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int MaxLockLevel
		{
			get
			{
				return m_MaxLockLevel;
			}
			set
			{
				m_MaxLockLevel = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int LockLevel
		{
			get
			{
				return m_LockLevel;
			}
			set
			{
				m_LockLevel = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int RequiredSkill
		{
			get
			{
				return m_RequiredSkill;
			}
			set
			{
				m_RequiredSkill = value;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual bool Locked
		{
			get
			{
				return m_Locked;
			}
			set
			{
				m_Locked = value;

				if ( m_Locked )
					m_Picker = null;

				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public uint KeyValue
		{
			get
			{
				return m_KeyValue;
			}
			set
			{
				m_KeyValue = value;
			}
		}

		public override bool TrapOnOpen
		{
			get
			{
				return !m_TrapOnLockpick;
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public bool TrapOnLockpick
		{
			get
			{
				return m_TrapOnLockpick;
			}
			set
			{
				m_TrapOnLockpick = value;
			}
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CraftQuality Quality
		{
			get{ return m_Quality; }
			set{ m_Quality = value; InvalidateProperties(); }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Crafter
		{
			get{ return m_Crafter; }
			set{ m_Crafter = value; InvalidateProperties(); }
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get{ return m_Resource; }
			set{ m_Resource = value; Hue = CraftResources.GetHue( m_Resource ); InvalidateProperties(); }
		}
		
		public virtual CraftResource DefaultResource{ get{ return CraftResource.RegularWood; } }

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 7 ); // version
			
			writer.Write( (int)m_Quality );
			writer.Write( (int)m_Resource );
			writer.WriteMobile( m_Crafter );

			writer.Write( m_IsShipwreckedItem );

			writer.Write( (bool) m_TrapOnLockpick );

			writer.Write( (int) m_RequiredSkill );

			writer.Write( (int) m_MaxLockLevel );

			writer.Write( m_KeyValue );
			writer.Write( (int) m_LockLevel );
			writer.Write( (bool) m_Locked );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			switch ( version )
			{
				case 7:
				{
					m_Quality = (CraftQuality)reader.ReadInt();
					m_Resource = (CraftResource)reader.ReadInt();
					m_Crafter = reader.ReadMobile();
					goto case 6;
				}
				case 6:
				{
					m_IsShipwreckedItem = reader.ReadBool();

					goto case 5;
				}
				case 5:
				{
					m_TrapOnLockpick = reader.ReadBool();

					goto case 4;
				}
				case 4:
				{
					m_RequiredSkill = reader.ReadInt();

					goto case 3;
				}
				case 3:
				{
					m_MaxLockLevel = reader.ReadInt();

					goto case 2;
				}
				case 2:
				{
					m_KeyValue = reader.ReadUInt();

					goto case 1;
				}
				case 1:
				{
					m_LockLevel = reader.ReadInt();

					goto case 0;
				}
				case 0:
				{
					if ( version < 3 )
						m_MaxLockLevel = 100;

					if ( version < 4 )
					{
						if ( (m_MaxLockLevel - m_LockLevel) == 40 )
						{
							m_RequiredSkill = m_LockLevel + 6;
							m_LockLevel = m_RequiredSkill - 10;
							m_MaxLockLevel = m_RequiredSkill + 39;
						}
						else
						{
							m_RequiredSkill = m_LockLevel;
						}
					}
					
					if ( version < 7 )
					{
						m_Quality = CraftQuality.Regular;
						m_Resource = DefaultResource;
					}

					m_Locked = reader.ReadBool();

					break;
				}
			}
		}

		public LockableContainer( int itemID ) : base( itemID )
		{
			m_Quality = CraftQuality.Regular;
			Resource = DefaultResource;
			m_MaxLockLevel = 100;
		}

		public LockableContainer( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );
			
			if ( m_Crafter != null )
				list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

			if ( m_Quality == CraftQuality.Exceptional )
				list.Add( 1060636 ); // exceptional
			
			if( ( m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood ) && Hue == CraftResources.GetHue( m_Resource ) )
				list.Add( CraftResources.GetLocalizationNumber( m_Resource ) ); // resource name
		}

		public override bool CheckContentDisplay( Mobile from )
		{
			return !m_Locked && base.CheckContentDisplay( from );
		}

		public override bool TryDropItem( Mobile from, Item dropped, bool sendFullMessage )
		{
			if ( from.AccessLevel < AccessLevel.GameMaster && m_Locked )
			{
				from.SendLocalizedMessage( 501747 ); // It appears to be locked.
				return false;
			}

			return base.TryDropItem( from, dropped, sendFullMessage );
		}

		public override bool OnDragDropInto( Mobile from, Item item, Point3D p )
		{
			if ( from.AccessLevel < AccessLevel.GameMaster && m_Locked )
			{
				from.SendLocalizedMessage( 501747 ); // It appears to be locked.
				return false;
			}

			return base.OnDragDropInto( from, item, p );
		}

		public override bool CheckLift( Mobile from, Item item, ref LRReason reject )
		{
			if ( !base.CheckLift( from, item, ref reject ) )
				return false;

			if ( item != this && from.AccessLevel < AccessLevel.GameMaster && m_Locked )
				return false;

			return true;
		}

		public override bool CheckItemUse( Mobile from, Item item )
		{
			if ( !base.CheckItemUse( from, item ) )
				return false;

			if ( item != this && from.AccessLevel < AccessLevel.GameMaster && m_Locked )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return false;
			}

			return true;
		}

		public override bool DisplaysContent{ get{ return !m_Locked; } }

		public virtual bool CheckLocked( Mobile from )
		{
			bool inaccessible = false;

			if ( m_Locked )
			{
				int number;

				if ( from.AccessLevel >= AccessLevel.GameMaster )
				{
					number = 502502; // That is locked, but you open it with your godly powers.
				}
				else
				{
					number = 501747; // It appears to be locked.
					inaccessible = true;
				}

				from.Send( new MessageLocalized( Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", "" ) );
			}

			return inaccessible;
		}

		public override void OnTelekinesis( Mobile from )
		{
			if ( CheckLocked( from ) )
			{
				Effects.SendLocationParticles( EffectItem.Create( Location, Map, EffectItem.DefaultDuration ), 0x376A, 9, 32, 5022 );
				Effects.PlaySound( Location, Map, 0x1F5 );
				return;
			}

			base.OnTelekinesis( from );
		}

		public override void OnDoubleClickSecureTrade( Mobile from )
		{
			if ( CheckLocked( from ) )
				return;

			base.OnDoubleClickSecureTrade( from );
		}

		public override void Open( Mobile from )
		{
			if ( CheckLocked( from ) )
				return;

			base.Open( from );
		}

		public override void OnSnoop( Mobile from )
		{
			if ( CheckLocked( from ) )
				return;

			base.OnSnoop( from );
		}

		public virtual void LockPick( Mobile from )
		{
			Locked = false;
			Picker = from;

			if ( this.TrapOnLockpick && ExecuteTrap( from ) )
			{
				this.TrapOnLockpick = false;
			}
		}

		public override void AddNameProperties( ObjectPropertyList list )
		{
			base.AddNameProperties( list );

			if ( m_IsShipwreckedItem )
				list.Add( 1041645 ); // recovered from a shipwreck
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			if ( m_IsShipwreckedItem )
				LabelTo( from, 1041645 );	//recovered from a shipwreck
		}

		#region ICraftable Members

		public int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			if ( Core.ML )
			{
				Quality = (CraftQuality)quality;
			
				Type resourceType = typeRes;

				if ( resourceType == null )
					resourceType = craftItem.Resources.GetAt( 0 ).ItemType;
			
				Resource = CraftResources.GetFromType( resourceType );
			
				CraftContext context = craftSystem.GetContext( from );

				if ( context != null && context.DoNotColor )
					Resource = DefaultResource;
				
				if ( makersMark )
					Crafter = from;
			}
			
			if ( from.CheckSkill( SkillName.Tinkering, -5.0, 15.0 ) )
			{
				from.SendLocalizedMessage( 500636 ); // Your tinker skill was sufficient to make the item lockable.

				Key key = new Key( KeyType.Copper, Key.RandomValue() );

				KeyValue = key.KeyValue;
				DropItem( key );

				double tinkering = from.Skills[SkillName.Tinkering].Value;
				int level = (int)(tinkering * 0.8);

				RequiredSkill = level - 4;
				LockLevel = level - 14;
				MaxLockLevel = level + 35;

				if ( LockLevel == 0 )
					LockLevel = -1;
				else if ( LockLevel > 95 )
					LockLevel = 95;

				if ( RequiredSkill > 95 )
					RequiredSkill = 95;

				if ( MaxLockLevel > 95 )
					MaxLockLevel = 95;
			}
			else
			{
				from.SendLocalizedMessage( 500637 ); // Your tinker skill was insufficient to make the item lockable.
			}

			return quality;
		}

		#endregion

		#region IShipwreckedItem Members

		private bool m_IsShipwreckedItem;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool IsShipwreckedItem
		{
			get { return m_IsShipwreckedItem; }
			set { m_IsShipwreckedItem = value; }
		}
		#endregion

	}
}