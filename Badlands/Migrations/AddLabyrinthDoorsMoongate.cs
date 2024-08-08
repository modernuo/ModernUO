// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using ModernUO.Serialization;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Badlands.Migrations;

public class AddLabyrinthDoorsMoongate : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        var left = new LabyrinthDoor( 0x248b )
        {
            MapDest = Map.Malas,
            PointDest = new Point3D( 330, 1973, 0 )
        };

        left.MoveToWorld( new Point3D( 1732, 972, -75 ), Map.Malas );

        serials.Add( left.Serial );

        var right = new LabyrinthDoor( 0x248b )
        {
            MapDest = Map.Malas,
            PointDest = new Point3D( 330, 1973, 0 )
        };

        right.MoveToWorld( new Point3D( 1734, 972, -75 ), Map.Malas );

        serials.Add( right.Serial );

        var moongateTeleporter = new Teleporter( new Point3D( 1722, 1158, -90 ), Map.Malas );

        moongateTeleporter.MoveToWorld( new Point3D( 1775, 971, -85 ), Map.Malas );

        serials.Add( moongateTeleporter.Serial );

        return serials;
    }

    public void Down()
    {
    }
}

[SerializationGenerator( 0 )]
public partial class LabyrinthDoor : Item
{
    [InvalidateProperties] [SerializableField( 1 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Map _mapDest;

    [InvalidateProperties] [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Point3D _pointDest;

    public LabyrinthDoor( int itemId ) : base( itemId ) => Movable = false;

    public override void OnDoubleClick( Mobile m )
    {
        var map = _mapDest;

        if ( map == Map.Internal )
        {
            map = m.Map;
        }

        var p = _pointDest;

        if ( p == Point3D.Zero )
        {
            p = m.Location;
        }

        BaseCreature.TeleportPets( m, p, map );

        m.MoveToWorld( p, map );
    }
}
