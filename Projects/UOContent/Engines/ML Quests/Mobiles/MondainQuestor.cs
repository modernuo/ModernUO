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
using Server.Mobiles;

namespace Server.Engines.ML_Quests.Mobiles;

[SerializationGenerator(0, false )]
public partial class MondainQuestor : BaseCreature
{
    public MondainQuestor( string name, string title ) : base(AIType.AI_Vendor, FightMode.None, 2)
    {
        Name = name;
        Title = title;
        SpeechHue = 0x3B2;

        InitBody();
        InitOutfit();
    }

    public virtual void InitBody()
    {
    }

    public virtual void InitOutfit()
    {
    }
}
