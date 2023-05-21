using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EmptyJar : Item
{
    [Constructible]
    public EmptyJar() : base(0x1005)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class EmptyJars : Item
{
    [Constructible]
    public EmptyJars() : base(0xe44)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class EmptyJars2 : Item
{
    [Constructible]
    public EmptyJars2() : base(0xe45)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class EmptyJars3 : Item
{
    [Constructible]
    public EmptyJars3() : base(0xe46)
    {
        Movable = true;
        Stackable = false;
    }
}

[SerializationGenerator(0, false)]
public partial class EmptyJars4 : Item
{
    [Constructible]
    public EmptyJars4() : base(0xe47)
    {
        Movable = true;
        Stackable = false;
    }
}
