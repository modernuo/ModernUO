using System;

namespace Server.Engines.BulkOrders
{
    public class BOBLargeSubEntry
    {
        public BOBLargeSubEntry(LargeBulkEntry lbe)
        {
            ItemType = lbe.Details.Type;
            AmountCur = lbe.Amount;
            Number = lbe.Details.Number;
            Graphic = lbe.Details.Graphic;
        }

        public BOBLargeSubEntry(IGenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        string type = reader.ReadString();

                        if (type != null)
                            ItemType = AssemblyHandler.FindFirstTypeForName(type);

                        AmountCur = reader.ReadEncodedInt();
                        Number = reader.ReadEncodedInt();
                        Graphic = reader.ReadEncodedInt();

                        break;
                    }
            }
        }

        public Type ItemType { get; }

        public int AmountCur { get; }

        public int Number { get; }

        public int Graphic { get; }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(ItemType?.FullName);

            writer.WriteEncodedInt(AmountCur);
            writer.WriteEncodedInt(Number);
            writer.WriteEncodedInt(Graphic);
        }
    }
}
