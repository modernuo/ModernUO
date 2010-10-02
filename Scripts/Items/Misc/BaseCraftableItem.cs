using System;
using System.Collections;
using System.Collections.Generic;
using Server.Network;
using Server.Engines.Craft;
using Server.Misc;

namespace Server.Items
{
	public enum CraftQuality
	{
		Low,
		Regular,
		Exceptional
	}
	
	public abstract class BaseCraftableItem : Item, ICraftable
	{
		private Mobile m_Crafter;
		private CraftQuality m_Quality;
		private CraftResource m_Resource;

		[CommandProperty( AccessLevel.GameMaster )]
		public Mobile Crafter
		{
			get { return m_Crafter; } 
			set { m_Crafter = value; InvalidateProperties(); } 
		}
		
		[CommandProperty( AccessLevel.GameMaster )]
		public CraftQuality Quality
		{ 
			get { return m_Quality; } 
			set { m_Quality = value; InvalidateProperties(); } 
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public CraftResource Resource
		{
			get { return m_Resource; }
			set
			{
				if ( m_Resource != value )
				{
					m_Resource = value;
					Hue = CraftResources.GetHue( m_Resource );
					InvalidateProperties();
				}
			}
		}
		
		#region Old Item Serialization Vars
		/* DO NOT USE! Only used in serialization of furniture that originally derived from Item */
		private bool m_InheritsItem;
		private int m_OldVersion;
		
		protected bool InheritsItem
		{ 
			get{ return m_InheritsItem; } 
		}
		
		protected int OldVersion
		{
			get{ return m_OldVersion; }
		}
		#endregion
		
		public virtual CraftResource DefaultResource{ get{ return CraftResource.RegularWood; } }
		public virtual bool DisplaysResource{ get{ return true; } }
		//For odd entries that can be crafted exceptionally with maker's mark but don't actually display a maker's mark
		public virtual bool DisplaysMakersMark{ get{ return true; } } 
		
		public BaseCraftableItem( int itemID ) : base( itemID )
		{
			m_Quality = CraftQuality.Regular;
			Resource = DefaultResource;
		}

		public BaseCraftableItem(Serial serial) : base(serial)
		{
		}
		
		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Crafter != null && DisplaysMakersMark )
				list.Add( 1050043, m_Crafter.Name ); // crafted by ~1_NAME~

			if ( m_Quality == CraftQuality.Exceptional )
				list.Add( 1060636 ); // exceptional

			if( ( m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood ) && Hue == CraftResources.GetHue( m_Resource ) && DisplaysResource )
				list.Add( CraftResources.GetLocalizationNumber( m_Resource ) ); // resource name
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int) 1);
			
			writer.Write( m_Crafter );
			writer.WriteEncodedInt( (int) m_Quality );
			writer.WriteEncodedInt( (int) m_Resource );
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			
			int version = reader.ReadInt();
			
			switch ( version )
			{
				case 1: 
				{
					m_Crafter = reader.ReadMobile();
					m_Quality = (CraftQuality)reader.ReadEncodedInt();
					m_Resource = (CraftResource)reader.ReadEncodedInt();
					break;
				}
				case 0: 
				{
					m_InheritsItem = true;
					m_OldVersion = version;
					m_Quality = CraftQuality.Regular;
					m_Resource = DefaultResource;
					break;
				}
			}
		}
		
		public virtual int OnCraft( int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue )
		{
			if ( Core.ML )
			{
				Quality = (CraftQuality)quality;
			
				if ( makersMark )
					Crafter = from;

				Type resourceType = typeRes;

				if ( resourceType == null )
					resourceType = craftItem.Resources.GetAt( 0 ).ItemType;

				Resource = CraftResources.GetFromType( resourceType );

				CraftContext context = craftSystem.GetContext( from );

				if ( context != null && context.DoNotColor )
					Hue = 0;
			}
			
			return (int)Quality;
		}
	}
}
