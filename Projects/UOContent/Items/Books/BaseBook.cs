using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items
{
    public class BookPageInfo
    {
        public BookPageInfo() => Lines = Array.Empty<string>();

        public BookPageInfo(params string[] lines) => Lines = lines;

        public BookPageInfo(IGenericReader reader)
        {
            int length = reader.ReadInt();

            Lines = new string[length];

            for (int i = 0; i < Lines.Length; ++i)
                Lines[i] = Utility.Intern(reader.ReadString());
        }

        public string[] Lines { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Lines.Length);

            for (int i = 0; i < Lines.Length; ++i)
                writer.Write(Lines[i]);
        }
    }

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
            BookContent content = DefaultContent;

            m_Title = title ?? content?.Title;
            m_Author = author ?? content?.Author;
            Writable = writable;

            if (content == null)
            {
                Pages = new BookPageInfo[pageCount];

                for (int i = 0; i < Pages.Length; ++i)
                    Pages[i] = new BookPageInfo();
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

        public BookPageInfo[] Pages { get; private set; }

        public virtual BookContent DefaultContent => null;

        public string ContentAsString
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (BookPageInfo bpi in Pages)
                    foreach (string line in bpi.Lines)
                        sb.AppendLine(line);

                return sb.ToString();
            }
        }

        public string[] ContentAsStringArray
        {
            get
            {
                List<string> lines = new List<string>();

                foreach (BookPageInfo bpi in Pages) lines.AddRange(bpi.Lines);

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

            BookContent content = DefaultContent;

            SaveFlags flags = SaveFlags.None;

            if (m_Title != content?.Title)
                flags |= SaveFlags.Title;

            if (m_Author != content?.Author)
                flags |= SaveFlags.Author;

            if (Writable)
                flags |= SaveFlags.Writable;

            if (content?.IsMatch(Pages) != true)
                flags |= SaveFlags.Content;

            writer.Write(4); // version

            writer.Write((int)Level);

            writer.Write((byte)flags);

            if ((flags & SaveFlags.Title) != 0)
                writer.Write(m_Title);

            if ((flags & SaveFlags.Author) != 0)
                writer.Write(m_Author);

            if ((flags & SaveFlags.Content) != 0)
            {
                writer.WriteEncodedInt(Pages.Length);

                for (int i = 0; i < Pages.Length; ++i)
                    Pages[i].Serialize(writer);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

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
                        BookContent content = DefaultContent;

                        SaveFlags flags = (SaveFlags)reader.ReadByte();

                        if ((flags & SaveFlags.Title) != 0)
                            m_Title = Utility.Intern(reader.ReadString());
                        else if (content != null)
                            m_Title = content.Title;

                        if ((flags & SaveFlags.Author) != 0)
                            m_Author = reader.ReadString();
                        else if (content != null)
                            m_Author = content.Author;

                        Writable = (flags & SaveFlags.Writable) != 0;

                        if ((flags & SaveFlags.Content) != 0)
                        {
                            Pages = new BookPageInfo[reader.ReadEncodedInt()];

                            for (int i = 0; i < Pages.Length; ++i)
                                Pages[i] = new BookPageInfo(reader);
                        }
                        else
                        {
                            if (content != null)
                                Pages = content.Copy();
                            else
                                Pages = Array.Empty<BookPageInfo>();
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

                            for (int i = 0; i < Pages.Length; ++i)
                                Pages[i] = new BookPageInfo(reader);
                        }
                        else
                        {
                            BookContent content = DefaultContent;

                            if (content != null)
                                Pages = content.Copy();
                            else
                                Pages = Array.Empty<BookPageInfo>();
                        }

                        break;
                    }
            }

            if (version < 3 && (Weight == 1 || Weight == 2))
                Weight = -1;
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (!string.IsNullOrEmpty(m_Title))
                list.Add(m_Title);
            else
                base.AddNameProperty(list);
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

            from.Send(new BookHeader(from, this));
            from.Send(new BookPageDetails(this));
        }

        public static void Initialize()
        {
            PacketHandlers.Register(0xD4, 0, true, HeaderChange);
            PacketHandlers.Register(0x66, 0, true, ContentChange);
            PacketHandlers.Register(0x93, 99, true, OldHeaderChange);
        }

        public static void OldHeaderChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;

            if (!(World.FindItem(pvSrc.ReadUInt32()) is BaseBook book) || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
                return;

            pvSrc.Seek(4, SeekOrigin.Current); // Skip flags and page count

            string title = pvSrc.ReadStringSafe(60);
            string author = pvSrc.ReadStringSafe(30);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void HeaderChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;

            if (!(World.FindItem(pvSrc.ReadUInt32()) is BaseBook book) || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
                return;

            pvSrc.Seek(4, SeekOrigin.Current); // Skip flags and page count

            int titleLength = pvSrc.ReadUInt16();

            if (titleLength > 60)
                return;

            string title = pvSrc.ReadUTF8StringSafe(titleLength);

            int authorLength = pvSrc.ReadUInt16();

            if (authorLength > 30)
                return;

            string author = pvSrc.ReadUTF8StringSafe(authorLength);

            book.Title = Utility.FixHtml(title);
            book.Author = Utility.FixHtml(author);
        }

        public static void ContentChange(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;

            if (!(World.FindItem(pvSrc.ReadUInt32()) is BaseBook book) || !book.Writable ||
                !from.InRange(book.GetWorldLocation(), 1) || !book.IsAccessibleTo(from))
                return;

            int pageCount = pvSrc.ReadUInt16();

            if (pageCount > book.PagesCount)
                return;

            for (int i = 0; i < pageCount; ++i)
            {
                int index = pvSrc.ReadUInt16();

                if (index >= 1 && index <= book.PagesCount)
                {
                    --index;

                    int lineCount = pvSrc.ReadUInt16();

                    if (lineCount <= 8)
                    {
                        string[] lines = new string[lineCount];

                        for (int j = 0; j < lineCount; ++j)
                            if ((lines[j] = pvSrc.ReadUTF8StringSafe()).Length >= 80)
                                return;

                        book.Pages[index].Lines = lines;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
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

    public sealed class BookPageDetails : Packet
    {
        public BookPageDetails(BaseBook book) : base(0x66)
        {
            EnsureCapacity(256);

            Stream.Write(book.Serial);
            Stream.Write((ushort)book.PagesCount);

            for (int i = 0; i < book.PagesCount; ++i)
            {
                BookPageInfo page = book.Pages[i];

                Stream.Write((ushort)(i + 1));
                Stream.Write((ushort)page.Lines.Length);

                for (int j = 0; j < page.Lines.Length; ++j)
                {
                    byte[] buffer = Utility.UTF8.GetBytes(page.Lines[j]);

                    Stream.Write(buffer, 0, buffer.Length);
                    Stream.Write((byte)0);
                }
            }
        }
    }

    public sealed class BookHeader : Packet
    {
        public BookHeader(Mobile from, BaseBook book) : base(0xD4)
        {
            string title = book.Title ?? "";
            string author = book.Author ?? "";

            byte[] titleBuffer = Utility.UTF8.GetBytes(title);
            byte[] authorBuffer = Utility.UTF8.GetBytes(author);

            EnsureCapacity(15 + titleBuffer.Length + authorBuffer.Length);

            Stream.Write(book.Serial);
            Stream.Write(true);
            Stream.Write(book.Writable && from.InRange(book.GetWorldLocation(), 1));
            Stream.Write((ushort)book.PagesCount);

            Stream.Write((ushort)(titleBuffer.Length + 1));
            Stream.Write(titleBuffer, 0, titleBuffer.Length);
            Stream.Write((byte)0); // terminate

            Stream.Write((ushort)(authorBuffer.Length + 1));
            Stream.Write(authorBuffer, 0, authorBuffer.Length);
            Stream.Write((byte)0); // terminate
        }
    }
}
