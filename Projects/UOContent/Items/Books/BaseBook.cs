using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Text;

namespace Server.Items
{
    [SerializationGenerator(5, false)]
    public partial class BaseBook : Item, ISecurable
    {
        [SerializableField(0)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private SecureLevel _level;

        [InternString]
        [InvalidateProperties]
        [SerializableField(1)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _title;

        [SerializableFieldSaveFlag(1)]
        private bool ShouldSerializeTitle() => _title != DefaultContent?.Title;

        [SerializableFieldDefault(1)]
        private string TitleDefaultValue() => DefaultContent?.Title;

        [InvalidateProperties]
        [SerializableField(2)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private string _author;

        [SerializableFieldSaveFlag(2)]
        private bool ShouldSerializeAuthor() => _author != DefaultContent?.Author;

        [SerializableFieldDefault(2)]
        private string AuthorDefaultValue() => DefaultContent?.Author;

        [SerializableField(3)]
        [SerializedCommandProperty(AccessLevel.GameMaster)]
        private bool _writable;

        [SerializableFieldSaveFlag(3)]
        private bool ShouldSerializeWritable() => _writable;

        [SerializableField(4, setter: "protected")]
        private BookPageInfo[] _pages;

        [SerializableFieldSaveFlag(4)]
        private bool ShouldSerializePages() => DefaultContent?.IsMatch(_pages) != true;

        [SerializableFieldDefault(4)]
        private BookPageInfo[] PagesDefaultvalue() => DefaultContent?.Copy() ?? Array.Empty<BookPageInfo>();

        [Constructible]
        public BaseBook(int itemID, int pageCount = 20, bool writable = true) : this(itemID, null, null, pageCount, writable)
        {
        }

        [Constructible]
        public BaseBook(int itemID, string title, string author, int pageCount, bool writable) : base(itemID)
        {
            var content = DefaultContent;

            _title = title ?? content?.Title;
            _author = author ?? content?.Author;
            _writable = writable;

            if (content == null)
            {
                _pages = new BookPageInfo[pageCount];

                for (var i = 0; i < _pages.Length; ++i)
                {
                    _pages[i] = new BookPageInfo();
                }
            }
            else
            {
                _pages = content.Copy();
            }
        }

        // Intended for defined books only
        public BaseBook(int itemID, bool writable) : this(itemID, 0, writable)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PagesCount => _pages.Length;

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

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);
            SetSecureLevelEntry.AddTo(from, this, list);
        }

        private void Deserialize(IGenericReader reader, int version)
        {
            Level = (SecureLevel)reader.ReadInt();
            var content = DefaultContent;

            var flags = (OldSaveFlags)reader.ReadEncodedInt();

            if ((flags & OldSaveFlags.Title) != 0)
            {
                _title = reader.ReadString().Intern();
            }
            else if (content != null)
            {
                _title = content.Title;
            }

            if ((flags & OldSaveFlags.Author) != 0)
            {
                _author = reader.ReadString();
            }
            else if (content != null)
            {
                _author = content.Author;
            }

            Writable = (flags & OldSaveFlags.Writable) != 0;

            if ((flags & OldSaveFlags.Content) != 0)
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
        }

        public override void AddNameProperty(IPropertyList list)
        {
            if (!string.IsNullOrEmpty(_title))
            {
                list.Add(_title);
            }
            else
            {
                base.AddNameProperty(list);
            }
        }

        /*public override void GetProperties( ObjectPropertyList list )
        {
          base.GetProperties( list );

          if (_title?.Length > 0)
            list.Add( 1060658, "Title\t{0}", _title ); // ~1_val~: ~2_val~

          if (_author?.Length > 0)
            list.Add( 1060659, "Author\t{0}", _author ); // ~1_val~: ~2_val~

          if (_pages?.Length > 0)
            list.Add( 1060660, "Pages\t{0}", _pages.Length ); // ~1_val~: ~2_val~
        }*/

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, $"{_title} by {_author}");
            LabelTo(from, $"[{_pages.Length} pages]");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (_title == null && _author == null && Writable)
            {
                Title = "a book";
                Author = from.Name;
            }

            from.NetState.SendBookCover(from, this);
            from.NetState.SendBookContent(this);
        }

        [Flags]
        private enum OldSaveFlags
        {
            None = 0x00,
            Title = 0x01,
            Author = 0x02,
            Writable = 0x04,
            Content = 0x08
        }
    }
}
