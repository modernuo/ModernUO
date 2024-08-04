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
using Server.Items;

namespace Badlands.Quests.FriendOfTheLibrary;

public class TheSecretIngredientQuest : MLQuest
{
    public TheSecretIngredientQuest()
    {
        Activated = true;
        /* The Secret Ingredient */
        Title = 1072718;
        /* What's this?  Sealing wax ... Sarakki ... official documents ... Oh, I see.  Can do, can do.  You will need to get me the poison
        sacs of course.  They are so volatile they aren't viable after an hour or so ... yes, yes.  Right, well off you go, speckled scorpions
        are the only critters that have the right poison.  They live in the desert near here. */
        Description = 1072727;
        /* I see. I see.  Well good luck to you. */
        RefusalMessage = 1072728;
        /* Hello, hello!  Speckled, that's what they are.  All covered with spots and speckles when you look very closely.  Speckled scorpion
        poison sacs will do the trick. */
        InProgressMessage = 1072729;
        /* Fine, fine.  Do you have them? */
        CompletionMessage = 1073987;

        Objectives.Add( new CollectObjective( 5, typeof( SpeckledPoisonSac ), "speckled poison sacs" /*, 0x23A, 3600*/ ) );

        Rewards.Add( new DummyReward( 1074871 ) ); // A step closer to having sealing wax.
    }

    //public override QuestChain ChainID => QuestChain.LibraryFriends;
    public override Type NextQuest => typeof(SpecialDeliveryQuest);
    public override bool IsChainTriggered => true;
}
