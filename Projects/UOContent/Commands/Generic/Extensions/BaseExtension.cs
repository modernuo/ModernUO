using System;
using System.Collections.Generic;

namespace Server.Commands.Generic
{
    public delegate BaseExtension ExtensionConstructor();

    public sealed class ExtensionInfo
    {
        public ExtensionInfo(int order, string name, int size, ExtensionConstructor constructor)
        {
            Name = name;
            Size = size;

            Order = order;

            Constructor = constructor;
        }

        public static Dictionary<string, ExtensionInfo> Table { get; } =
            new(StringComparer.InvariantCultureIgnoreCase);

        public int Order { get; }

        public string Name { get; }

        public int Size { get; }

        public bool IsFixedSize => Size >= 0;

        public ExtensionConstructor Constructor { get; }

        public static void Register(ExtensionInfo ext)
        {
            Table[ext.Name] = ext;
        }
    }

    public sealed class Extensions : List<BaseExtension>
    {
        public bool IsValid(object obj)
        {
            for (var i = 0; i < Count; ++i)
            {
                if (!this[i].IsValid(obj))
                {
                    return false;
                }
            }

            return true;
        }

        public void Filter(List<object> list)
        {
            for (var i = 0; i < Count; ++i)
            {
                this[i].Filter(list);
            }
        }

        public static Extensions Parse(Mobile from, ref string[] args)
        {
            var parsed = new Extensions();

            var size = args.Length;

            Type baseType = null;

            for (var i = args.Length - 1; i >= 0; --i)
            {
                if (!ExtensionInfo.Table.TryGetValue(args[i], out var extInfo))
                {
                    continue;
                }

                if (extInfo.IsFixedSize && i != size - extInfo.Size - 1)
                {
                    throw new Exception("Invalid extended argument count.");
                }

                var ext = extInfo.Constructor();

                ext.Parse(from, args, i + 1, size - i - 1);

                if (ext is WhereExtension extension)
                {
                    baseType = extension.Conditional.Type;
                }

                parsed.Add(ext);

                size = i;
            }

            parsed.Sort((a, b) => a.Order - b.Order);

            AssemblyEmitter emitter = null;

            foreach (var update in parsed)
            {
                update.Optimize(from, baseType, ref emitter);
            }

            if (size != args.Length)
            {
                var old = args;
                args = new string[size];

                for (var i = 0; i < args.Length; ++i)
                {
                    args[i] = old[i];
                }
            }

            return parsed;
        }
    }

    public abstract class BaseExtension
    {
        public abstract ExtensionInfo Info { get; }

        public string Name => Info.Name;

        public int Size => Info.Size;

        public bool IsFixedSize => Info.IsFixedSize;

        public int Order => Info.Order;

        public virtual void Optimize(Mobile from, Type baseType, ref AssemblyEmitter assembly)
        {
        }

        public virtual void Parse(Mobile from, string[] arguments, int offset, int size)
        {
        }

        public virtual bool IsValid(object obj) => true;

        public virtual void Filter(List<object> list)
        {
        }
    }
}
