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

namespace Badlands.Engines.ML_Quests.Definitions;

public class EvidenceQuest : MLQuest
{
    public EvidenceQuest()
    {
        /* Evidence */
        Title = 1072906;
        /* We believe the Black Order has fallen under the sway of Minax, somehow.  Seek evidence that proves our theory
by piercing the secrets of the Citadel. */
        Description = 1072964;
        /* Many fear to tangle with the wicked sorceress.  I understand and appreciate your concerns. */
        RefusalMessage = 1072975;
        /* I don't know where inside The Citadel such evidence could be found.  Perhaps the most guarded sanctum is
the place to look. */
        InProgressMessage = 1072976;

        Objectives.Add( new CollectObjective( 1, typeof( OrdersFromMinax ), "orders from minax" ) );
        Rewards.Add( ItemReward.Strongbox );
    }
}
