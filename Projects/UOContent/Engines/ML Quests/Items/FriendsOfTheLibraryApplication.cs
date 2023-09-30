using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FriendsOfTheLibraryApplication : Item
{
    [Constructible]
    public FriendsOfTheLibraryApplication() : base(0xEC0) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073131; // Friends of the Library Application

    public override bool Nontransferable => true;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        AddQuestItemProperty(list);
    }
}
