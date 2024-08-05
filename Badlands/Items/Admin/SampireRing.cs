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
using Server.Items;

namespace Badlands.Items.Admin;

[SerializationGenerator(0)]
public partial class SampireRing : AnOldRing
{
    [Constructible]
    public SampireRing()
    {
        LootType = LootType.Blessed;
        Name = "Sampire Ring";
        Attributes.RegenHits = 10;
        Attributes.RegenStam = 10;
        Attributes.RegenMana = 12;
        Attributes.DefendChance = 30;
        Attributes.AttackChance = 45;
        Attributes.WeaponSpeed = 40;
        Attributes.WeaponDamage = 100;
        Attributes.LowerManaCost = 45;
        Attributes.BonusDex = 25;
        Attributes.BonusStr = 25;

        Resistances.Physical = 70;
        Resistances.Fire = 95;
        Resistances.Cold = 70;
        Resistances.Poison = 70;
        Resistances.Energy = 70;
    }

    public override bool CanEquip( Mobile m ) => m.AccessLevel >= AccessLevel.GameMaster;
}
