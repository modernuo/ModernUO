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

using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Engines.Quests;
using Server.Items;

namespace Badlands.Quests.FriendOfTheLibrary;

public class AccessToTheStacksQuest : MLQuest
{
    public AccessToTheStacksQuest()
    {
        Activated = true;
        /* Access to the Stacks */
        Title = 1072723;
        /* There you are!  *pleased smile*  Don't you just love when a form is all filled in like that?  All of the sections are complete,
        everything is neat and tidy and the official seal is perfectly formed and in exactly the right spot.  It's comforting.  Here you are.
        All you need to do now is return the form to Librarian Verity.  Have a nice day! */
        Description = 1072748;
        /* Oh dear!  You've changed your mind?  *looks flustered*  I'll file your notarized application then, in case you decide at a future
        date to become a friend of the library. */
        RefusalMessage = 1072750;
        /* The librarian can always be found in the Library.  *admiring tone*  She's got a really strong work ethic. */
        InProgressMessage = 1072751;
        /* As an official friend of the library you can make contributions at a donation area. */
        CompletionMessage = 1074811;

        Objectives.Add(
            new DeliverObjective(
                typeof( NotarizedApplication ),
                1, "notarized application",
                typeof( Verity )
                /*"Verity (Britain)"*/
            )
        );

        Rewards.Add(new ItemReward( 1072749, typeof(FriendOfTheLibraryToken))); // Friends of the Library Membership Token.
    }

    public override bool IsChainTriggered => true;

    public override void OnRewardClaimed( MLQuestInstance instance )
    {
        base.OnRewardClaimed( instance );

        instance.Player.LibraryFriend = true;
    }
}
