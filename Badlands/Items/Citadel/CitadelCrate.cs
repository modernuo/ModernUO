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

using Badlands.Engines.ML_Quests.Definitions;
using ModernUO.Serialization;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;

namespace Badlands.Items.Citadel;

[SerializationGenerator(0, false)]
public partial class CitadelCrate : Item
{
    [Constructible]
    public CitadelCrate() : base(0xE3F) => Movable = false;

    public override void OnDoubleClick( Mobile from )
    {
        if (from is PlayerMobile player)
        {
            var context = MLQuestSystem.GetContext(player);

            if ( context == null )
            {
                player.SendLocalizedMessage(1074278); // You realize that your eyes are playing tricks on you. That crate isn't really shimmering.
                return;
            }

            if ( context.IsDoingQuest( typeof( BlackOrderBadgesQuest ) ) || context.IsDoingQuest( typeof( EvidenceQuest ) ) )
            {
                BaseCreature.TeleportPets(player, new Point3D(107, 1883, 0), Map.Malas);
                player.MoveToWorld(new Point3D(107, 1883, 0), Map.Malas);
            }
            else
            {
                player.SendLocalizedMessage(1074278); // You realize that your eyes are playing tricks on you. That crate isn't really shimmering.
            }
        }

    }
}
