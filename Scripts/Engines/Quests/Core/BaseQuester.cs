using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.ContextMenus;

namespace Server.Engines.Quests
{
	public class TalkEntry : ContextMenuEntry
	{
		private BaseQuester m_Quester;

		public TalkEntry( BaseQuester quester ) : base( quester.TalkNumber )
		{
			m_Quester = quester;
		}

		public override void OnClick()
		{
			Mobile from = Owner.From;

			if ( from.CheckAlive() && from is PlayerMobile && m_Quester.CanTalkTo( (PlayerMobile)from ) )
				m_Quester.OnTalk( (PlayerMobile)from, true );
		}
	}

	public abstract class BaseQuester : BaseVendor
	{
		protected ArrayList m_SBInfos = new ArrayList();
		protected override ArrayList SBInfos{ get { return m_SBInfos; } }

		public override bool IsActiveVendor{ get{ return false; } }
		public override bool IsInvulnerable{ get{ return true; } }
		public override bool DisallowAllMoves{ get{ return true; } }
		public override bool ClickTitle{ get { return false; } }
		public override bool CanTeach{ get{ return false; } }

		public virtual int TalkNumber{ get{ return 6146; } } // Talk

		public override void InitSBInfo()
		{
		}

		public BaseQuester() : this( null )
		{
		}

		public BaseQuester( string title ) : base( title )
		{
		}

		public BaseQuester( Serial serial ) : base( serial )
		{
		}

		public abstract void OnTalk( PlayerMobile player, bool contextMenu );

		public virtual bool CanTalkTo( PlayerMobile to )
		{
			return true;
		}

		public virtual int GetAutoTalkRange( PlayerMobile m )
		{
			return -1;
		}

		public override bool CanBeDamaged()
		{
			return false;
		}

		protected Item SetHue( Item item, int hue )
		{
			item.Hue = hue;
			return item;
		}

		public override void AddCustomContextEntries( Mobile from, List<ContextMenuEntry> list )
		{
			base.AddCustomContextEntries( from, list );

			if ( from.Alive && from is PlayerMobile && TalkNumber > 0 && CanTalkTo( (PlayerMobile)from ) )
				list.Add( new TalkEntry( this ) );
		}

		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m.Alive && m is PlayerMobile )
			{
				PlayerMobile pm = (PlayerMobile)m;

				int range = GetAutoTalkRange( pm );

				if ( m.Alive && range >= 0 && InRange( m, range ) && !InRange( oldLocation, range ) && CanTalkTo( pm ) )
					OnTalk( pm, false );
			}
		}

		public void FocusTo( Mobile to )
		{
			QuestSystem.FocusTo( this, to );
		}

		public static Container GetNewContainer()
		{
			Bag bag = new Bag();
			bag.Hue = QuestSystem.RandomBrightHue();
			return bag;
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