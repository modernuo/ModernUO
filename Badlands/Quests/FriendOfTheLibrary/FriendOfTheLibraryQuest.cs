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
using Badlands.Quests.FriendOfTheLibrary;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;

namespace Server.Engines.MLQuests.Definitions;

[SerializationGenerator( 0 )]
public partial class FriendsOfTheLibraryQuest : MLQuest
{
    public FriendsOfTheLibraryQuest()
    {
        Activated = true;
        Title = 1072716; // Friends of the Library
        Description =
            1072720; // Shhh!  *angry whisper*  This is a library, not some bawdy house!  If you wish to become a friend of the library
        // you'll learn to moderate your volume.  And, of course, you'll take this application and have it notarized by Sarakki
        // the Notary.  Until you've become an official friend of the library, I can't allow you to make donations.  It wouldn't
        // be proper.
        RefusalMessage = 1072721; // *glare*  Shhhh!  You need to visit the notary.  She can be found near the castle.
        /* *glare*  Shhhh!  You need to visit the notary.  She can be found near the castle. */
        InProgressMessage = 1072722;
        /* Greetings! */
        CompletionMessage = 1073985;

        Objectives.Add(
            new DeliverObjective(
                typeof( LibraryApplication ),
                1,
                "friends of the library application",
                typeof( Sarakki ) /*, "Sarakki (Britain)" )*/
            )
        );

        Rewards.Add( new DummyReward( 1072749 ) ); // Friends of the Library Membership Token.

        OneTimeOnly = true;
    }

    public override Type NextQuest => typeof( BureaucraticDelayQuest );
}
