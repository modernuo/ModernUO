using System.Text.Json;

namespace Server.CustomizableStore
{
    internal class StoreItem
    {
        private Item _item;
        private int _price;
        private string _name;
        private string _description;
        private int _stock;
        private int _currency;

        public Item Item { get => _item; set => _item = value; }
        public int Price { get => _price; set => _price = value; }
        public string Name { get => _name; set => _name = value; }
        public string Description { get => _description; set => _description = value; }
        public int Stock { get => _stock; set => _stock = value; }
        public int Currency { get => _currency; set => _currency = value; }

        public StoreItem()
        {
            
        }

        public StoreItem(Item item, int price, string name, string description, int stock = -1, int currency = 0xEED)
        {
            _item = item;
            _price = price;
            _name = name;
            _description = description;
            _stock = stock;
            _currency = currency;

        }

        public StoreItem(IGenericReader reader)
        {
            Deserialize(reader);
        }

        

        public void Serialize(IGenericWriter writer)
        {

            writer.Write(0); // version
            writer.Write(_item);
            writer.Write(_price);
            writer.Write(_name);
            writer.Write(_description);
            writer.Write(_stock);
            writer.Write(_currency);
            

        }
        public void Deserialize(IGenericReader reader)
        {
            int version = reader.ReadInt();
            Item = reader.ReadEntity<Item>();
            _price = reader.ReadInt();
            _name = reader.ReadString();
            _description = reader.ReadString();
            _stock = reader.ReadInt();
            _currency = reader.ReadInt();


        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
