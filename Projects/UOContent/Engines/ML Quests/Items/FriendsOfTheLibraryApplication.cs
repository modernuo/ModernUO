namespace Server.Items
{
    public class FriendsOfTheLibraryApplication : Item
    {
        [Constructible]
        public FriendsOfTheLibraryApplication() : base(0xEC0) => LootType = LootType.Blessed;

        public FriendsOfTheLibraryApplication(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1073131; // Friends of the Library Application

        public override bool Nontransferable => true;

        public override void AddNameProperties(IPropertyList list)
        {
            base.AddNameProperties(list);
            AddQuestItemProperty(list);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // Version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
