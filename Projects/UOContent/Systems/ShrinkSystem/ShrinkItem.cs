/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ShrinkItem.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using Server.Mobiles;
using System.Collections.Generic;

namespace Server.Items
{
    public class ShrinkItem : Item
    {
        private string _petName;
        private Type _petType;
        private int _petHue;
        private int _petBody;
        private int _petSound;
        private int _petHits, _petStam, _petMana;
        private int _petStr, _petDex, _petInt;
        private double _petMinTameSkill;
        private int _petControlSlots;
        private List<SkillData> _petSkills;
        private List<ItemData> _petItems;

        private class SkillData
        {
            public string Name;
            public double Value;
            public double Base;
            public double Cap;
        }

        private class ItemData
        {
            public int ItemID;
            public int Hue;
            public string Name;
        }

        public ShrinkItem(BaseCreature pet, Mobile owner) : base(ShrinkTable.Lookup(pet))
        {
            if (!pet.Controlled || pet.ControlMaster != owner)
            {
                owner.SendMessage("You can only shrink your own pets.");
            }

            _petName = pet.Name;
            _petType = pet.GetType();
            _petHue = pet.Hue;
            _petBody = pet.Body;
            _petSound = pet.BaseSoundID;
            _petHits = pet.Hits;
            _petStam = pet.Stam;
            _petMana = pet.Mana;
            _petStr = pet.Str;
            _petDex = pet.Dex;
            _petInt = pet.Int;
            _petMinTameSkill = pet.MinTameSkill;
            _petControlSlots = pet.ControlSlots;

            _petSkills = new List<SkillData>();
            foreach (var skill in pet.Skills)
            {
                _petSkills.Add(new SkillData
                {
                    Name = skill.Name,
                    Value = skill.Value,
                    Base = skill.Base,
                    Cap = skill.Cap
                });
            }

            _petItems = new List<ItemData>();
            if (pet.Backpack != null)
            {
                foreach (Item item in pet.Backpack.Items)
                {
                    _petItems.Add(new ItemData
                    {
                        ItemID = item.ItemID,
                        Hue = item.Hue,
                        Name = item.Name
                    });
                }
            }
        }

        public ShrinkItem(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version

            writer.Write(_petName);
            writer.Write(_petType?.AssemblyQualifiedName ?? "");
            writer.Write(_petHue);
            writer.Write(_petBody);
            writer.Write(_petSound);
            writer.Write(_petHits);
            writer.Write(_petStam);
            writer.Write(_petMana);
            writer.Write(_petStr);
            writer.Write(_petDex);
            writer.Write(_petInt);
            writer.Write(_petMinTameSkill);
            writer.Write(_petControlSlots);

            writer.Write(_petSkills.Count);
            foreach (var skill in _petSkills)
            {
                writer.Write(skill.Name);
                writer.Write(skill.Value);
                writer.Write(skill.Base);
                writer.Write(skill.Cap);
            }

            writer.Write(_petItems.Count);
            foreach (var item in _petItems)
            {
                writer.Write(item.ItemID);
                writer.Write(item.Hue);
                writer.Write(item.Name ?? "");
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            _petName = reader.ReadString();
            var typeName = reader.ReadString();
            _petType = !string.IsNullOrEmpty(typeName) ? Type.GetType(typeName) : null;
            _petHue = reader.ReadInt();
            _petBody = reader.ReadInt();
            _petSound = reader.ReadInt();
            _petHits = reader.ReadInt();
            _petStam = reader.ReadInt();
            _petMana = reader.ReadInt();
            _petStr = reader.ReadInt();
            _petDex = reader.ReadInt();
            _petInt = reader.ReadInt();
            _petMinTameSkill = reader.ReadDouble();
            _petControlSlots = reader.ReadInt();

            int skillCount = reader.ReadInt();

            _petSkills = new List<SkillData>();
            for (int i = 0; i < skillCount; i++)
            {
                _petSkills.Add(new SkillData
                {
                    Name = reader.ReadString(),
                    Value = reader.ReadDouble(),
                    Base = reader.ReadDouble(),
                    Cap = reader.ReadDouble()
                });
            }

            int itemCount = reader.ReadInt();

            _petItems = new List<ItemData>();
            for (int i = 0; i < itemCount; i++)
            {
                _petItems.Add(new ItemData
                {
                    ItemID = reader.ReadInt(),
                    Hue = reader.ReadInt(),
                    Name = reader.ReadString()
                });
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("That must be in your backpack to use.");
                return;
            }

            BaseCreature pet = CreatePet(from);
            if (pet == null)
            {
                from.SendMessage("Failed to restore your pet.");
                return;
            }

            pet.Name = _petName;
            pet.Hue = _petHue;
            pet.Body = _petBody;
            pet.BaseSoundID = _petSound;

            pet.SetStr(_petStr);
            pet.SetDex(_petDex);
            pet.SetInt(_petInt);
            pet.SetHits(_petHits);
            pet.SetStam(_petStam);
            pet.SetMana(_petMana);

            pet.MinTameSkill = _petMinTameSkill;
            pet.ControlSlots = _petControlSlots;

            foreach (var skill in _petSkills)
            {
                if (Enum.TryParse<SkillName>(skill.Name, out var skillEnum))
                {
                    pet.SetSkill(skillEnum, skill.Base);
                    pet.Skills[skillEnum].Cap = skill.Cap;
                }
            }

            if (pet.Backpack != null)
            {
                foreach (var item in _petItems)
                {
                    Item newItem = new Item(item.ItemID)
                    {
                        Hue = item.Hue,
                        Name = item.Name
                    };

                    pet.Backpack.DropItem(newItem);
                }
            }

            pet.IsStabled = false;
            pet.StabledBy = null;
            pet.Controlled = true;
            pet.ControlMaster = from;
            pet.MoveToWorld(from.Location, from.Map);

            pet.ControlOrder = OrderType.Stay;

            Delete();

            from.SendMessage($"Your pet, {_petName}, has been restored!");
        }

        private BaseCreature CreatePet(Mobile from)
        {
            try
            {
                if (_petType == null)
                {
                    return null;
                }

                var ctor = _petType.GetConstructor(Type.EmptyTypes);
                if (ctor != null)
                {
                    return ctor.Invoke(null) as BaseCreature;
                }
            }
            catch
            {
                throw new Exception($"Failed to create {_petName}, for player {from?.Name}");
            }

            return null;
        }
    }
}