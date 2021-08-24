namespace Server.Items
{
    public struct BulletinEquip
    {
        public int _itemID;
        public int _hue;

        public BulletinEquip(int itemID, int hue)
        {
            _itemID = itemID;
            _hue = hue;
        }

        public BulletinEquip(IGenericReader reader)
        {
            _itemID = reader.ReadInt();
            _hue = reader.ReadInt();
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(_itemID);
            writer.Write(_hue);
        }
    }
}
