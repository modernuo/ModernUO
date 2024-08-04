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

public class BureaucraticDelayQuest : MLQuest
{
    public BureaucraticDelayQuest()
    {
        Activated = true;
        Title = 1072717;
        /* What do you have there?  Oh.  *look of dismay*  It seems everyone is interested in helping the library -- but
        no one warned me to stock up on sealing wax.  I'm afraid I'm out of the mixture we use to notarize offical documents.
        There will be a delay ... unless you'd like to take matters into your own hands and retrieve more for me from Petrus? */
        Description = 1072724;
        /* I do apologize for being unprepared. Perhaps when you return later I'll have more wax in stock. */
        RefusalMessage = 1072725;
        /* Petrus lives in Ilshenar, past the Compassion gate and beyond the gypsy camp. */
        InProgressMessage = 1072726;
        /* Hello, hello. */
        CompletionMessage = 1073986;

        Objectives.Add(new DeliverObjective( typeof( SealingWaxOrder ), 1, "sealing wax order", typeof( Petrus )/*, "Petrus (Ilshenar)" )*/));
        Rewards.Add( new DummyReward( 1074871 ) ); // A step closer to having sealing wax.
    }

    public override bool IsChainTriggered => true;
    public override Type NextQuest => typeof(TheSecretIngredientQuest);
}
