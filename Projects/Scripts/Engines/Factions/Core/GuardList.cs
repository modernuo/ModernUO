using Server.Utilities;
using System;
using System.Collections.Generic;

namespace Server.Factions
{
  public class GuardList
  {
    public GuardList(GuardDefinition definition)
    {
      Definition = definition;
      Guards = new List<BaseFactionGuard>();
    }

    public GuardDefinition Definition{ get; }

    public List<BaseFactionGuard> Guards{ get; }

    public BaseFactionGuard Construct()
    {
      try
      {
        return ActivatorUtil.CreateInstance(Definition.Type) as BaseFactionGuard;
      }
      catch
      {
        return null;
      }
    }
  }
}
