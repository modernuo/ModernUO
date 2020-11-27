using System;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public sealed class LimitExtension : BaseExtension
    {
        public static ExtensionInfo ExtInfo = new(80, "Limit", 1, () => new LimitExtension());

        public override ExtensionInfo Info => ExtInfo;

        public int Limit { get; private set; }

        public static void Initialize()
        {
            ExtensionInfo.Register(ExtInfo);
        }

        public override void Parse(Mobile from, string[] arguments, int offset, int size)
        {
            Limit = Utility.ToInt32(arguments[offset]);

            if (Limit < 0)
            {
                throw new Exception("Limit cannot be less than zero.");
            }
        }

        public override void Filter(List<object> list)
        {
            if (list.Count > Limit)
            {
                list.RemoveRange(Limit, list.Count - Limit);
            }
        }
    }
}
