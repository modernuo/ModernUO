using Server.Gumps;

namespace Server.Tests.Gumps;

public sealed class EmptyLegacyTestGump : Gump
{
    public bool HasVisualElementsForTest => HasVisualElements;

    public EmptyLegacyTestGump() : base(0, 0)
    {
    }
}

public sealed class EmptyDynamicTestGump : DynamicGump
{
    public bool HasVisualElementsForTest => HasVisualElements;

    public EmptyDynamicTestGump() : base(0, 0)
    {
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
    }
}

public sealed class EmptyStaticTestGump : StaticGump<EmptyStaticTestGump>
{
    public bool HasVisualElementsForTest => HasVisualElements;

    public EmptyStaticTestGump() : base(0, 0)
    {
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.SetNoClose();
    }
}
