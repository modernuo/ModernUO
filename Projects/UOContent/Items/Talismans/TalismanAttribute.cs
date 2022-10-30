using System;

namespace Server.Items
{
    [PropertyObject]
    public class TalismanAttribute
    {
        public TalismanAttribute() : this(null, 0)
        {
        }

        public TalismanAttribute(TalismanAttribute copy)
        {
            if (copy != null)
            {
                Type = copy.Type;
                Name = copy.Name;
                Amount = copy.Amount;
            }
        }

        public TalismanAttribute(Type type, TextDefinition name, int amount = 0)
        {
            Type = type;
            Name = name;
            Amount = amount;
        }

        public TalismanAttribute(IGenericReader reader)
        {
            var version = reader.ReadInt();

            var flags = (SaveFlag)reader.ReadEncodedInt();

            if (GetSaveFlag(flags, SaveFlag.Type))
            {
                Type = AssemblyHandler.FindTypeByFullName(reader.ReadString());
            }

            if (GetSaveFlag(flags, SaveFlag.Name))
            {
                Name = reader.ReadTextDefinition();
            }

            if (GetSaveFlag(flags, SaveFlag.Amount))
            {
                Amount = reader.ReadEncodedInt();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Type Type { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextDefinition Name { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Amount { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty => Type == null;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsItem => Type?.Namespace == "Server.Items";

        public override string ToString() => Type?.Name ?? "None";

        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool setIf)
        {
            if (setIf)
            {
                flags |= toSet;
            }
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet) => (flags & toGet) != 0;

        public virtual void Serialize(IGenericWriter writer)
        {
            writer.Write(0); // version

            var flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Type, Type != null);
            SetSaveFlag(ref flags, SaveFlag.Name, Name != null);
            SetSaveFlag(ref flags, SaveFlag.Amount, Amount != 0);

            writer.WriteEncodedInt((int)flags);

            if (GetSaveFlag(flags, SaveFlag.Type))
            {
                writer.Write(Type!.FullName);
            }

            if (GetSaveFlag(flags, SaveFlag.Name))
            {
                writer.Write(Name);
            }

            if (GetSaveFlag(flags, SaveFlag.Amount))
            {
                writer.WriteEncodedInt(Amount);
            }
        }

        public int DamageBonus(Mobile to) => to?.GetType() == Type ? Amount : 0;

        [Flags]
        private enum SaveFlag
        {
            None = 0x00000000,
            Type = 0x00000001,
            Name = 0x00000002,
            Amount = 0x00000004
        }
    }
}
