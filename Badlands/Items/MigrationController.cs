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

namespace Badlands.Items;

[SerializationGenerator(0, false)]
public partial class MigrationController : Item
{
    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    public List<Type> _migrations;

    [Constructible]
    public MigrationController() : base (0x0ED4)
    {
        Name = "Migration Controller";
        Visible = false;
    }

    public void AddMigration( Type migration )
    {
        _migrations ??= [];

        _migrations.Add( migration );
    }

    public bool HasMigration( Type migration )
    {
        _migrations ??= [];

        return _migrations.Contains( migration );
    }
}