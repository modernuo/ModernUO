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

namespace Badlands.Items;

[SerializationGenerator(0)]
public partial class AccessLevelChanger : Item
{
    [Constructible]
    public AccessLevelChanger() : base(0x14F0)
    {
        Name = "Access Level Changer";
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( from is PlayerMobile player && player.Serial == 0x01 )
        {
            player.AccessLevel = player.AccessLevel == AccessLevel.Player ? AccessLevel.Owner : AccessLevel.Player;
        }
    }
}
