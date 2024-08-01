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

using Server;

namespace Badlands.Migrations;

public class MigrationPersistence() : GenericPersistence( "Badlands", 10 )
{
    private readonly List<MigrationEntry> _migrations = new();

    public override void Serialize( IGenericWriter writer )
    {
        writer.WriteEncodedInt( 1 ); // version

        writer.Write( _migrations.Count );

        foreach ( var migration in _migrations )
        {
            writer.Write( migration.Type );
            writer.Write( migration.Name );
            writer.Write( migration.MigrationDateTime );

            writer.Write( migration.Entities.Count );

            foreach ( var entity in migration.Entities )
            {
                writer.Write( entity );
            }
        }
    }

    public void Add( MigrationEntry migration )
    {
        _migrations.Add( migration );
    }

    public bool Contains( Type type )
    {
        return _migrations.Any( m => m.Type == type );
    }

    public override void Deserialize( IGenericReader reader )
    {
        var version = reader.ReadEncodedInt();

        if ( version >= 1 )
        {
            var count = reader.ReadInt();

            for ( var i = 0; i < count; i++ )
            {
                var migration = new MigrationEntry
                {
                    Type = reader.ReadType(),
                    Name = reader.ReadString(),
                    MigrationDateTime = reader.ReadDateTime()
                };

                var entityCount = reader.ReadInt();

                for ( var j = 0; j < entityCount; j++ )
                {
                    migration.Entities.Add( reader.ReadSerial() );
                }

                _migrations.Add( migration );
            }
        }
    }
}

public class MigrationEntry
{
    public Type Type { get; set; }
    public string Name { get; set; }

    public List<Serial> Entities { get; set; } = [];
    public DateTime MigrationDateTime { get; set; } = DateTime.UtcNow;
}
