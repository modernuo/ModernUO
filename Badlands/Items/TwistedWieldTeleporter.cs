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
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class TwistedWieldTeleporter : Teleporter
{
    public TwistedWieldTeleporter() : base( new Point3D( 2186, 1251, 3 ), Map.Ilshenar, true )
    {
    }

    public override bool OnMoveOver( Mobile m )
    {
        //if (!MondainsLegacy.TwistedWeald && (int)m.AccessLevel < (int)AccessLevel.GameMaster)
        //{
        //    m.SendLocalizedMessage(1042753, "Twisted Weald"); // ~1_SOMETHING~ has been temporarily disabled.
        //    return true;
        //}

        if ( m is PlayerMobile player )
        {
            var context = MLQuestSystem.GetContext( player );

            if ( context == null )
            {
                player.SendLocalizedMessage(1074274); // You dance in the fairy ring, but nothing happens.
                return true;
            }

            if ( context.IsDoingQuest( typeof( DreadhornQuest ) ) || context.HasDoneQuest( typeof( DreadhornQuest ) ) )
            {
                return base.OnMoveOver( m );
            }

            player.SendLocalizedMessage( 1074274 ); // You dance in the fairy ring, but nothing happens.
        }

        return true;
    }
}
