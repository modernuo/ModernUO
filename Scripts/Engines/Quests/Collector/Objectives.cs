using System;
using Server;
using Server.Mobiles;

namespace Server.Engines.Quests.Collector
{
	public class FishPearlsObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Fish up shellfish from Lake Haven and collect rainbow pearls.
				return 1055084;
			}
		}

		public override int MaxProgress{ get{ return 6; } }

		public FishPearlsObjective()
		{
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				// Rainbow pearls collected:
				gump.AddHtmlObject( 70, 260, 270, 100, 1055085, BaseQuestGump.Blue, false, false );

				gump.AddLabel( 70, 280, 0x64, CurProgress.ToString() );
				gump.AddLabel( 100, 280, 0x64, "/" );
				gump.AddLabel( 130, 280, 0x64, MaxProgress.ToString() );
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnPearlsObjective() );
		}
	}

	public class ReturnPearlsObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You've collected enough rainbow pearls. Speak to
				 * Elwood to give them to him and get your next task.
				 */
				return 1055088;
			}
		}

		public ReturnPearlsObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ReturnPearlsConversation() );
		}
	}

	public class FindAlbertaObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Go to Vesper and speak to Alberta Giacco at the Colored Canvas.
				return 1055091;
			}
		}

		public FindAlbertaObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new AlbertaPaintingConversation() );
		}
	}

	public class SitOnTheStoolObjective : QuestObjective
	{
		private static readonly Point3D m_StoolLocation = new Point3D( 2899, 706, 0 );
		private static readonly Map m_StoolMap = Map.Trammel;

		private DateTime m_Begin;

		public override object Message
		{
			get
			{
				/* Sit on the stool in front of Alberta's easel so that she can
				 * paint your portrait. You'll need to sit there for about 30 seconds.
				 */
				return 1055093;
			}
		}

		public SitOnTheStoolObjective()
		{
			m_Begin = DateTime.MaxValue;
		}

		public override void CheckProgress()
		{
			PlayerMobile pm = System.From;

			if ( pm.Map == m_StoolMap && pm.Location == m_StoolLocation )
			{
				if ( m_Begin == DateTime.MaxValue )
				{
					m_Begin = DateTime.Now;
				}
				else if ( DateTime.Now - m_Begin > TimeSpan.FromSeconds( 30.0 ) )
				{
					Complete();
				}
			}
			else if ( m_Begin != DateTime.MaxValue )
			{
				m_Begin = DateTime.MaxValue;
				pm.SendLocalizedMessage( 1055095, "", 0x26 ); // You must remain seated on the stool until the portrait is complete. Alberta will now have to start again with a fresh canvas.
			}
		}

		public override void OnComplete()
		{
			System.AddConversation( new AlbertaEndPaintingConversation() );
		}
	}

	public class ReturnPaintingObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Elwood and let him know that the painting is complete.
				return 1055099;
			}
		}

		public ReturnPaintingObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ReturnPaintingConversation() );
		}
	}

	public class FindGabrielObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* Go to Britain and obtain the autograph of renowned
				 * minstrel, Gabriel Piete. He is often found at the Conservatory of Music.
				 */
				return 1055101;
			}
		}

		public FindGabrielObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new GabrielAutographConversation() );
		}
	}

	public enum Theater
	{
		Britain,
		Nujelm,
		Jhelom
	}

	public class FindSheetMusicObjective : QuestObjective
	{
		private Theater m_Theater;

		public override object Message
		{
			get
			{
				/* Find some sheet music for one of Gabriel's songs.
				 * Try speaking to an impresario from one of the theaters in the land.
				 */
				return 1055104;
			}
		}

		public FindSheetMusicObjective( bool init )
		{
			if ( init )
				InitTheater();
		}

		public FindSheetMusicObjective()
		{
		}

		public void InitTheater()
		{
			switch ( Utility.Random( 3 ) )
			{
				case 1: m_Theater = Theater.Britain; break;
				case 2: m_Theater = Theater.Nujelm; break;
				default: m_Theater = Theater.Jhelom; break;
			}
		}

		public bool IsInRightTheater()
		{
			PlayerMobile player = System.From;

			Region region = Region.Find( player.Location, player.Map );

			if ( region == null )
				return false;

			switch ( m_Theater )
			{
				case Theater.Britain: return region.IsPartOf( "Britain" );
				case Theater.Nujelm: return region.IsPartOf( "Nujel'm" );
				case Theater.Jhelom: return region.IsPartOf( "Jhelom" );

				default: return false;
			}
		}

		public override void OnComplete()
		{
			System.AddConversation( new GetSheetMusicConversation() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			m_Theater = (Theater) reader.ReadEncodedInt();
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( (int) m_Theater );
		}
	}

	public class ReturnSheetMusicObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Speak to Gabriel to have him autograph the sheet music.
				return 1055110;
			}
		}

		public ReturnSheetMusicObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new GabrielSheetMusicConversation() );
		}
	}

	public class ReturnAutographObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Speak to Elwood to give him the autographed sheet music.
				return 1055114;
			}
		}

		public ReturnAutographObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ReturnAutographConversation() );
		}
	}

	public class FindTomasObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Go to Trinsic and speak to Tomas O'Neerlan, the famous toymaker.
				return 1055117;
			}
		}

		public FindTomasObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new TomasToysConversation() );
		}
	}

	public enum CaptureResponse
	{
		Valid,
		AlreadyDone,
		Invalid
	}

	public class CaptureImagesObjective : QuestObjective
	{
		private ImageType[] m_Images;
		private bool[] m_Done;

		public override object Message
		{
			get
			{
				// Use the enchanted paints to capture the image of all of the creatures listed below.
				return 1055120;
			}
		}

		public override bool Completed
		{
			get
			{
				for ( int i = 0; i < m_Done.Length; i++ )
				{
					if ( !m_Done[i] )
						return false;
				}

				return true;
			}
		}

		public CaptureImagesObjective( bool init )
		{
			if ( init )
			{
				m_Images = ImageTypeInfo.RandomList( 4 );
				m_Done = new bool[4];
			}
		}

		public CaptureImagesObjective()
		{
		}

		public override bool IgnoreYoungProtection( Mobile from )
		{
			if ( Completed )
				return false;

			Type fromType = from.GetType();

			for ( int i = 0; i < m_Images.Length; i++ )
			{
				ImageTypeInfo info = ImageTypeInfo.Get( m_Images[i] );

				if ( info.Type == fromType )
					return true;
			}

			return false;
		}

		public CaptureResponse CaptureImage( Type type, out ImageType image )
		{
			for ( int i = 0; i < m_Images.Length; i++ )
			{
				ImageTypeInfo info = ImageTypeInfo.Get( m_Images[i] );

				if ( info.Type == type )
				{
					image = m_Images[i];

					if ( m_Done[i] )
					{
						return CaptureResponse.AlreadyDone;
					}
					else
					{
						m_Done[i] = true;

						CheckCompletionStatus();

						return CaptureResponse.Valid;
					}
				}
			}

			image = (ImageType)0;
			return CaptureResponse.Invalid;
		}

		public override void RenderProgress( BaseQuestGump gump )
		{
			if ( !Completed )
			{
				for ( int i = 0; i < m_Images.Length; i++ )
				{
					ImageTypeInfo info = ImageTypeInfo.Get( m_Images[i] );

					gump.AddHtmlObject( 70, 260 + 20 * i, 200, 100, info.Name, BaseQuestGump.Blue, false, false );
					gump.AddLabel( 200, 260 + 20 * i, 0x64, " : " );
					gump.AddHtmlObject( 220, 260 + 20 * i, 100, 100, m_Done[i] ? 1055121 : 1055122, BaseQuestGump.Blue, false, false );
				}
			}
			else
			{
				base.RenderProgress( gump );
			}
		}

		public override void OnComplete()
		{
			System.AddObjective( new ReturnImagesObjective() );
		}

		public override void ChildDeserialize( GenericReader reader )
		{
			int version = reader.ReadEncodedInt();

			int count = reader.ReadEncodedInt();

			m_Images = new ImageType[count];
			m_Done = new bool[count];

			for ( int i = 0; i < count; i++ )
			{
				m_Images[i] = (ImageType) reader.ReadEncodedInt();
				m_Done[i] = reader.ReadBool();
			}
		}

		public override void ChildSerialize( GenericWriter writer )
		{
			writer.WriteEncodedInt( (int) 0 ); // version

			writer.WriteEncodedInt( (int) m_Images.Length );

			for ( int i = 0; i < m_Images.Length; i++ )
			{
				writer.WriteEncodedInt( (int) m_Images[i] );
				writer.Write( (bool) m_Done[i] );
			}
		}
	}

	public class ReturnImagesObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				/* You now have all of the creature images you need.
				 * Return to Tomas O'Neerlan so that he can make the toy figurines.
				 */
				return 1055128;
			}
		}

		public ReturnImagesObjective()
		{
		}

		public override void OnComplete()
		{
			System.AddConversation( new ReturnImagesConversation() );
		}
	}

	public class ReturnToysObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Elwood with news that the toy figurines will be delivered when ready.
				return 1055132;
			}
		}

		public ReturnToysObjective()
		{
		}
	}

	public class MakeRoomObjective : QuestObjective
	{
		public override object Message
		{
			get
			{
				// Return to Elwood for your reward when you have some room in your backpack.
				return 1055136;
			}
		}

		public MakeRoomObjective()
		{
		}
	}
}