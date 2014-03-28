using System;
using System.Collections.Generic;
using System.Text;

using Server;
using Server.Mobiles;
using Server.Factions;
using System.IO;

namespace Server {
	public abstract class PowerFactionItem : Item {
		public abstract bool Use( Mobile mob );

		private sealed class DestructionTimer : Timer {
			private Mobile _mobile;

			private bool _screamed;

			public DestructionTimer( Mobile mob )
				: base( TimeSpan.FromSeconds( 5 ), TimeSpan.FromSeconds( 0.1 ), 10 ) {
				_mobile = mob;
			}

			protected override void OnTick() {
				if ( _mobile.Alive ) {
					if ( !_screamed ) {
						_screamed = true;

						_mobile.PlaySound( _mobile.Female ? 814 : 1088 );
						_mobile.PublicOverheadMessage( Server.Network.MessageType.Regular, 2118, false, "Aaaaah!" );
					}

					_mobile.Damage( Utility.Dice( 2, 6, 0 ) );
				}
			}
		}

		private sealed class WeightedItem {
			private int _weight;
			private Type _type;

			public int Weight {
				get {
					return _weight;
				}
			}

			public Type Type {
				get {
					return _type;
				}
			}

			public WeightedItem( int weight, Type type ) {
				_weight = weight;
				_type = type;
			}

			public Item Construct() {
				return Activator.CreateInstance( _type ) as Item;
			}
		}

		private static WeightedItem[] _items = {
			new WeightedItem( 30, typeof( GemOfEmpowerment ) ),
			new WeightedItem( 25, typeof( BloodRose ) ),
			new WeightedItem( 20, typeof( ClarityPotion ) ),
			new WeightedItem( 15, typeof( UrnOfAscension ) ),
			new WeightedItem( 10, typeof( StormsEye ) )
		};

		public static void CheckSpawn( Mobile killer, Mobile victim ) {
			if ( killer != null && victim != null ) {
				PlayerState ps = PlayerState.Find( victim );

				if ( ps != null ) {
					int chance = ps.Rank.Rank;

					if ( chance > Utility.Random( 100 ) ) {
						int weight = 0;

						foreach ( WeightedItem item in _items ) {
							weight += item.Weight;
						}

						weight = Utility.Random( weight );

						foreach ( WeightedItem item in _items ) {
							if ( weight < item.Weight ) {
								Item obj = item.Construct();

								if ( obj != null ) {
									killer.AddToBackpack( obj );

									killer.SendSound( 1470 );
									killer.LocalOverheadMessage(
										Server.Network.MessageType.Regular, 2119, false,
										"You notice a strange item on the corpse, and decide to pick it up."
									);

									try {
										using ( StreamWriter op = new StreamWriter( "faction-power-items.log", true ) ) {
											op.WriteLine( "{0}\t{1}\t{2}\t{3}", DateTime.UtcNow, killer, victim, obj );
										}
									} catch {
									}
								}

								break;
							} else {
								weight -= item.Weight;
							}
						}
					}
				}
			}
		}

		public override void OnDoubleClick( Mobile from ) {
			if ( !IsChildOf( from.Backpack ) ) {
				from.SendLocalizedMessage( 1042038 ); // You must have the object in your backpack to use it.
			} else if ( from is PlayerMobile && ((PlayerMobile)from).DuelContext != null ) {
				from.SendMessage( "You can't use that." );
			} else if ( Faction.Find( from ) == null ) {
				from.LocalOverheadMessage( Server.Network.MessageType.Regular, 2119, false, "The object vanishes from your hands as you touch it." );

				Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), delegate() {
					from.LocalOverheadMessage( Server.Network.MessageType.Regular, 2118, false, "You feel a strange tingling sensation throughout your body." );
				} );

				Timer.DelayCall( TimeSpan.FromSeconds( 4.0 ), delegate() {
					from.LocalOverheadMessage( Server.Network.MessageType.Regular, 2118, false, "Your skin begins to burn." );
				} );

				new DestructionTimer( from ).Start();
				Delete();

				//from.SendMessage( "You must be in a faction to use this item." );
			} else if ( Use( from ) ) {
				from.RevealingAction();
				Consume();
			}
		}

		public PowerFactionItem( int itemId )
			: base( itemId ) {
		}

		public PowerFactionItem( Serial serial )
			: base( serial ) {
		}

		public override void Serialize( GenericWriter writer ) {
			base.Serialize( writer );

			writer.WriteEncodedInt( ( int ) 0 ); // version
		}

		public override void Deserialize( GenericReader reader ) {
			base.Deserialize( reader );

			int version = reader.ReadEncodedInt();
		}
	}
}