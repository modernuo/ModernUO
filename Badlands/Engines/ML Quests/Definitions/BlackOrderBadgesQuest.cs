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
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Objectives;
using Server.Engines.MLQuests.Rewards;
using Server.Items;
using Server.Mobiles;

namespace Badlands.Engines.ML_Quests.Definitions;

public class BlackOrderBadgesQuest : MLQuest
{
    public BlackOrderBadgesQuest()
    {
        Activated = true;
        /* Black Order Badges */
        Title = 1072903;
        /* What's that? *alarmed gasp*  Do not speak of the Black Order so loudly, they might hear and take offense.  *whispers*
    I collect the badges of their sects, if you wish to seek them out and slay them.  Bring five of each and I will reward you. */
        Description = 1072962;
        /* *whisper* It's a very dangerous task.  Let me know if you change your mind. */
        RefusalMessage = 1072971;
        /* *whisper* The Citadel entrance is disguised as a fishing village.  The magical portal into the stronghold itself is
moved frequently.  You'll need to search for it. */
        InProgressMessage = 1072972;

        Objectives.Add( new CollectObjective( 5, typeof( SerpentFangSectBadge ), "serpent fang badges" ) );
        Objectives.Add( new CollectObjective( 5, typeof( TigerClawSectBadge ), "tiger claw badges" ) );
        Objectives.Add( new CollectObjective( 5, typeof( DragonFlameSectBadge ), "dragon flame badges" ) );

        Rewards.Add( ItemReward.BagOfTreasure );
    }
}

[SerializationGenerator( 0, false )]
public partial class Sarakki : BaseCreature
{
    [Constructible]
    public Sarakki() : base( AIType.AI_Vendor )

    {
        Title = "the notary";
        InitStats( 100, 100, 25 );
        Female = true;
        Race = Race.Human;

        Hue = 0x841E;
        HairItemID = 0x2049;
        HairHue = 0x1BB;

        AddItem( new Backpack() );
        AddItem( new Shoes( 0x740 ) );
        AddItem( new FancyShirt( 0x72C ) );
        AddItem( new Skirt( 0x53C ) );
    }

    public override string DefaultName { get; } = "Sarakki";
}
