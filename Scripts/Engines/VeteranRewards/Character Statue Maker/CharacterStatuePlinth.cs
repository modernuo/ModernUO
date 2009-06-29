using System;
using System.Globalization;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Targets;

namespace Server.Items
{	
	public class CharacterStatuePlinth : Static, IAddon
	{		
		public Item Deed{ get{ return new CharacterStatueDeed( m_Statue ); } }
		public override int LabelNumber{ get{ return 1076201; } } // Character Statue
	
		private CharacterStatue m_Statue;
		
		public CharacterStatuePlinth( CharacterStatue statue ) : base( 0x32F2 )
		{
			m_Statue = statue;
			
			InvalidateHue();
		}

		public CharacterStatuePlinth( Serial serial ) : base( serial )
		{
		}    
		
		public override void OnAfterDelete()
		{
			base.OnAfterDelete();
			
			if ( m_Statue != null && !m_Statue.Deleted )
				m_Statue.Delete();
		}
		
		public override void OnMapChange()
		{			
			if ( m_Statue != null )
				m_Statue.Map = Map;
		}
		
		public override void OnLocationChange( Point3D oldLocation )
		{			
			if ( m_Statue != null )
				m_Statue.Location = new Point3D( X, Y, Z + 5 );
		}
		
		public override void OnDoubleClick( Mobile from )
		{
			if ( m_Statue != null )
				from.SendGump( new CharacterPlinthGump( m_Statue ) );			
		}		

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( (int) 0 ); // version
			
			writer.Write( (Mobile) m_Statue );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
			
			m_Statue = reader.ReadMobile() as CharacterStatue;
		}
		
		public void InvalidateHue()
		{
			if ( m_Statue != null )
				Hue = 0xB8F + (int) m_Statue.StatueType * 4 + (int) m_Statue.Material;
		}
		
		public virtual bool CouldFit( IPoint3D p, Map map )
		{
			Point3D point = new Point3D( p.X, p.Y, p.Z );
			
			if ( map == null || !map.CanFit( point, 20 ) )
				return false;
				
			BaseHouse house = BaseHouse.FindHouseAt( point, map, 20 );
			
			if ( house == null )
				return false;
			
			AddonFitResult result = CharacterStatueTarget.CheckDoors( point, 20, house );
			
			if ( result == AddonFitResult.Valid )
				return true;
				
			return false;
		}
		
		private class CharacterPlinthGump : Gump
		{	
			public CharacterPlinthGump( CharacterStatue statue ) : base( 60, 30 )
			{						
				Closable = true;
				Disposable = true;
				Dragable = true;
				Resizable = false;
			
				AddPage( 0 );			
				AddImage( 0, 0, 0x24F4 );
				AddHtml( 55, 50, 150, 20, statue.Name, false, false );
				AddHtml( 55, 75, 150, 20, statue.SculptedOn.ToString( "G", new CultureInfo("de-DE") ), false, false );
				AddHtmlLocalized( 55, 100, 150, 20, GetTypeNumber( statue.StatueType ), 0, false, false );
			}
			
			public int GetTypeNumber( StatueType type )
			{
				switch ( type )
				{
					case StatueType.Marble: return 1076181;
					case StatueType.Jade: return 1076180;
					case StatueType.Bronze: return 1076230;
					default: return 1076181;
				}
			}
		}
	}
}
