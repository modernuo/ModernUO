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
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;

namespace Badlands.Quests.FriendOfTheLibrary;

public class SpecialDeliveryQuest : MLQuest
{
    public SpecialDeliveryQuest()
    {
        Activated = true;
        /* Special Delivery */
        Title = 1072719;
        /* Good good!  The wax is just cooling now and will be ready by the time you get it back to Sarakki.  You still want the wax, right? */
        Description = 1072741;
        /* Sorry, sorry.  I'll hold onto your order in case you change your mind. */
        RefusalMessage = 1072746;
        /* Hello, hello.  No rush of course.  I'm sure Sarakki is patient and doesn't mind waiting for the wax. */
        InProgressMessage = 1072747;
        /* Oh welcome back!  Do you have my wax? */
        CompletionMessage = 1073988;

        Objectives.Add(
            new DeliverObjective(
                typeof( OfficialSealingWax ),
                1,
                "sealing wax",
                typeof( Sarakki ) /*, "Sarakki (Britain)"*/
            )
        );

        Rewards.Add( new DummyReward( 1072749 ) ); // Friends of the Library Membership Token.
    }

    public override Type NextQuest => typeof( AccessToTheStacksQuest );
    public override bool IsChainTriggered => true;
}
