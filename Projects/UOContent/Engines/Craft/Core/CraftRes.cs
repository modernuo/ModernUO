using System;

namespace Server.Engines.Craft
{
    public class CraftRes
    {
        public CraftRes(Type type, TextDefinition name, int amount, TextDefinition message = null)
        {
            ItemType = type;
            Amount = amount;

            Name = name;
            Message = message;
        }

        public Type ItemType { get; }

        public TextDefinition Message { get; }

        public TextDefinition Name { get; }

        public int Amount { get; }

        public void SendMessage(Mobile from)
        {
            if (Message?.Number > 0)
            {
                from.SendLocalizedMessage(Message.Number);
            }
            else if (!string.IsNullOrEmpty(Message?.String))
            {
                from.SendMessage(Message.String);
            }
            else
            {
                from.SendLocalizedMessage(502925); // You don't have the resources required to make that item.
            }
        }
    }
}
