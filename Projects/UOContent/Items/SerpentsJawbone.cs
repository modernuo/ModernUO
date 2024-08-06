using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class SerpentsJawbone : Item, ISecurable
{
    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 0 )]
    private SecureLevel _level;

    [Constructible]
    public SerpentsJawbone() : base( 0x9F74 )
    {
    }

    public static Dictionary<int, Point3D> Locations { get; set; }

    public override int LabelNumber => 1157654; // Serpent's Jawbone

    public override bool ForceShowProperties => true;

    public static void Initialize()
    {
        Locations = new Dictionary<int, Point3D>();

        Locations[1157135] = new Point3D( 1156, 1143, -24 ); // The Village of Lakeshire		
        Locations[1157619] = new Point3D( 644, 854, -56 );   // The Rat Fort		
        Locations[1157620] = new Point3D( 1363, 1075, -13 ); // Reg Volom			
        Locations[1016410] = new Point3D( 1572, 1046, -8 );  // Twin Oaks Tavern			
        Locations[1157621] = new Point3D( 984, 622, -80 );   // The Oasis			
        Locations[1078308] = new Point3D( 1746, 1221, -1 );  // Blood Dungeon		
        Locations[1111764] = new Point3D( 912, 1362, -21 );  // Cyclops Dungeon			
        Locations[1111765] = new Point3D( 824, 774, -80 );   // Exodus Dungeon		
        Locations[1111766] = new Point3D( 349, 1434, 16 );   // The Kirin Passage			
        Locations[1157622] = new Point3D( 971, 303, 54 );    // Pass of Karnaugh			
        Locations[1157623] = new Point3D( 1033, 1154, -24 ); // The Rat Cave		
        Locations[1078315] = new Point3D( 541, 466, -72 );   // Terort Skitas			
        Locations[1111825] = new Point3D( 1450, 1477, -29 ); // Twisted Weald			
        Locations[1113002] = new Point3D( 642, 1307, -55 );  // Wisp Dungeon			
        Locations[1157624] = new Point3D( 753, 497, -62 );   // Gwenno's Memorial			
        Locations[1157625] = new Point3D( 1504, 628, -14 );  // Desert Gypsy Camp			
        Locations[1113000] = new Point3D( 1785, 573, 71 );   // Rock Dungeon
    }

    public override void GetContextMenuEntries( Mobile from, ref PooledRefList<ContextMenuEntry> list )
    {
        base.GetContextMenuEntries( from, ref list );
        SetSecureLevelEntry.AddTo( from, this, ref list );
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( ( IsLockedDown || IsSecure ) && from.InRange( GetWorldLocation(), 2 ) )
        {
            from.SendGump( new InternalGump( from as PlayerMobile, this ) );
        }
        else if ( !from.InRange( GetWorldLocation(), 2 ) )
        {
            from.SendLocalizedMessage( 500295 ); // You are too far away to do that.
        }
        else
        {
            from.SendLocalizedMessage( 502692 ); // This must be in a house and be locked down to work.
        }
    }

    private class InternalGump : Gump
    {
        public InternalGump( PlayerMobile pm, Item jawbone )
            : base( 100, 100 )
        {
            Jawbone = jawbone;
            User = pm;

            AddGumpLayout();
        }

        public Item Jawbone { get; }
        public PlayerMobile User { get; }

        public void AddGumpLayout()
        {
            AddBackground( 0, 0, 370, 428, 0x1400 );

            AddHtmlLocalized( 10, 10, 350, 18, 1114513, "#1156704", 0x56BA ); // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>

            var i = 0;
            foreach ( var (key, _) in Locations )
            {
                AddButton( 10, 41 + i * 20, 1209, 1210, key );
                AddHtmlLocalized( 50, 41 + i * 20, 150, 20, key, 0x7FFF );
                i++;
            }
        }

        public override void OnResponse( NetState state, in RelayInfo info )
        {
            if ( info.ButtonID > 0 )
            {
                var id = info.ButtonID;

                if ( Locations.ContainsKey( id ) )
                {
                    var p = Locations[id];

                    if ( CheckTravel( p ) )
                    {
                        BaseCreature.TeleportPets( User, p, Map.Ilshenar );
                        User.Combatant = null;
                        User.Warmode = false;
                        User.Hidden = true;

                        User.MoveToWorld( p, Map.Ilshenar );

                        Effects.PlaySound( p, Map.Ilshenar, 0x1FE );
                    }
                }
            }
        }

        private bool CheckTravel( Point3D p )
        {
            if ( !User.InRange( Jawbone.GetWorldLocation(), 2 ) || User.Map != Jawbone.Map )
            {
                User.SendLocalizedMessage( 500295 ); // You are too far away to do that.
            }
            else if ( /*SpellHelper.RestrictRedTravel && */User.Murderer )
            {
                User.SendLocalizedMessage( 1019004 ); // You are not allowed to travel there.
            }
            //else if (Engines.VvV.VvVSigil.ExistsOn(User))
            //{
            //    User.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            //}
            else if ( User.Criminal )
            {
                User.SendLocalizedMessage( 1005561, "", 0x22 ); // Thou'rt a criminal and cannot escape so easily.
            }
            else if ( SpellHelper.CheckCombat( User ) )
            {
                User.SendLocalizedMessage( 1005564, "", 0x22 ); // Wouldst thou flee during the heat of battle??
            }
            else if ( User.Spell != null )
            {
                User.SendLocalizedMessage( 1049616 ); // You are too busy to do that at the moment.
            }
            else if ( User.Map == Map.Ilshenar && User.InRange( p, 1 ) )
            {
                User.SendLocalizedMessage( 1019003 ); // You are already there.
            }
            else
            {
                return true;
            }

            return false;
        }
    }
}
