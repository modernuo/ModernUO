using System.Collections.Generic;

namespace Server.Factions;

public class GuardList
{
    public GuardList(GuardDefinition definition)
    {
        Definition = definition;
        Guards = [];
    }

    public GuardDefinition Definition { get; }

    public List<BaseFactionGuard> Guards { get; }

    public BaseFactionGuard Construct()
    {
        try
        {
            return Definition.Type.CreateInstance<BaseFactionGuard>();
        }
        catch
        {
            return null;
        }
    }
}