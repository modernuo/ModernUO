using System;
using System.Collections.Generic;
using Server.Buffers;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items
{
    public class BaseBook : Item, ISecurable
    {
        private string m_Author;
        private string m_Title;

        [Constructible]
        public BaseBook(int itemID, int pageCount = 20, bool writable = true) : this(itemID, null, null, pageCount, writable)
        {
        }

        [Constructible]
        public BaseBook(int itemID, string title, string author, int pageCount, bool writable) : base(itemID)
        {
            var content = DefaultContent;

            m_Title = title ?? content?.Title;
            m_Author = author ?? content?.Author;
            Writable = writable;

            if (content == null)
            {
                Pages = new BookPageInfo[pageCount];

                for (var i = 0; i < Pages.Length; ++i)
                {
                    Pages[i] = new BookPageInfo();
                }
            }
            else
            {
                Pages = content.Copy();
            }
        }

        // Intended for defined books only
        public BaseBook(int itemID, bool writable) : this(itemID, 0, writable)
        {
        }

        public BaseBook(Serial serial) : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Author
        {
            get => m_Author;
            set
            {
                m_Author = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Writable { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PagesCount => Pages.Length;

        public BookPageInfo[] Pages { get; protected set; }

        public virtual BookContent DefaultContent => null;

        public string ContentAsString
        {
            get
            {
                using var sb = new ValueStringBuilder(stackalloc char[256]);

                foreach (var bpi in Pages)
                {
                    foreach (var line in bpi.Lines)
                    {
                        sb.AppendLine(line);
                    }
                }

                return sb.ToString();
            }
        }

        public string[] ContentAsStringArray
        {
            get
            {
                var lines = new List<string>();

                foreach (var bpi in Pages)
                {
                    lines.AddRange(bpi.Lines);
                }

                return lines.ToArray();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level { get; set; }

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            var content = DefaultContent;

            var flags = SaveFlags.None;

            if (m_Title != content?.Title)
            {
                flags |= SaveFlags.Title;
            }

            if (m_Author != content?.Author)
            {
                flags |= SaveFlags.Author;
            }

            if (Writable)
            {
                flags |= SaveFlags.Writable;
            }

            if (content?.IsMatch(Pages) != true)
            {
                flags |= SaveFlags.Content;
            }

            writer.Write(4); // version

            writer.Write((int)Level);

            writer.Write((byte)flags);

            if ((flags & SaveFlags.Title) != 0)
            {
                writer.Write(m_Title);
            }

            if ((flags & SaveFlags.Author) != 0)
            {
                writer.Write(m_Author);
            }

            if ((flags & SaveFlags.Content) != 0)
            {
                writer.WriteEncodedInt(Pages.Length);

                for (var i = 0; i < Pages.Length; ++i)
                {
                    Pages[i].Serialize(writer);
                }
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        Level = (SecureLevel)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                case 2:
                    {
                        var content = DefaultContent;

                        var flags = (SaveFlags)reader.ReadByte();

                        if ((flags & SaveFlags.Title) != 0)
                        {
                            m_Title = Utility.Intern(reader.ReadString());
                        }
                        else if (content != null)
                        {
                            m_Title = content.Title;
                        }

                        if ((flags & SaveFlags.Author) != 0)
                        {
                            m_Author = reader.ReadString();
                        }
                        else if (content != null)
                        {
                            m_Author = content.Author;
                        }

                        Writable = (flags & SaveFlags.Writable) != 0;

                        if ((flags & SaveFlags.Content) != 0)
                        {
                            Pages = new BookPageInfo[reader.ReadEncodedInt()];

                            for (var i = 0; i < Pages.Length; ++i)
                            {
                                Pages[i] = new BookPageInfo(reader);
                            }
                        }
                        else
                        {
                            Pages = content?.Copy() ?? Array.Empty<BookPageInfo>();
                        }

                        break;
                    }
                case 1:
                case 0:
                    {
                        m_Title = reader.ReadString();
                        m_Author = reader.ReadString();
                        Writable = reader.ReadBool();

                        if (version == 0 || reader.ReadBool())
                        {
                            Pages = new BookPageInfo[reader.ReadInt()];

                            for (var i = 0; i < Pages.Length; ++i)
                            {
                                Pages[i] = new BookPageInfo(reader);
                            }
                        }
                        else
                        {
                            var content = DefaultContent;

                            Pages = content?.Copy() ?? Array.Empty<BookPageInfo>();
                        }

                        break;
                    }
            }

            if (version < 3 && (Weight == 1 || Weight == 2))
            {
                Weight = -1;
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (!string.IsNullOrEmpty(m_Title))
            {
                list.Add(m_Title);
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        /*public override void GetProperties( ObjectPropertyList list )
        {
          base.GetProperties( list );

          if (m_Title?.Length > 0)
            list.Add( 1060658, "Title\t{0}", m_Title ); // ~1_val~: ~2_val~

          if (m_Author?.Length > 0)
            list.Add( 1060659, "Author\t{0}", m_Author ); // ~1_val~: ~2_val~

          if (m_Pages?.Length > 0)
            list.Add( 1060660, "Pages\t{0}", m_Pages.Length ); // ~1_val~: ~2_val~
        }*/

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "{0} by {1}", m_Title, m_Author);
            LabelTo(from, "[{0} pages]", Pages.Length);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Title == null && m_Author == null && Writable)
            {
                Title = "a book";
                Author = from.Name;
            }

            from.NetState.SendBookCover(from, this);
            from.NetState.SendBookContent(this);
        }

        [Flags]
        private enum SaveFlags
        {
            None = 0x00,
            Title = 0x01,
            Author = 0x02,
            Writable = 0x04,
            Content = 0x08
        }
    }
}
