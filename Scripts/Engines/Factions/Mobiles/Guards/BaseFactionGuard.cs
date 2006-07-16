using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Factions.AI;

namespace Server.Factions
{
	public abstract class BaseFactionGuard : BaseCreature
	{
		private Faction m_Faction;
		private Town m_Town;
		private Orders m_Orders;

		public override bool BardImmune{ get{ return true; } }

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public Faction Faction
		{
			get{ return m_Faction; }
			set{ Unregister(); m_Faction = value; Register(); }
		}

		public Orders Orders
		{
			get{ return m_Orders; }
		}

		[CommandProperty( AccessLevel.GameMaster, AccessLevel.Administrator )]
		public Town Town
		{
			get{ return m_Town; }
			set{ Unregister(); m_Town = value; Register(); }
		}

		public void Register()
		{
			if ( m_Town != null && m_Faction != null )
				m_Town.RegisterGuard( this );
		}

		public void Unregister()
		{
			if ( m_Town != null )
				m_Town.UnregisterGuard( this );
		}

		public abstract GuardAI GuardAI{ get; }

		protected override BaseAI ForcedAI
		{
			get { return new FactionGuardAI( this ); }
		}

		public override TimeSpan ReacquireDelay{ get{ return TimeSpan.FromSeconds( 2.0 ); } }
 
		public override bool IsEnemy( Mobile m )
		{
			Faction ourFaction = m_Faction;
			Faction theirFaction = Faction.Find( m );

			if ( theirFaction == null && m is BaseFactionGuard )
				theirFaction = ((BaseFactionGuard)m).Faction;

			if ( ourFaction != null && theirFaction != null && ourFaction != theirFaction )
			{
				ReactionType reactionType = Orders.GetReaction( theirFaction ).Type;

				if ( reactionType == ReactionType.Attack )
					return true;

				if ( theirFaction != null )
				{
					List<AggressorInfo> list = m.Aggressed;

					for ( int i = 0; i < list.Count; ++i )
					{
						AggressorInfo ai = list[i];

						if ( ai.Defender is BaseFactionGuard )
						{
							BaseFactionGuard bf = (BaseFactionGuard)ai.Defender;

							if ( bf.Faction == ourFaction )
								return true;
						}
					}
				}
			}

			return false;
		}
 
		public override void OnMovement( Mobile m, Point3D oldLocation )
		{
			if ( m.Player && m.Alive && InRange( m, 10 ) && !InRange( oldLocation, 10 ) && InLOS( m ) && m_Orders.GetReaction( Faction.Find( m ) ).Type == ReactionType.Warn )
			{
				Direction = GetDirectionTo( m );

				string warning = null;

				switch ( Utility.Random( 6 ) )
				{
					case 0: warning = "I warn you, {0}, you would do well to leave this area before someone shows you the world of gray."; break;
					case 1: warning = "It would be wise to leave this area, {0}, lest your head become my commanders' trophy."; break;
					case 2: warning = "You are bold, {0}, for one of the meager {1}. Leave now, lest you be taught the taste of dirt."; break;
					case 3: warning = "Your presence here is an insult, {0}. Be gone now, knave."; break;
					case 4: warning = "Dost thou wish to be hung by your toes, {0}? Nay? Then come no closer."; break;
					case 5: warning = "Hey, {0}. Yeah, you. Get out of here before I beat you with a stick."; break;
				}

				Faction faction = Faction.Find( m );

				Say( warning, m.Name, faction == null ? "civilians" : faction.Definition.FriendlyName );
			}
		}

		private const int ListenRange = 12;

		public override bool HandlesOnSpeech( Mobile from )
		{
			if ( InRange( from, ListenRange ) )
				return true;

			return base.HandlesOnSpeech( from );
		}

		private DateTime m_OrdersEnd;

		private void ChangeReaction( Faction faction, ReactionType type )
		{
			if ( faction == null )
			{
				switch ( type )
				{
					case ReactionType.Ignore:	Say( 1005179 ); break; // Civilians will now be ignored.
					case ReactionType.Warn:		Say( 1005180 ); break; // Civilians will now be warned of their impending deaths.
					case ReactionType.Attack:	return;
				}
			}
			else
			{
				TextDefinition def = null;

				switch ( type )
				{
					case ReactionType.Ignore:	def = faction.Definition.GuardIgnore; break;
					case ReactionType.Warn:		def = faction.Definition.GuardWarn; break;
					case ReactionType.Attack:	def = faction.Definition.GuardAttack; break;
				}

				if ( def != null && def.Number > 0 )
					Say( def.Number );
				else if ( def != null && def.String != null )
					Say( def.String );
			}

			m_Orders.SetReaction( faction, type );
		}

		private bool WasNamed( string speech )
		{
			string name = this.Name;

			return ( name != null && Insensitive.StartsWith( speech, name ) );
		}
 
		public override void OnSpeech( SpeechEventArgs e )
		{
			base.OnSpeech( e );

			Mobile from = e.Mobile;

			if ( !e.Handled && InRange( from, ListenRange ) && from.Alive )
			{
				if ( e.HasKeyword( 0xE6 ) && (Insensitive.Equals( e.Speech, "orders" ) || WasNamed( e.Speech )) ) // *orders*
				{
					if ( m_Town == null || !m_Town.IsSheriff( from ) )
					{
						this.Say( 1042189 ); // I don't work for you!
					}
					else if ( Town.FromRegion( this.Region ) == m_Town )
					{
						this.Say( 1042180 ); // Your orders, sire?
						m_OrdersEnd = DateTime.Now + TimeSpan.FromSeconds( 10.0 );
					}
				}
				else if ( DateTime.Now < m_OrdersEnd )
				{
					if ( m_Town != null && m_Town.IsSheriff( from ) && Town.FromRegion( this.Region ) == m_Town )
					{
						m_OrdersEnd = DateTime.Now + TimeSpan.FromSeconds( 10.0 );

						bool understood = true;
						ReactionType newType = 0;

						if ( Insensitive.Contains( e.Speech, "attack" ) )
							newType = ReactionType.Attack;
						else if ( Insensitive.Contains( e.Speech, "warn" ) )
							newType = ReactionType.Warn;
						else if ( Insensitive.Contains( e.Speech, "ignore" ) )
							newType = ReactionType.Ignore;
						else
							understood = false;

						if ( understood )
						{
							understood = false;

							if ( Insensitive.Contains( e.Speech, "civil" ) )
							{
								ChangeReaction( null, newType );
								understood = true;
							}

							List<Faction> factions = Faction.Factions;

							for ( int i = 0; i < factions.Count; ++i )
							{
								Faction faction = factions[i];

								if ( faction != m_Faction && Insensitive.Contains( e.Speech, faction.Definition.Keyword ) )
								{
									ChangeReaction( faction, newType );
									understood = true;
								}
							}
						}
						else if ( Insensitive.Contains( e.Speech, "patrol" ) )
						{
							Home = Location;
							RangeHome = 6;
							Combatant = null;
							m_Orders.Movement = MovementType.Patrol;
							Say( 1005146 ); // This spot looks like it needs protection!  I shall guard it with my life.
							understood = true;
						}
						else if ( Insensitive.Contains( e.Speech, "follow" ) )
						{
							Home = Location;
							RangeHome = 6;
							Combatant = null;
							m_Orders.Follow = from;
							m_Orders.Movement = MovementType.Follow;
							Say( 1005144 ); // Yes, Sire.
							understood = true;
						}

						if ( !understood )
							Say( 1042183 ); // I'm sorry, I don't understand your orders...
					}
				}
			}
		}

		public override void GetProperties( ObjectPropertyList list )
		{
			base.GetProperties( list );

			if ( m_Faction != null && Map == Faction.Facet )
				list.Add( 1060846, m_Faction.Definition.PropName ); // Guard: ~1_val~
		}

		public override void OnSingleClick( Mobile from )
		{
			if ( m_Faction != null && Map == Faction.Facet )
			{
				string text = String.Concat( "(Guard, ", m_Faction.Definition.FriendlyName, ")" );

				int hue = ( Faction.Find( from ) == m_Faction ? 98 : 38 );

				PrivateOverheadMessage( MessageType.Label, hue, true, text, from.NetState );
			}

			base.OnSingleClick( from );
		}

		public virtual void GenerateRandomHair()
		{
			Utility.AssignRandomHair( this );
			Utility.AssignRandomFacialHair( this, HairHue );
		}

		private static Type[] m_StrongPotions = new Type[]
		{
			typeof( GreaterHealPotion ), typeof( GreaterHealPotion ), typeof( GreaterHealPotion ),
			typeof( GreaterCurePotion ), typeof( GreaterCurePotion ), typeof( GreaterCurePotion ),
			typeof( GreaterStrengthPotion ), typeof( GreaterStrengthPotion ),
			typeof( GreaterAgilityPotion ), typeof( GreaterAgilityPotion ),
			typeof( TotalRefreshPotion ), typeof( TotalRefreshPotion ),
			typeof( GreaterExplosionPotion )
		};

		private static Type[] m_WeakPotions = new Type[]
		{
			typeof( HealPotion ), typeof( HealPotion ), typeof( HealPotion ),
			typeof( CurePotion ), typeof( CurePotion ), typeof( CurePotion ),
			typeof( StrengthPotion ), typeof( StrengthPotion ),
			typeof( AgilityPotion ), typeof( AgilityPotion ),
			typeof( RefreshPotion ), typeof( RefreshPotion ),
			typeof( ExplosionPotion )
		};

		public void PackStrongPotions( int min, int max )
		{
			PackStrongPotions( Utility.RandomMinMax( min, max ) );
		}

		public void PackStrongPotions( int count )
		{
			for ( int i = 0; i < count; ++i )
				PackStrongPotion();
		}

		public void PackStrongPotion()
		{
			PackItem( Loot.Construct( m_StrongPotions ) );
		}

		public void PackWeakPotions( int min, int max )
		{
			PackWeakPotions( Utility.RandomMinMax( min, max ) );
		}

		public void PackWeakPotions( int count )
		{
			for ( int i = 0; i < count; ++i )
				PackWeakPotion();
		}

		public void PackWeakPotion()
		{
			PackItem( Loot.Construct( m_WeakPotions ) );
		}

		public Item Immovable( Item item )
		{
			item.Movable = false;
			return item;
		}

		public Item Newbied( Item item )
		{
			item.LootType = LootType.Newbied;
			return item;
		}

		public Item Rehued( Item item, int hue )
		{
			item.Hue = hue;
			return item;
		}

		public Item Layered( Item item, Layer layer )
		{
			item.Layer = layer;
			return item;
		}

		public Item Resourced( BaseWeapon weapon, CraftResource resource )
		{
			weapon.Resource = resource;
			return weapon;
		}

		public Item Resourced( BaseArmor armor, CraftResource resource )
		{
			armor.Resource = resource;
			return armor;
		}

		public override void OnAfterDelete()
		{
			base.OnAfterDelete();
			Unregister();
		}

		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			c.Delete();
		}

		public virtual void GenerateBody( bool isFemale, bool randomHair )
		{
			Hue = Utility.RandomSkinHue();

			if ( isFemale )
			{
				Female = true;
				Body = 401;
				Name = NameList.RandomName( "female" );
			}
			else
			{
				Female = false;
				Body = 400;
				Name = NameList.RandomName( "male" );
			}

			if ( randomHair )
				GenerateRandomHair();
		}

		public override bool ClickTitle{ get{ return false; } }

		public BaseFactionGuard( string title ) : base( AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4 )
		{
			m_Orders = new Orders( this );
			Title = title;

			RangeHome = 6;
		}

		public BaseFactionGuard( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			Faction.WriteReference( writer, m_Faction );
			Town.WriteReference( writer, m_Town );

			m_Orders.Serialize( writer );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Faction = Faction.ReadReference( reader );
			m_Town = Town.ReadReference( reader );
			m_Orders = new Orders( this, reader );

			Timer.DelayCall( TimeSpan.Zero, new TimerCallback( Register ) );
		}
	}

	public class VirtualMount : IMount
	{
		private VirtualMountItem m_Item;

		public Mobile Rider
		{
			get{ return m_Item.Rider; }
			set{}
		}

		public VirtualMount( VirtualMountItem item )
		{
			m_Item = item;
		}

		public virtual void OnRiderDamaged( int amount, Mobile from, bool willKill )
		{
		}
	}

	public class VirtualMountItem : Item, IMountItem
	{
		private Mobile m_Rider;
		private VirtualMount m_Mount;

		public Mobile Rider{ get{ return m_Rider; } }

		public VirtualMountItem( Mobile mob ) : base( 0x3EA0 )
		{
			Layer = Layer.Mount;

			m_Rider = mob;
			m_Mount = new VirtualMount( this );
		}

		public IMount Mount
		{
			get{ return m_Mount; }
		}

		public VirtualMountItem( Serial serial ) : base( serial )
		{
			m_Mount = new VirtualMount( this );
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version

			writer.Write( (Mobile) m_Rider );
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			m_Rider = reader.ReadMobile();

			if ( m_Rider == null )
				Delete();
		}
	}
}