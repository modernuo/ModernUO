using System.Collections.Generic;
using Server.Utilities;

namespace Server.Factions
{
    public class GuardList
    {
        public GuardList(GuardDefinition definition)
        {
            Definition = definition;
            Guards = new List<BaseFactionGuard>();
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
}
