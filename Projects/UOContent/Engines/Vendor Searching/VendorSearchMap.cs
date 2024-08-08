using System;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.VendorSearching;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

public class VendorSearchMap : MapItem
{
    public readonly int DeleteDelayMinutes = 30;
    public readonly int TeleportCost = 1000;

    public VendorSearchMap( SearchItem item )
        : base( item.Map )
    {
        var map = item.Map;

        LootType = LootType.Blessed;
        Hue = RecallRune.CalculateHue( map, null, true );

        SearchItem = item.Item;
        Vendor = item.Vendor;
        //AuctionSafe = item.AuctionSafe;

        Point3D p;

        //if (IsAuction)
        //{
        //    p = AuctionSafe.Location;
        //}
        //else
        //{
        p = Vendor.Location;
        //}

        const int width = 300;
        const int height = 300;

        SetDisplay( p.X - width / 2, p.Y - height / 2, p.X + width / 2, p.Y + height / 2, width, height );
        AddWorldPin( p.X, p.Y );

        DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes( DeleteDelayMinutes );
        Timer.DelayCall( TimeSpan.FromMinutes( DeleteDelayMinutes ), Delete );
    }

    public VendorSearchMap( Serial serial )
        : base( serial )
    {
    }

    [CommandProperty( AccessLevel.GameMaster )]
    public PlayerVendor Vendor { get; }

    //TODO
    //[CommandProperty(AccessLevel.GameMaster)]
    //public IAuctionItem AuctionSafe { get; }

    [CommandProperty( AccessLevel.GameMaster )]
    public Item SearchItem { get; }

    [CommandProperty( AccessLevel.GameMaster )]
    public Point3D SetLocation { get; set; }

    [CommandProperty( AccessLevel.GameMaster )]
    public Map SetMap { get; set; }

    [CommandProperty( AccessLevel.GameMaster )]
    public DateTime DeleteTime { get; }

    //TODO
    //[CommandProperty(AccessLevel.GameMaster)]
    //public bool IsAuction => AuctionSafe != null;

    public int TimeRemaining => DeleteTime <= DateTime.UtcNow ? 0 : ( int )( DeleteTime - DateTime.UtcNow ).TotalMinutes;

    public override bool DropToWorld( Mobile from, Point3D p )
    {
        from.SendLocalizedMessage( 500424 ); // You destroyed the item.
        Delete();

        return true;
    }

    public override bool AllowSecureTrade( Mobile from, Mobile to, Mobile newOwner, bool accepted ) => false;

    public override void AddNameProperty( IPropertyList list )
    {
        var name = Name();

        list.Add( 1154559, string.Format( "{0}\t{1}", name[0], name[1] ) ); // Map to Vendor ~1_Name~: ~2_Shop~
    }

    public new string[] Name()
    {
        var array = new string[2];

        var Name = "Unknown";
        var Shop = "Unknown";

        //if (IsAuction)
        //{
        //    if (SearchItem != null)
        //    {
        //        if (AuctionSafe != null)
        //        {
        //            BaseHouse house = BaseHouse.FindHouseAt(AuctionSafe);

        //            if (house != null)
        //                Name = house.Sign.GetName();
        //        }

        //        Shop = (SearchItem.LabelNumber != 0) ? string.Format("#{0}", SearchItem.LabelNumber) : SearchItem.Name;
        //    }
        //}
        //else
        //{
        if ( Vendor != null )
        {
            Name = Vendor.Name;
            Shop = Vendor.ShopName;
        }
        //}

        return new[] { Name, Shop };
    }

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        var coord = GetCoords();

        if ( SetLocation == Point3D.Zero )
        {
            list.Add( 1154639, string.Format( "{0}\t{1}", coord[0], coord[1] ) ); //  Vendor Located at ~1_loc~ (~2_facet~)
        }
        else
        {
            list.Add(
                1154638,
                string.Format( "{0}\t{1}", coord[0], coord[1] )
            ); //  Return to ~1_loc~ (~2_facet~)                
        }

        if ( !IsSale() )
        {
            list.Add( 1154700 ); // Item no longer for sale.
        }

        list.Add( 1075269 ); // Destroyed when dropped
    }

    public bool IsSale() => SearchItem != null /*AuctionSafe != null && AuctionSafe.CheckAuctionItem(SearchItem) ||*/ &&
                            Vendor != null && Vendor.GetVendorItem( SearchItem ) != null;

    public string[] GetCoords()
    {
        var array = new string[2];

        var loc = Point3D.Zero;
        var locmap = Map.Internal;

        if ( SetLocation != Point3D.Zero )
        {
            loc = SetLocation;
            locmap = SetMap;
        }
        //else if (AuctionSafe != null)
        //{
        //    loc = AuctionSafe.Location;
        //    locmap = AuctionSafe.Map;
        //}
        else if ( Vendor != null )
        {
            loc = Vendor.Location;
            locmap = Vendor.Map;
        }

        if ( loc != Point3D.Zero && locmap != Map.Internal )
        {
            var x = loc.X;
            var y = loc.Y;
            var z = loc.Z;
            var map = locmap;

            int xLong = 0, yLat = 0;
            int xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            if ( Sextant.Format(
                    new Point3D( x, y, z ),
                    map,
                    ref xLong,
                    ref yLat,
                    ref xMins,
                    ref yMins,
                    ref xEast,
                    ref ySouth
                ) )
            {
                return new[]
                {
                    string.Format(
                        "{0}o {1}'{2}, {3}o {4}'{5}",
                        yLat,
                        yMins,
                        ySouth ? "S" : "N",
                        xLong,
                        xMins,
                        xEast ? "E" : "W"
                    ),
                    map.ToString()
                };
            }
        }

        return new[] { "an unknown location", "Unknown" };
    }

    public void OnBeforeTravel( Mobile from )
    {
        if ( SetLocation != Point3D.Zero )
        {
            Delete();
        }
        else
        {
            Banker.Withdraw( from, TeleportCost );
            from.SendLocalizedMessage(
                1060398,
                TeleportCost.ToString()
            ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.

            SetLocation = from.Location;
            SetMap = from.Map;
        }
    }

    public override void GetContextMenuEntries( Mobile from, ref PooledRefList<ContextMenuEntry> list )
    {
        base.GetContextMenuEntries( from, ref list );

        list.Add( new OpenMapEntry( from, this ) );

        if ( SetLocation == Point3D.Zero )
        {
            list.Add( new TeleportEntry( from, this ) );
        }
        else
        {
            list.Add( new ReturnTeleportEntry( from, this ) );
        }

        list.Add( new OpenContainerEntry( from, this ) );
    }

    public Point3D GetLocation( Mobile m )
    {
        BaseHouse h = null;

        if ( SetLocation != Point3D.Zero )
        {
            h = BaseHouse.FindHouseAt( SetLocation, SetMap, 16 );
        }
        //else if (IsAuction)
        //{
        //    if (AuctionSafe != null)
        //    {
        //        h = BaseHouse.FindHouseAt(AuctionSafe);
        //    }
        //}
        else
        {
            if ( Vendor != null )
            {
                h = BaseHouse.FindHouseAt( Vendor );
            }
        }

        if ( h != null )
        {
            m.SendLocalizedMessage( 1070905 ); // Strong magics have redirected you to a safer location!
            return h.BanLocation;
        }

        return SetLocation != Point3D.Zero ? SetLocation : Point3D.Zero;
    }

    public Map GetMap()
    {
        if ( SetLocation != Point3D.Zero )
        {
            return SetMap;
        }

        Map map = null;

        //if (IsAuction)
        //{
        //    if (AuctionSafe != null)
        //    {
        //        map = AuctionSafe.Map;
        //    }
        //}
        //else
        //{
        if ( Vendor != null )
        {
            map = Vendor.Map;
        }
        //}

        return map;
    }

    public override void Serialize( IGenericWriter writer )
    {
        base.Serialize( writer );
        writer.Write( 0 );
    }

    public override void Deserialize( IGenericReader reader )
    {
        base.Deserialize( reader );
        var version = reader.ReadInt();

        Delete();
    }

    public class OpenMapEntry : ContextMenuEntry
    {
        public OpenMapEntry( Mobile from, VendorSearchMap map )
            : base( 3006150, 1 ) // Open Map
        {
            VendorMap = map;
            Clicker = from;
        }

        public VendorSearchMap VendorMap { get; }
        public Mobile Clicker { get; }

        public override void OnClick( Mobile from, IEntity target )
        {
            VendorMap.DisplayTo( Clicker );
        }
    }

    public class TeleportEntry : ContextMenuEntry
    {
        public TeleportEntry( Mobile from, VendorSearchMap map )
            : base( 1154558 ) // Teleport To Vendor
        {
            VendorMap = map;
            Clicker = from;
            Enabled = VendorMap.IsSale();
        }

        private VendorSearchMap VendorMap { get; }
        private Mobile Clicker { get; }

        public override void OnClick( Mobile from, IEntity target )
        {
            if ( VendorMap.IsSale() )
            {
                Clicker.SendGump( new ConfirmTeleportGump( VendorMap, ( PlayerMobile )Clicker ) );
            }
            else
            {
                Clicker.SendLocalizedMessage( 1154700 ); // Item no longer for sale.
            }
        }
    }

    public class ReturnTeleportEntry : ContextMenuEntry
    {
        public ReturnTeleportEntry( Mobile from, VendorSearchMap map )
            : base( 1154636 ) // Return to Previous Location
        {
            VendorMap = map;
            Clicker = from;
        }

        private VendorSearchMap VendorMap { get; }
        private Mobile Clicker { get; }

        public override void OnClick( Mobile from, IEntity target )
        {
            if ( Clicker is PlayerMobile )
            {
                Clicker.SendGump( new ConfirmTeleportGump( VendorMap, ( PlayerMobile )Clicker ) );
            }
        }
    }

    public class OpenContainerEntry : ContextMenuEntry
    {
        public OpenContainerEntry( Mobile from, VendorSearchMap map )
            : base( 1154699 ) // Open Container Containing Item
        {
            VendorMap = map;
            Clicker = from;

            if ( VendorMap.SearchItem != null )
            {
                Container = VendorMap.SearchItem.Parent as Container;
            }

            Enabled = IsAccessible();
        }

        private VendorSearchMap VendorMap { get; }
        private Mobile Clicker { get; }
        private Container Container { get; }

        private bool IsAccessible()
        {
            if ( Container == null /*|| VendorMap.IsAuction*/ || VendorMap.Vendor == null ||
                 Container.RootParent != VendorMap.Vendor )
            {
                return false;
            }

            if ( !Container.IsAccessibleTo( Clicker ) )
            {
                return false;
            }

            if ( !Clicker.InRange( Container.GetWorldLocation(), 18 ) )
            {
                return false;
            }

            return true;
        }

        public override void OnClick( Mobile from, IEntity target )
        {
            if ( IsAccessible() )
            {
                RecurseOpen( Container, Clicker );
            }
        }

        private static void RecurseOpen( Container c, Mobile from )
        {
            if ( c.Parent is Container parent )
            {
                RecurseOpen( parent, from );
            }

            c.DisplayTo( from );
        }
    }
}
