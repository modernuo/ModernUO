using System;
using System.Collections;
using Server;
using Server.Targeting;
using System.Collections.Generic;

namespace Server.Items
{
	public class KeyRing : Item
	{
		public static readonly int MaxKeys = 20;

		private List<Key> m_Keys;

		public List<Key> Keys { get { return m_Keys; } }

		[Constructable]
		public KeyRing() : base( 0x1011 )
		{
			Weight = 1.0; // They seem to have no weight on OSI ?!

			m_Keys = new List<Key>();
		}

		public override bool OnDragDrop( Mobile from, Item dropped )
		{
			if ( !this.IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1060640 ); // The item must be in your backpack to use it.
				return false;
			}

			Key key = dropped as Key;

			if ( key == null || key.KeyValue == 0 )
			{
				from.SendLocalizedMessage( 501689 ); // Only non-blank keys can be put on a keyring.
				return false;
			}
			else if ( this.Keys.Count >= MaxKeys )
			{
				from.SendLocalizedMessage( 1008138 ); // This keyring is full.
				return false;
			}
			else
			{
				Add( key );
				from.SendLocalizedMessage( 501691 ); // You put the key on the keyring.
				return true;
			}
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !this.IsChildOf( from.Backpack ) )
			{
				from.SendLocalizedMessage( 1060640 ); // The item must be in your backpack to use it.
				return;
			}

			from.SendLocalizedMessage( 501680 ); // What do you want to unlock?
			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private KeyRing m_KeyRing;

			public InternalTarget( KeyRing keyRing ) : base( -1, false, TargetFlags.None )
			{
				m_KeyRing = keyRing;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_KeyRing.Deleted || !m_KeyRing.IsChildOf( from.Backpack ) )
				{
					from.SendLocalizedMessage( 1060640 ); // The item must be in your backpack to use it.
					return;
				}

				if ( m_KeyRing == targeted )
				{
					m_KeyRing.Open( from );
					from.SendLocalizedMessage( 501685 ); // You open the keyring.
				}
				else if ( targeted is ILockable )
				{
					ILockable o = (ILockable) targeted;

					foreach ( Key key in m_KeyRing.Keys )
					{
						if ( key.UseOn( from, o ) )
							return;
					}

					from.SendLocalizedMessage( 1008140 ); // You do not have a key for that.
				}
				else
				{
					from.SendLocalizedMessage( 501666 ); // You can't unlock that!
				}
			}
		}

		public override void OnDelete()
		{
			base.OnDelete();

			foreach ( Key key in m_Keys )
			{
				key.Delete();
			}

			m_Keys.Clear();
		}

		public void Add( Key key )
		{
			key.Internalize();
			m_Keys.Add( key );

			UpdateItemID();
		}

		public void Open( Mobile from )
		{
			Container cont = this.Parent as Container;

			if ( cont == null )
				return;

			for ( int i = m_Keys.Count - 1; i >= 0; i-- )
			{
				Key key = m_Keys[i];

				if ( !key.Deleted && !cont.TryDropItem( from, key, true ) )
					break;

				m_Keys.RemoveAt( i );
			}

			UpdateItemID();
		}

		public void RemoveKeys( uint keyValue )
		{
			for ( int i = m_Keys.Count - 1; i >= 0; i-- )
			{
				Key key = m_Keys[i];

				if ( key.KeyValue == keyValue )
				{
					key.Delete();
					m_Keys.RemoveAt( i );
				}
			}

			UpdateItemID();
		}

		public bool ContainsKey( uint keyValue )
		{
			foreach ( Key key in m_Keys )
			{
				if ( key.KeyValue == keyValue )
					return true;
			}

			return false;
		}

		private void UpdateItemID()
		{
			if ( this.Keys.Count < 1 )
				this.ItemID = 0x1011;
			else if ( this.Keys.Count < 3 )
				this.ItemID = 0x1769;
			else if ( this.Keys.Count < 5 )
				this.ItemID = 0x176A;
			else
				this.ItemID = 0x176B;
		}

		public KeyRing( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.WriteEncodedInt( 0 ); // version

			writer.WriteItemList<Key>( m_Keys );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();

			m_Keys = reader.ReadStrongItemList<Key>();
		}
	}
}