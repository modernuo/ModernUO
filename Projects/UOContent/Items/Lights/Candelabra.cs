using System;

namespace Server.Items
{
    public class Candelabra : BaseLight, IShipwreckedItem
    {
        [Constructible]
        public Candelabra() : base(0xA27)
        {
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle225;
            Weight = 3.0;
        }

        public Candelabra(Serial serial) : base(serial)
        {
        }

        public override int LitItemID => 0xB1D;
        public override int UnlitItemID => 0xA27;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShipwreckedItem { get; set; }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(IsShipwreckedItem);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        IsShipwreckedItem = reader.ReadBool();
                        break;
                    }
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (IsShipwreckedItem)
            {
                list.Add(1041645); // recovered from a shipwreck
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (IsShipwreckedItem)
            {
                LabelTo(from, 1041645); // recovered from a shipwreck
            }
        }
    }
}
