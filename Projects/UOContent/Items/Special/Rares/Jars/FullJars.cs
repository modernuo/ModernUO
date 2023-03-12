using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DecoFullJar : Item
{
    [Constructible]
    public DecoFullJar() : base(0x1006)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class DecoFullJars3 : Item
{
    [Constructible]
    public DecoFullJars3() : base(0xE4a)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class DecoFullJars4 : Item
{
    [Constructible]
    public DecoFullJars4() : base(0xE4b)
    {
        Movable = true;
        Stackable = false;
    }
}
