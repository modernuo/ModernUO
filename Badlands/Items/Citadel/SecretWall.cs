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
using Server.Mobiles;

namespace Badlands.Items.Citadel;

[SerializationGenerator( 0 )]
public partial class SecretWall : Item
{
    [SerializableField( 3 )] [SerializedCommandProperty( AccessLevel.GameMaster, true )]
    public bool _active = true;

    [SerializableField( 2 )] [SerializedCommandProperty( AccessLevel.GameMaster, true )]
    public bool _locked;

    [SerializableField( 1 )] [SerializedCommandProperty( AccessLevel.GameMaster, true )]
    public Map _mapDest;

    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster, true )]
    public Point3D _pointDest;

    [Constructible]
    public SecretWall( int itemId ) : base( itemId )
    {
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( from.InRange( Location, 2 ) )
        {
            if ( !_locked && _active )
            {
                BaseCreature.TeleportPets( from, PointDest, MapDest );
                from.MoveToWorld( PointDest, MapDest );
                from.SendLocalizedMessage( 1072790 ); // The wall becomes transparent, and you push your way through it.
            }
            else
            {
                from.Say( 502684 ); // This door appears to be locked.
            }
        }
    }
}
