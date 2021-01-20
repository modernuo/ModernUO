using System;
using Server.Utilities;

namespace Server.Engines.Quests
{
    public static class QuestSerializer
    {
        public static object Construct(Type type)
        {
            try
            {
                return type.CreateInstance<object>();
            }
            catch
            {
                return null;
            }
        }

        public static void Write(Type type, Type[] referenceTable, IGenericWriter writer)
        {
            if (type == null)
            {
                writer.WriteEncodedInt(0x00);
            }
            else
            {
                for (var i = 0; i < referenceTable.Length; ++i)
                {
                    if (referenceTable[i] == type)
                    {
                        writer.WriteEncodedInt(0x01);
                        writer.WriteEncodedInt(i);
                        return;
                    }
                }

                writer.WriteEncodedInt(0x02);
                writer.Write(type.FullName);
            }
        }

        public static Type ReadType(Type[] referenceTable, IGenericReader reader)
        {
            var encoding = reader.ReadEncodedInt();

            switch (encoding)
            {
                default:
                    {
                        return null;
                    }
                case 0x01: // indexed
                    {
                        var index = reader.ReadEncodedInt();

                        if (index >= 0 && index < referenceTable.Length)
                        {
                            return referenceTable[index];
                        }

                        return null;
                    }
                case 0x02: // by name
                    {
                        var fullName = reader.ReadString();

                        if (fullName == null)
                        {
                            return null;
                        }

                        return AssemblyHandler.FindTypeByFullName(fullName);
                    }
            }
        }

        public static QuestSystem DeserializeQuest(IGenericReader reader)
        {
            var encoding = reader.ReadEncodedInt();

            switch (encoding)
            {
                default:
                    {
                        return null;
                    }
                case 0x01:
                    {
                        var type = ReadType(QuestSystem.QuestTypes, reader);

                        var qs = Construct(type) as QuestSystem;

                        qs?.BaseDeserialize(reader);

                        return qs;
                    }
            }
        }

        public static void Serialize(QuestSystem qs, IGenericWriter writer)
        {
            if (qs == null)
            {
                writer.WriteEncodedInt(0x00);
            }
            else
            {
                writer.WriteEncodedInt(0x01);

                Write(qs.GetType(), QuestSystem.QuestTypes, writer);

                qs.BaseSerialize(writer);
            }
        }

        public static QuestObjective DeserializeObjective(Type[] referenceTable, IGenericReader reader)
        {
            var encoding = reader.ReadEncodedInt();

            switch (encoding)
            {
                default:
                    {
                        return null;
                    }
                case 0x01:
                    {
                        var type = ReadType(referenceTable, reader);

                        var obj = Construct(type) as QuestObjective;

                        obj?.BaseDeserialize(reader);

                        return obj;
                    }
            }
        }

        public static void Serialize(Type[] referenceTable, QuestObjective obj, IGenericWriter writer)
        {
            if (obj == null)
            {
                writer.WriteEncodedInt(0x00);
            }
            else
            {
                writer.WriteEncodedInt(0x01);

                Write(obj.GetType(), referenceTable, writer);

                obj.BaseSerialize(writer);
            }
        }

        public static QuestConversation DeserializeConversation(Type[] referenceTable, IGenericReader reader)
        {
            var encoding = reader.ReadEncodedInt();

            switch (encoding)
            {
                default:
                    {
                        return null;
                    }
                case 0x01:
                    {
                        var type = ReadType(referenceTable, reader);

                        var conv = Construct(type) as QuestConversation;

                        conv?.BaseDeserialize(reader);

                        return conv;
                    }
            }
        }

        public static void Serialize(Type[] referenceTable, QuestConversation conv, IGenericWriter writer)
        {
            if (conv == null)
            {
                writer.WriteEncodedInt(0x00);
            }
            else
            {
                writer.WriteEncodedInt(0x01);

                Write(conv.GetType(), referenceTable, writer);

                conv.BaseSerialize(writer);
            }
        }
    }
}
