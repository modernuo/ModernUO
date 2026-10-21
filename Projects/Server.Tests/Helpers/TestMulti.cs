using Server.Items;

namespace Server.Tests;

/// <summary>
/// A multi with caller-supplied components. The test host never loads MultiData, so a multi built
/// from client data would have no components; without a list this one is empty the same way.
/// </summary>
internal sealed class TestMulti : BaseMulti
{
    private readonly MultiComponentList _components;

    public TestMulti(MultiComponentList components = null) : base(0x1) =>
        _components = components ?? MultiComponentList.Empty;

    public override MultiComponentList Components => _components;
}
