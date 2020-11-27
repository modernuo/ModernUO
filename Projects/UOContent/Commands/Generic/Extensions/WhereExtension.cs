using System;

namespace Server.Commands.Generic
{
    public sealed class WhereExtension : BaseExtension
    {
        public static ExtensionInfo ExtInfo = new(20, "Where", -1, () => new WhereExtension());

        public override ExtensionInfo Info => ExtInfo;

        public ObjectConditional Conditional { get; private set; }

        public static void Initialize()
        {
            ExtensionInfo.Register(ExtInfo);
        }

        public override void Optimize(Mobile from, Type baseType, ref AssemblyEmitter assembly)
        {
            if (baseType == null)
            {
                throw new InvalidOperationException("Insanity.");
            }

            Conditional.Compile(ref assembly);
        }

        public override void Parse(Mobile from, string[] arguments, int offset, int size)
        {
            if (size < 1)
            {
                throw new Exception("Invalid condition syntax.");
            }

            Conditional = ObjectConditional.ParseDirect(from, arguments, offset, size);
        }

        public override bool IsValid(object obj) => Conditional.CheckCondition(obj);
    }
}
