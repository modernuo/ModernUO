/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ShrinkWand.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
     public class ShrinkWand : Item
     {
          private int _charges;

          [CommandProperty(AccessLevel.Player)]
          public int Charges
          {
               get
               {
                    return _charges;
               }
               set
               {
                    _charges = value;
                    InvalidateProperties();
               }
          }

          [Constructible]
          public ShrinkWand() : base(0x0DF2)
          {
              Charges = 10;
          }

          public override string DefaultName => $"a shrink wand ({Charges} charges)";
          public override double DefaultWeight => 1.0;

          public void GetProperties(ObjectPropertyList list)
          {
              base.GetProperties(list);
              list.Add($"Charges: {Charges}");
          }

          public ShrinkWand(Serial serial) : base(serial)
          {
          }

          public override void OnDoubleClick(Mobile from)
          {
               if (!IsChildOf(from.Backpack))
               {
                    from.SendMessage("That must be in your backpack to use.");
                    return;
               }

               from.SendMessage("Target the pet you wish to shrink.");
               from.Target = new ShrinkWandTarget(this);
          }

          private class ShrinkWandTarget : Target
          {
               private readonly ShrinkWand _wand;

               public ShrinkWandTarget(ShrinkWand wand) : base(3, false, TargetFlags.None)
               {
                    _wand = wand;
               }

               protected override void OnTarget(Mobile from, object targeted)
               {
                    if (_wand.Deleted || _wand.Charges <= 0)
                    {
                         return;
                    }

                    if (targeted is BaseCreature pet && pet.Controlled && pet.ControlMaster == from)
                    {
                         pet.Controlled = false;
                         pet.ControlMaster = null;
                         pet.Internalize();

                         var shrinkItem = new ShrinkItem(pet, from)
                         {
                              Name = pet.Name,
                              Hue = pet.Hue
                         };

                         from.AddToBackpack(shrinkItem);
                         from.SendMessage("Your pet was shrunk to a statuette.");
                         from.PlaySound(0x1FA);

                         _wand.Charges--;
                         if (_wand.Charges <= 0)
                         {
                              from.SendMessage("The wand crumbles to dust.");
                              from.PlaySound(0x3B4);
                              _wand.Delete();
                         }
                    }
                    else
                    {
                         from.SendMessage("That is not your pet.");
                    }
               }
          }

          public override void Serialize(IGenericWriter writer)
          {
               base.Serialize(writer);
               writer.Write(0); // version
               writer.Write(_charges);
          }

          public override void Deserialize(IGenericReader reader)
          {
               base.Deserialize(reader);
               int version = reader.ReadInt();
               _charges = reader.ReadInt();
          }
     }
}