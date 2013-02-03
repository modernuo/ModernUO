using System;
using System.Collections.Generic;
using Server;
using Server.Engines.MLQuests.Mobiles;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server.Engines.MLQuests.Gumps
{
	public interface IRaceChanger
	{
		bool CheckComplete( PlayerMobile from );
		void ConsumeNeeded( PlayerMobile from );
		void OnCancel( PlayerMobile from );
	}

	public class RaceChangeConfirmGump : Gump
	{
		public static readonly Type Type = typeof( RaceChangeConfirmGump );

		private IRaceChanger m_Owner;
		private PlayerMobile m_From;
		private Race m_Race;

		public RaceChangeConfirmGump( IRaceChanger owner, PlayerMobile from, Race targetRace )
			: base( 50, 50 )
		{
			from.CloseGump( Type );

			m_Owner = owner;
			m_From = from;
			m_Race = targetRace;

			AddPage( 0 );
			AddBackground( 0, 0, 240, 135, 0x2422 );

			if ( targetRace == Race.Human )
				AddHtmlLocalized( 15, 15, 210, 75, 1073643, 0, false, false ); // Are you sure you wish to embrace your humanity?
			else if ( targetRace == Race.Elf )
				AddHtmlLocalized( 15, 15, 210, 75, 1073642, 0, false, false ); // Are you sure you want to follow the elven ways?
			else
				AddHtml( 15, 15, 210, 75, String.Format( "Are you sure you want to change your race to {0}?", targetRace.Name ), false, false );

			AddButton( 160, 95, 0xF7, 0xF8, 1, GumpButtonType.Reply, 0 );
			AddButton( 90, 95, 0xF2, 0xF1, 0, GumpButtonType.Reply, 0 );
		}

		public override void OnResponse( NetState sender, RelayInfo info )
		{
			switch ( info.ButtonID )
			{
				case 0: // Cancel
				{
					if ( m_Owner != null )
						m_Owner.OnCancel( m_From );

					break;
				}
				case 1: // Okay
				{
					if ( m_Owner == null || m_Owner.CheckComplete( m_From ) )
						Offer( m_Owner, m_From, m_Race );

					break;
				}
			}
		}

		private class RaceChangeState
		{
			private static readonly TimeSpan m_TimeoutDelay = TimeSpan.FromMinutes( 1 );
			private static readonly TimerStateCallback<NetState> m_TimeoutCallback = new TimerStateCallback<NetState>( Timeout );

			public IRaceChanger m_Owner;
			public Race m_TargetRace;
			public Timer m_Timeout;

			public RaceChangeState( IRaceChanger owner, NetState ns, Race targetRace )
			{
				m_Owner = owner;
				m_TargetRace = targetRace;
				m_Timeout = Timer.DelayCall<NetState>( m_TimeoutDelay, m_TimeoutCallback, ns );
			}
		}

		private static Dictionary<NetState, RaceChangeState> m_Pending;

		public static void Initialize()
		{
			m_Pending = new Dictionary<NetState, RaceChangeState>();

			PacketHandlers.RegisterExtended( 0x2A, true, new OnPacketReceive( RaceChangeReply ) );
		}

		public static bool IsPending( NetState state )
		{
			return ( state != null && m_Pending.ContainsKey( state ) );
		}

		private static void Offer( IRaceChanger owner, PlayerMobile from, Race targetRace )
		{
			NetState ns = from.NetState;

			if ( ns == null || !CanChange( from, targetRace ) )
				return;

			CloseCurrent( ns );

			m_Pending[ns] = new RaceChangeState( owner, ns, targetRace );
			ns.Send( new RaceChanger( from.Female, targetRace ) );
		}

		private static void CloseCurrent( NetState ns )
		{
			RaceChangeState state;

			if ( m_Pending.TryGetValue( ns, out state ) )
			{
				state.m_Timeout.Stop();
				m_Pending.Remove( ns );
			}

			ns.Send( CloseRaceChanger.Instance );
		}

		private static void Timeout( NetState ns )
		{
			if ( m_Pending.ContainsKey( ns ) )
			{
				m_Pending.Remove( ns );
				ns.Send( CloseRaceChanger.Instance );
			}
		}

		public static bool IsWearingEquipment( Mobile from )
		{
			foreach ( Item item in from.Items )
			{
				switch ( item.Layer )
				{
					case Layer.Hair:
					case Layer.FacialHair:
					case Layer.Backpack:
					case Layer.Mount:
					case Layer.Bank:
					{
						continue; // ignore
					}
					default:
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool CanChange( PlayerMobile from, Race targetRace )
		{
			if ( from.Deleted )
				return false;

			if ( from.Race == targetRace )
				from.SendLocalizedMessage( 1111918 ); // You are already that race.
			else if ( !MondainsLegacy.CheckML( from, false ) )
				from.SendLocalizedMessage( 1073651 ); // You must have Mondain's Legacy before proceeding...
			else if ( !from.Alive )
				from.SendLocalizedMessage( 1073646 ); // Only the living may proceed...
			else if ( from.Mounted )
				from.SendLocalizedMessage( 1073647 ); // You may not continue while mounted...
			else if ( !from.CanBeginAction( typeof( PolymorphSpell ) ) || DisguiseTimers.IsDisguised( from ) || AnimalForm.UnderTransformation( from ) || !from.CanBeginAction( typeof( IncognitoSpell ) ) || from.IsBodyMod ) // TODO: Does this cover everything?
				from.SendLocalizedMessage( 1073648 ); // You may only proceed while in your original state...
			else if ( from.Spell != null && from.Spell.IsCasting )
				from.SendLocalizedMessage( 1073649 ); // One may not proceed while embracing magic...
			else if ( from.Poisoned )
				from.SendLocalizedMessage( 1073652 ); // You must be healthy to proceed...
			else if ( IsWearingEquipment( from ) )
				from.SendLocalizedMessage( 1073650 ); // To proceed you must be unburdened by equipment...
			else
				return true;

			return false;
		}

		private static void RaceChangeReply( NetState state, PacketReader pvSrc )
		{
			RaceChangeState raceChangeState;

			if ( !m_Pending.TryGetValue( state, out raceChangeState ) )
				return;

			CloseCurrent( state );

			PlayerMobile pm = state.Mobile as PlayerMobile;

			if ( pm == null )
				return;

			IRaceChanger owner = raceChangeState.m_Owner;
			Race targetRace = raceChangeState.m_TargetRace;

			if ( pvSrc.Size == 5 )
			{
				if ( owner != null )
					owner.OnCancel( pm );

				return;
			}

			if ( !CanChange( pm, targetRace ) || ( owner != null && !owner.CheckComplete( pm ) ) )
				return;

			int hue = pvSrc.ReadUInt16();
			int hairItemId = pvSrc.ReadUInt16();
			int hairHue = pvSrc.ReadUInt16();
			int facialHairItemId = pvSrc.ReadUInt16();
			int facialHairHue = pvSrc.ReadUInt16();

			pm.Race = targetRace;
			pm.Hue = targetRace.ClipSkinHue( hue ) | 0x8000;

			if ( targetRace.ValidateHair( pm, hairItemId ) )
			{
				pm.HairItemID = hairItemId;
				pm.HairHue = targetRace.ClipHairHue( hairHue );
			}
			else
			{
				pm.HairItemID = 0;
			}

			if ( targetRace.ValidateFacialHair( pm, facialHairItemId ) )
			{
				pm.FacialHairItemID = facialHairItemId;
				pm.FacialHairHue = targetRace.ClipHairHue( facialHairHue );
			}
			else
			{
				pm.FacialHairItemID = 0;
			}

			if ( targetRace == Race.Human )
				pm.SendLocalizedMessage( 1073654 ); // You are now fully human.
			else if ( targetRace == Race.Elf )
				pm.SendLocalizedMessage( 1073653 ); // You are now fully initiated into the Elven culture.
			else
				pm.SendMessage( "You have fully changed your race to {0}.", targetRace.Name );

			if ( owner != null )
				owner.ConsumeNeeded( pm );
		}
	}

	public sealed class RaceChanger : Packet
	{
		public RaceChanger( bool female, Race targetRace )
			: base( 0xBF )
		{
			EnsureCapacity( 7 );

			m_Stream.Write( (short)0x2A );
			m_Stream.Write( (byte)( female ? 1 : 0 ) );
			m_Stream.Write( (byte)( targetRace.RaceID + 1 ) );
		}
	}

	public sealed class CloseRaceChanger : Packet
	{
		public static readonly Packet Instance = Packet.SetStatic( new CloseRaceChanger() );

		private CloseRaceChanger()
			: base( 0xBF )
		{
			EnsureCapacity( 7 );

			m_Stream.Write( (short)0x2A );
			m_Stream.Write( (byte)0 );
			m_Stream.Write( (byte)0xFF );
		}
	}

	#region For testing

	public class RaceChangeDeed : Item, IRaceChanger
	{
		public override string DefaultName { get { return "a race change deed"; } }

		[Constructable]
		public RaceChangeDeed()
			: base( 0x14F0 )
		{
			LootType = LootType.Blessed;
		}

		public bool CheckComplete( PlayerMobile pm )
		{
			if ( Deleted )
				return false;

			if ( !IsChildOf( pm.Backpack ) )
			{
				pm.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
				return false;
			}

			return true;
		}

		public void ConsumeNeeded( PlayerMobile pm )
		{
			Consume();
		}

		public void OnCancel( PlayerMobile pm )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			PlayerMobile pm = from as PlayerMobile;

			if ( pm == null )
				return;

			if ( CheckComplete( pm ) )
				pm.SendGump( new RaceChangeConfirmGump( this, pm, ( pm.Race == Race.Human ) ? Race.Elf : Race.Human ) );
		}

		public RaceChangeDeed( Serial serial )
			: base( serial )
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

	#endregion
}
