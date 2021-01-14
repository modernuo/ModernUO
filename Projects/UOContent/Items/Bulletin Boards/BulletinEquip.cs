namespace Server.Items
{
    public struct BulletinEquip
    {
        public int itemID;
        public int hue;

        public BulletinEquip(int itemID, int hue)
        {
            this.itemID = itemID;
            this.hue = hue;
        }
    }
}
