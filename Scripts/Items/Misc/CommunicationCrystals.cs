using System;
using System.Collections;
using Server;
using Server.Network;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{
	public class CrystalRechargeInfo
	{
		public static readonly CrystalRechargeInfo[] Table = new CrystalRechargeInfo[]
			{
				new CrystalRechargeInfo( typeof( Citrine ), 500 ),
				new CrystalRechargeInfo( typeof( Amber ), 500 ),
				new CrystalRechargeInfo( typeof( Tourmaline ), 750 ),
				new CrystalRechargeInfo( typeof( Emerald ), 1000 ),
				new CrystalRechargeInfo( typeof( Sapphire ), 1000 ),
				new CrystalRechargeInfo( typeof( Amethyst ), 1000 ),
				new CrystalRechargeInfo( typeof( StarSapphire ), 1250 ),
				new CrystalRechargeInfo( typeof( Diamond ), 2000 )
			};

		public static CrystalRechargeInfo Get( Type type )
		{
			foreach ( CrystalRechargeInfo info in Table )
			{
				if ( info.Type == type )
					return info;
			}

			return null;
		}

		private Type m_Type;
		private int m_Amount;

		public Type Type{ get{ return m_Type; } }
		public int Amount{ get{ return m_Amount; } }

		private CrystalRechargeInfo( Type type, int amount )
		{
			m_Type = type;
			m_Amount = amount;
		}
	}

	public class BroadcastCrystal : Item
	{
		public static readonly int MaxCharges = 2000;

		public override int LabelNumber{ get{ return 1060740; } } // communication crystal

		private int m_Charges;
		private List<ReceiverCrystal> m_Receivers;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get{ return this.ItemID == 0x1ECD; }
			set
			{
				this.ItemID = value ? 0x1ECD : 0x1ED0;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public int Charges
		{
			get{ return m_Charges; }
			set
			{
				m_Charges = value;
				InvalidateProperties();
			}
		}

		public List<ReceiverCrystal> Receivers
		{
			get{ return m_Receivers; }
		}

		[Constructable]
		public BroadcastCrystal() : this( 2000 )
		{
		}

		[Constructable]
		public BroadcastCrystal( int charges ) : base( 0x1ED0 )
		{
			Light = LightType.Circle150;

			m_Charges = charges;

			m_Receivers = new List<ReceiverCrystal>();
		}

		public BroadcastCrystal( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( this.Active ? 1060742 : 1060743 ); // active / inactive
			list.Add( 1060745 ); // broadcast
			list.Add( 1060741, this.Charges.ToString() ); // charges: ~1_val~

			if ( Receivers.Count > 0 )
				list.Add( 1060746, Receivers.Count.ToString() ); // links: ~1_val~
		}

		public override void OnSingleClick(Mobile from)
		{
			base.OnSingleClick( from );

			LabelTo( from, this.Active ? 1060742 : 1060743 ); // active / inactive
			LabelTo( from, 1060745 ); // broadcast
			LabelTo( from, 1060741, this.Charges.ToString() ); // charges: ~1_val~

			if ( Receivers.Count > 0 )
				LabelTo( from, 1060746, Receivers.Count.ToString() ); // links: ~1_val~
		}

		public override bool HandlesOnSpeech
		{
			get{ return Active && Receivers.Count > 0 && ( RootParent == null || RootParent is Mobile ); }
		}

		public override void OnSpeech( SpeechEventArgs e )
		{
			if ( !Active || Receivers.Count == 0 || ( RootParent != null && !(RootParent is Mobile) ) )
				return;

			if ( e.Type == MessageType.Emote )
				return;

			Mobile from = e.Mobile;
			string speech = e.Speech;

			foreach ( ReceiverCrystal receiver in new ArrayList( Receivers ) )
			{
				if ( receiver.Deleted )
				{
					Receivers.Remove( receiver );
				}
				else if ( Charges > 0 )
				{
					receiver.TransmitMessage( from, speech );
					Charges--;
				}
				else
				{
					this.Active = false;
					break;
				}
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return;
			}

			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private BroadcastCrystal m_Crystal;

			public InternalTarget( BroadcastCrystal crystal ) : base( 2, false, TargetFlags.None )
			{
				m_Crystal = crystal;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( !m_Crystal.IsAccessibleTo( from ) )
					return;

				if ( from.Map != m_Crystal.Map || !from.InRange( m_Crystal.GetWorldLocation(), 2 ) )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
					return;
				}

				if ( targeted == m_Crystal )
				{
					if ( m_Crystal.Active )
					{
						m_Crystal.Active = false;
						from.SendLocalizedMessage( 500672 ); // You turn the crystal off.
					}
					else
					{
						if ( m_Crystal.Charges > 0 )
						{
							m_Crystal.Active = true;
							from.SendLocalizedMessage( 500673 ); // You turn the crystal on.
						}
						else
						{
							from.SendLocalizedMessage( 500676 ); // This crystal is out of charges.
						}
					}
				}
				else if ( targeted is ReceiverCrystal )
				{
					ReceiverCrystal receiver = (ReceiverCrystal) targeted;

					if ( m_Crystal.Receivers.Count >= 10 )
					{
						from.SendLocalizedMessage( 1010042 ); // This broadcast crystal is already linked to 10 receivers.
					}
					else if ( receiver.Sender == m_Crystal )
					{
						from.SendLocalizedMessage( 500674 ); // This crystal is already linked with that crystal.
					}
					else if ( receiver.Sender != null )
					{
						from.SendLocalizedMessage( 1010043 ); // That receiver crystal is already linked to another broadcast crystal.
					}
					else
					{
						receiver.Sender = m_Crystal;
						from.SendLocalizedMessage( 500675 ); // That crystal has been linked to this crystal.
					}
				}
				else if ( targeted == from )
				{
					foreach( ReceiverCrystal receiver in new List<ReceiverCrystal>( m_Crystal.Receivers ) )
					{
						receiver.Sender = null;
					}

					from.SendLocalizedMessage( 1010046 ); // You unlink the broadcast crystal from all of its receivers.
				}
				else
				{
					Item targItem = targeted as Item;

					if ( targItem != null && targItem.VerifyMove( from ) )
					{
						CrystalRechargeInfo info = CrystalRechargeInfo.Get( targItem.GetType() );

						if ( info != null )
						{
							if ( m_Crystal.Charges >= MaxCharges )
							{
								from.SendLocalizedMessage( 500678 ); // This crystal is already fully charged.
							}
							else
							{
								targItem.Consume();

								if ( m_Crystal.Charges + info.Amount >= MaxCharges )
								{
									m_Crystal.Charges = MaxCharges;
									from.SendLocalizedMessage( 500679 ); // You completely recharge the crystal.
								}
								else
								{
									m_Crystal.Charges += info.Amount;
									from.SendLocalizedMessage( 500680 ); // You recharge the crystal.
								}
							}

							return;
						}
					}

					from.SendLocalizedMessage( 500681 ); // You cannot use this crystal on that.
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.WriteEncodedInt( m_Charges );
			writer.WriteItemList<ReceiverCrystal>( m_Receivers );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_Charges = reader.ReadEncodedInt();
			m_Receivers = reader.ReadStrongItemList<ReceiverCrystal>();
		}
	}

	public class ReceiverCrystal : Item
	{
		public override int LabelNumber{ get{ return 1060740; } } // communication crystal

		private BroadcastCrystal m_Sender;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool Active
		{
			get{ return this.ItemID == 0x1ED1; }
			set
			{
				this.ItemID = value ? 0x1ED1 : 0x1ED0;
				InvalidateProperties();
			}
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public BroadcastCrystal Sender
		{
			get{ return m_Sender; }
			set
			{
				if ( m_Sender != null )
				{
					m_Sender.Receivers.Remove( this );
					m_Sender.InvalidateProperties();
				}

				m_Sender = value;

				if ( value != null )
				{
					value.Receivers.Add( this );
					value.InvalidateProperties();
				}
			}
		}

		[Constructable]
		public ReceiverCrystal() : base( 0x1ED0 )
		{
			Light = LightType.Circle150;
		}

		public ReceiverCrystal( Serial serial ) : base( serial )
		{
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			list.Add( this.Active ? 1060742 : 1060743 ); // active / inactive
			list.Add( 1060744 ); // receiver
		}

		public override void OnSingleClick( Mobile from )
		{
			base.OnSingleClick( from );

			LabelTo( from, this.Active ? 1060742 : 1060743 ); // active / inactive
			LabelTo( from, 1060744 ); // receiver
		}

		public void TransmitMessage( Mobile from, string message )
		{
			if ( !this.Active )
				return;

			string text = String.Format( "{0} says {1}", from.Name, message );

			if ( this.RootParent is Mobile )
			{
				((Mobile)this.RootParent).SendMessage( 0x2B2, "Crystal: " + text );
			}
			else if ( this.RootParent is Item )
			{
				((Item)this.RootParent).PublicOverheadMessage( MessageType.Regular, 0x2B2, false, "Crystal: " + text );
			}
			else
			{
				PublicOverheadMessage( MessageType.Regular, 0x2B2, false, text );
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !from.InRange( GetWorldLocation(), 2 ) )
			{
				from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
				return;
			}

			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private ReceiverCrystal m_Crystal;

			public InternalTarget( ReceiverCrystal crystal ) : base( -1, false, TargetFlags.None )
			{
				m_Crystal = crystal;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( !m_Crystal.IsAccessibleTo( from ) )
					return;

				if ( from.Map != m_Crystal.Map || !from.InRange( m_Crystal.GetWorldLocation(), 2 ) )
				{
					from.LocalOverheadMessage( MessageType.Regular, 0x3B2, 1019045 ); // I can't reach that.
					return;
				}

				if ( targeted == m_Crystal )
				{
					if ( m_Crystal.Active )
					{
						m_Crystal.Active = false;
						from.SendLocalizedMessage( 500672 ); // You turn the crystal off.
					}
					else
					{
						m_Crystal.Active = true;
						from.SendLocalizedMessage( 500673 ); // You turn the crystal on.
					}
				}
				else if ( targeted == from )
				{
					if ( m_Crystal.Sender != null )
					{
						m_Crystal.Sender = null;
						from.SendLocalizedMessage( 1010044 ); // You unlink the receiver crystal.
					}
					else
					{
						from.SendLocalizedMessage( 1010045 ); // That receiver crystal is not linked.
					}
				}
				else
				{
					Item targItem = targeted as Item;

					if ( targItem != null && targItem.VerifyMove( from ) )
					{
						CrystalRechargeInfo info = CrystalRechargeInfo.Get( targItem.GetType() );

						if ( info != null )
						{
							from.SendLocalizedMessage( 500677 ); // This crystal cannot be recharged.
							return;
						}
					}

					from.SendLocalizedMessage( 1010045 ); // That receiver crystal is not linked.
				}
			}
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.WriteItem<BroadcastCrystal>( m_Sender );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_Sender = reader.ReadItem<BroadcastCrystal>();
		}
	}
}