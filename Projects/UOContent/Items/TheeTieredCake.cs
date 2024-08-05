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

using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class ThreeTieredCake : Item/*, IQuality*/
{
    //TODO Quality
    //private ItemQuality _Quality;

    //[CommandProperty(AccessLevel.GameMaster)]
    //public ItemQuality Quality { get { return _Quality; } set { _Quality = value; InvalidateProperties(); } }

    public bool PlayerConstructed => true;

    public override int LabelNumber => 1098235;  // A Three Tiered Cake 

    [Constructible]
    public ThreeTieredCake() : base(0x4BA3)
    {
        Weight = 1.0;
        Pieces = 10;
    }

    //public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
    //{
    //    Quality = (ItemQuality)quality;

    //    return quality;
    //}

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            Cake cake = new Cake
            {
                ItemID = 0x4BA4
            };

            from.PrivateOverheadMessage(MessageType.Regular, 1154, 1157341, from.NetState); // *You cut a slice from the cake.*
            from.AddToBackpack(cake);

            Pieces--;
        }
        else
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
    }

    //public override void AddCraftedProperties(IPropertyList list)
    //{
    //    if (_Quality == ItemQuality.Exceptional)
    //    {
    //        list.Add(1060636); // Exceptional
    //    }
    //}

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _pieces;
}
