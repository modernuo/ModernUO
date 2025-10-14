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

using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Commands
{
     public class PetShrink
     {
          public static void Initialize()
          {    // command = [shrink
               // change access level here (premium setup)
               CommandSystem.Register("shrink", AccessLevel.Owner, new CommandEventHandler(Shrink_OnCommand));
          }

          [Usage("shrink")] // target again or double click to restore
          [Description("Shrinks a targeted pet into an item.")]
          public static void Shrink_OnCommand(CommandEventArgs e)
          {
               e.Mobile.SendMessage("Target the pet you wish to shrink.");
               e.Mobile.Target = new ShrinkTarget();
          }

          private class ShrinkTarget : Target
          {
               public ShrinkTarget() : base(3, false, TargetFlags.None)
               {
               }

               protected override void OnTarget(Mobile from, object targeted)
               {
                    if (targeted is BaseCreature pet && pet.Controlled && pet.ControlMaster == from)
                    {
                         if (from is PlayerMobile)
                         {
                              pet.Controlled = false;
                              pet.ControlMaster = null;
                              pet.Internalize();
                         }

                         var shrinkItem = new ShrinkItem(pet.Serial)
                         {
                              Name = pet.Name,
                              Hue = pet.Hue
                         };

                         from.AddToBackpack(shrinkItem);
                         from.SendMessage("Your pet was shrunk to a statuette.");
                    }
                    else
                    {
                         from.SendMessage("That is not your pet.");
                    }
               }
          }
     }
}