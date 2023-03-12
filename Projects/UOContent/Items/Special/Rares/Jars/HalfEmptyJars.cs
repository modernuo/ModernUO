using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HalfEmptyJar : Item
{
    [Constructible]
    public HalfEmptyJar() : base(0x1007)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class HalfEmptyJars : Item
{
    [Constructible]
    public HalfEmptyJars() : base(0xe4c)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class Jars2 : Item
{
    [Constructible]
    public Jars2() : base(0xE4d)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class Jars3 : Item
{
    [Constructible]
    public Jars3() : base(0xE4e)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class Jars4 : Item
{
    [Constructible]
    public Jars4() : base(0xE4f)
    {
        Movable = true;
        Stackable = false;
    }
}
