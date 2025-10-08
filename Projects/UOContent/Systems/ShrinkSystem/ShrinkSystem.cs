/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ShrinkSystem.cs                                                 *
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
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.ShrinkSystem
{
     public class ShrinkSystem
     {
          public static void Initialize()
          {    // command = [shrink
               // change access level here (premium setup)
               CommandSystem.Register("shrink", AccessLevel.Owner, new CommandEventHandler(Shrink_OnCommand));
          }

          [Usage("shrink")] // target again or double click to restore
          [Description("Target your pet to shrink it.")]
          private static void Shrink_OnCommand(CommandEventArgs e)
          {
               e.Mobile.SendMessage("Target your pet to shrink.");
               e.Mobile.Target = new ShrinkToggleTarget();
          }

          private class ShrinkToggleTarget : Target
          {
               public ShrinkToggleTarget() : base(5, false, TargetFlags.None)
               { 
               }

               protected override void OnTarget(Mobile from, object targeted)
               {
                    if (targeted is BaseCreature pet)
                    {
                         if (!pet.Controlled || pet.ControlMaster != from)
                         {
                              from.SendMessage("That is not your pet.");
                              return;
                         }

                         if (pet.IsDeadPet)
                         {
                              from.SendMessage("You cannot shrink a dead pet.");
                              return;
                         }

                         if (pet.Combatant != null)
                         {
                              from.SendMessage("You cannot shrink a pet while it is in combat.");
                              return;
                         }

                         var statuette = new ShrinkBox(pet);
                         if (from.Backpack != null && from.Backpack.TryDropItem(from, statuette, false))
                         {
                              from.SendMessage("Your pet was shrunken to a box in your backpack.");
                              pet.Delete();
                         }
                         else
                         {
                              statuette.Delete();
                              from.SendMessage("You do not have enough space in your backpack.");
                         }
                    }
                    else if (targeted is ShrinkBox statuette)
                    {
                         statuette.ReleasePet(from);
                    }
                    else
                    {
                         from.SendMessage("That is not a valid shruken pet box.");
                    }
               }
          }
     }

     [Flippable(0x09A8, 0x0E80)]
     public class ShrinkBox : Item
     {
          private string _PetType;
          private string _PetName;
          private int _PetHue;

          public ShrinkBox(BaseCreature pet) : base(ShrinkTable.Lookup(pet))
          {
               Name = $"a shrunken {pet.Name}";
               _PetType = pet.GetType().FullName;
               _PetName = pet.Name;
               _PetHue = pet.Hue;
               Weight = 1.0;
               Hue = pet.Hue;
          }

          public ShrinkBox(Serial serial) : base(serial)
          {
          }

          public void ReleasePet(Mobile from)
          {
               if (!IsChildOf(from.Backpack))
               {
                    from.SendMessage("That must be in your backpack to use.");
                    return;
               }

               Type petType = Type.GetType(_PetType);
               if (petType == null)
               {
                    from.SendMessage("This pet type cannot be restored.");
                    return;
               }

               if (Activator.CreateInstance(petType) is BaseCreature pet)
               {
                    pet.Controlled = true;
                    pet.ControlMaster = from;
                    pet.Name = _PetName;
                    pet.Hue = _PetHue;
                    pet.MoveToWorld(from.Location, from.Map);
                    from.SendMessage("Your pet has been restored.");
                    Delete();
               }
               else
               {
                    from.SendMessage("Failed to restore pet.");
               }
          }

          public override void OnDoubleClick(Mobile from)
          {
               if (!IsChildOf(from.Backpack))
               {
                    from.SendMessage("That must be in your backpack to use.");
                    return;
               }
               else
               {
                    ReleasePet(from);
               }
          }
     }
}
