/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2025 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TransferItem.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.Mobiles
{
     internal sealed class TransferItem : Item
     {
          private readonly BaseCreature m_Creature;

          public TransferItem(BaseCreature creature) : base(ShrinkTable.Lookup(creature))
          {
               m_Creature = creature;
               Movable = false;

               if (!Core.AOS)
               {
                    Name = creature.Name;
               }
               else if (ItemID == ShrinkTable.DefaultItemID 
                    || creature.GetType().IsDefined(typeof(FriendlyNameAttribute), false) 
                    || creature is Reptalon)
               {
                    Name = FriendlyNameAttribute.GetFriendlyNameFor(creature.GetType()).ToString();
               }

               Hue = creature.Hue & 0x0FFF;
          }

          public TransferItem(Serial serial) : base(serial)
          {
          }

          public static bool IsInCombat(BaseCreature creature)
          {
               return (creature?.Aggressors.Count > 0) || (creature?.Aggressed.Count > 0);
          }

          public override void Serialize(IGenericWriter writer)
          {
               base.Serialize(writer);
               writer.Write(0); // version
          }

          public override void Deserialize(IGenericReader reader)
          {
               base.Deserialize(reader);
               reader.ReadInt(); // version
               Delete();
          }

          public override void GetProperties(IPropertyList list)
          {
               base.GetProperties(list);
               list.Add(1041603); // This item represents a pet currently in consideration for trade
               list.Add(1041601, m_Creature.Name); // Pet Name: ~1_val~

               if (m_Creature.ControlMaster != null)
               {
                    list.Add(1041602, m_Creature.ControlMaster.Name); // Owner: ~1_val~
               }
          }

          public override bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
          {
               if (!base.AllowSecureTrade(from, to, newOwner, accepted) || IsInvalidTrade(from, to))
               {
                    return false;
               }

               if (!accepted)
               {
                    return true;
               }

               return HandleAcceptedTrade(from, to);
          }

          private bool IsInvalidTrade(Mobile from, Mobile to)
          {
               return Deleted
                    || m_Creature?.Deleted != false
                    || m_Creature.ControlMaster != from
                    || !from.CheckAlive()
                    || !to.CheckAlive()
                    || from.Map != m_Creature.Map
                    || !from.InRange(m_Creature, 14);
          }

          private bool HandleAcceptedTrade(Mobile from, Mobile to)
          {
               if (!ValidateYoungStatus(from, to))
               {
                    return false;
               }

               if (!ValidateControlStatus(from, to))
               {
                    return false;
               }

               if (!ValidateFollowerLimit(to))
               {
                    return false;
               }

               if (IsInCombat(m_Creature))
               {
                    return false;
               }

               return true;
          }

          private bool ValidateFollowerLimit(Mobile to)
          {
               if (to.Followers + m_Creature.ControlSlots > to.FollowersMax)
               {
                    to.SendLocalizedMessage(1049607);
                    // You have too many followers to control that creature.
                    return false;
               }

               return true;
          }

          private static bool ValidateYoungStatus(Mobile from, Mobile to)
          {
               bool youngFrom = from is PlayerMobile mobile && mobile.Young;
               bool youngTo = to is PlayerMobile playerMobile && playerMobile.Young;

               if (youngFrom && !youngTo)
               {
                    from.SendLocalizedMessage(502051);
                    // As a young player, you may not transfer pets to older players.
                    return false;
               }

               if (!youngFrom && youngTo)
               {
                    from.SendLocalizedMessage(502052);
                    // As an older player, you may not transfer pets to young players.
                    return false;
               }

               return true;
          }

          private bool ValidateControlStatus(Mobile from, Mobile to)
          {
               if (!m_Creature.CanBeControlledBy(to))
               {
                    SendTransferRefusalMessages(from, to, 1043248, 1043249);
                    // The pet refuses to be transferred because it will not obey ~1_NAME~.~3_BLANK~
                    // The pet will not accept you as a master because it does not trust you.~3_BLANK~
                    return false;
               }

               if (!m_Creature.CanBeControlledBy(from))
               {
                    SendTransferRefusalMessages(from, to, 1043250, 1043251);
                    // The pet refuses to be transferred because it will not obey you sufficiently.~3_BLANK~
                    // The pet will not accept you as a master because it does not trust ~2_NAME~.~3_BLANK~
                    return false;
               }

               return true;
          }

          private static void SendTransferRefusalMessages(Mobile from, Mobile to, int fromMessage, int toMessage)
          {
               string args = $"{to.Name}\t{from.Name}\t ";
               from.SendLocalizedMessage(fromMessage, args);
               to.SendLocalizedMessage(toMessage, args);
          }

          public override void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
          {
               if (Deleted || IsInvalidTrade(from, to))
               {
                    Delete();
                    return;
               }

               Delete();

               if (!accepted || !m_Creature.SetControlMaster(to))
               {
                    return;
               }

               TransferPetOwnership(from, to);
          }

          private void TransferPetOwnership(Mobile from, Mobile to)
          {
               if (m_Creature.Summoned)
               {
                    m_Creature.SummonMaster = to;
               }

               m_Creature.ControlTarget = to;
               m_Creature.ControlOrder = OrderType.Follow;
               m_Creature.BondingBegin = DateTime.MinValue;
               m_Creature.OwnerAbandonTime = DateTime.MinValue;
               m_Creature.IsBonded = false;
               m_Creature.PlaySound(m_Creature.GetIdleSound());

               string args = $"{from.Name}\t{m_Creature.Name}\t{to.Name}";
               from.SendLocalizedMessage(1043253, args);
               // You have transferred your pet to ~3_GETTER~.
               to.SendLocalizedMessage(1043252, args);
               // ~1_NAME~ has transferred the allegiance of ~2_PET_NAME~ to you.
          }
     }
}