using Server.ContextMenus;
using Server.Menus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;

namespace Server.Network
{
    public sealed class DisplayItemListMenu : Packet
    {
        public DisplayItemListMenu(ItemListMenu menu) : base(0x7C)
        {
            EnsureCapacity(256);

            Stream.Write(((IMenu)menu).Serial);
            Stream.Write((short)0);

            var question = menu.Question;

            if (question == null)
            {
                Stream.Write((byte)0);
            }
            else
            {
                var questionLength = question.Length;
                Stream.Write((byte)questionLength);
                Stream.WriteAsciiFixed(question, questionLength);
            }

            var entries = menu.Entries;

            int entriesLength = (byte)entries.Length;

            Stream.Write((byte)entriesLength);

            for (var i = 0; i < entriesLength; ++i)
            {
                var e = entries[i];

                Stream.Write((ushort)e.ItemID);
                Stream.Write((short)e.Hue);

                var name = e.Name;

                if (name == null)
                {
                    Stream.Write((byte)0);
                }
                else
                {
                    var nameLength = name.Length;
                    Stream.Write((byte)nameLength);
                    Stream.WriteAsciiFixed(name, nameLength);
                }
            }
        }
    }

    public sealed class DisplayQuestionMenu : Packet
    {
        public DisplayQuestionMenu(QuestionMenu menu) : base(0x7C)
        {
            EnsureCapacity(256);

            Stream.Write(((IMenu)menu).Serial);
            Stream.Write((short)0);

            var question = menu.Question;

            if (question == null)
            {
                Stream.Write((byte)0);
            }
            else
            {
                var questionLength = question.Length;
                Stream.Write((byte)questionLength);
                Stream.WriteAsciiFixed(question, questionLength);
            }

            var answers = menu.Answers;

            int answersLength = (byte)answers.Length;

            Stream.Write((byte)answersLength);

            for (var i = 0; i < answersLength; ++i)
            {
                Stream.Write(0);

                var answer = answers[i];

                if (answer == null)
                {
                    Stream.Write((byte)0);
                }
                else
                {
                    var answerLength = answer.Length;
                    Stream.Write((byte)answerLength);
                    Stream.WriteAsciiFixed(answer, answerLength);
                }
            }
        }
    }

    public sealed class DisplayContextMenu : Packet
    {
        public DisplayContextMenu(ContextMenu menu) : base(0xBF)
        {
            var entries = menu.Entries;

            int length = (byte)entries.Length;

            EnsureCapacity(12 + length * 8);

            Stream.Write((short)0x14);
            Stream.Write((short)0x02);

            var target = menu.Target;

            Stream.Write(target.Serial);

            Stream.Write((byte)length);

            var p = target switch
            {
                Mobile _  => target.Location,
                Item item => item.GetWorldLocation(),
                _         => Point3D.Zero
            };

            for (var i = 0; i < length; ++i)
            {
                var e = entries[i];

                Stream.Write(e.Number);
                Stream.Write((short)i);

                var range = e.Range;

                if (range == -1)
                {
                    range = Core.GlobalUpdateRange;
                }

                var flags = e.Flags;
                if (!(e.Enabled && menu.From.InRange(p, range)))
                {
                    flags |= CMEFlags.Disabled;
                }

                Stream.Write((short)flags);
            }
        }
    }

    public sealed class DisplayContextMenuOld : Packet
    {
        public DisplayContextMenuOld(ContextMenu menu) : base(0xBF)
        {
            var entries = menu.Entries;

            int length = (byte)entries.Length;

            EnsureCapacity(12 + length * 8);

            Stream.Write((short)0x14);
            Stream.Write((short)0x01);

            var target = menu.Target;

            Stream.Write(target.Serial);

            Stream.Write((byte)length);

            var p = target switch
            {
                Mobile _  => target.Location,
                Item item => item.GetWorldLocation(),
                _         => Point3D.Zero
            };

            for (var i = 0; i < length; ++i)
            {
                var e = entries[i];

                Stream.Write((short)i);
                Stream.Write((ushort)(e.Number - 3000000));

                var range = e.Range;

                if (range == -1)
                {
                    range = Core.GlobalUpdateRange;
                }

                var flags = e.Flags;
                if (!(e.Enabled && menu.From.InRange(p, range)))
                {
                    flags |= CMEFlags.Disabled;
                }

                var color = e.Color & 0xFFFF;

                if (color != 0xFFFF)
                {
                    flags |= CMEFlags.Colored;
                }

                Stream.Write((short)flags);

                if ((flags & CMEFlags.Colored) != 0)
                {
                    Stream.Write((short)color);
                }
            }
        }
    }
}
