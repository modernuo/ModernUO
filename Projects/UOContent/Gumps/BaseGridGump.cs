using System.Runtime.CompilerServices;
using Server.Utilities;

namespace Server.Gumps
{
    public abstract class BaseGridGump : Gump
    {
        public const int ArrowLeftID1 = 0x15E3;
        public const int ArrowLeftID2 = 0x15E7;
        public const int ArrowLeftWidth = 16;
        public const int ArrowLeftHeight = 16;

        public const int ArrowRightID1 = 0x15E1;
        public const int ArrowRightID2 = 0x15E5;
        public const int ArrowRightWidth = 16;
        public const int ArrowRightHeight = 16;
        protected GumpBackground m_Background;
        protected GumpImageTiled m_Offset;

        public BaseGridGump(int x, int y) : base(x, y)
        {
        }

        public int CurrentPage { get; private set; }

        public int CurrentX { get; private set; }

        public int CurrentY { get; private set; }

        public virtual int BorderSize => 10;
        public virtual int OffsetSize => 1;

        public virtual int EntryHeight => 20;

        public virtual int OffsetGumpID => 0x0A40;
        public virtual int HeaderGumpID => 0x0E14;
        public virtual int EntryGumpID => 0x0BBC;
        public virtual int BackGumpID => 0x13BE;

        public virtual int TextHue => 0;
        public virtual int TextOffsetX => 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Center(string text) => text.Center();

        public string Color(string text, int color) => text.Color(color);

        public int GetButtonID(int typeCount, int type, int index) => 1 + index * typeCount + type;

        public bool SplitButtonID(int buttonID, int typeCount, out int type, out int index)
        {
            if (buttonID < 1)
            {
                type = 0;
                index = 0;
                return false;
            }

            buttonID -= 1;

            type = buttonID % typeCount;
            index = buttonID / typeCount;

            return true;
        }

        public void FinishPage()
        {
            if (m_Background != null)
            {
                m_Background.Height = CurrentY + EntryHeight + OffsetSize + BorderSize;
            }

            if (m_Offset != null)
            {
                m_Offset.Height = CurrentY + EntryHeight + OffsetSize - BorderSize;
            }
        }

        public void AddNewPage()
        {
            FinishPage();

            CurrentX = BorderSize + OffsetSize;
            CurrentY = BorderSize + OffsetSize;

            AddPage(++CurrentPage);

            m_Background = new GumpBackground(0, 0, 100, 100, BackGumpID);
            Add(m_Background);

            m_Offset = new GumpImageTiled(BorderSize, BorderSize, 100, 100, OffsetGumpID);
            Add(m_Offset);
        }

        public void AddNewLine()
        {
            CurrentY += EntryHeight + OffsetSize;
            CurrentX = BorderSize + OffsetSize;
        }

        public void IncreaseX(int width)
        {
            CurrentX += width + OffsetSize;

            width = CurrentX + BorderSize;

            if (width > m_Background?.Width)
            {
                m_Background.Width = width;
            }

            width = CurrentX - BorderSize;

            if (width > m_Offset?.Width)
            {
                m_Offset.Width = width;
            }
        }

        public void AddEntryLabel(int width, string text)
        {
            AddImageTiled(CurrentX, CurrentY, width, EntryHeight, EntryGumpID);
            AddLabelCropped(CurrentX + TextOffsetX, CurrentY, width - TextOffsetX, EntryHeight, TextHue, text);

            IncreaseX(width);
        }

        public void AddEntryHtml(int width, string text)
        {
            AddImageTiled(CurrentX, CurrentY, width, EntryHeight, EntryGumpID);
            AddHtml(CurrentX + TextOffsetX, CurrentY, width - TextOffsetX, EntryHeight, text);

            IncreaseX(width);
        }

        public void AddEntryHeader(int width)
        {
            AddEntryHeader(width, 1);
        }

        public void AddEntryHeader(int width, int spannedEntries)
        {
            AddImageTiled(
                CurrentX,
                CurrentY,
                width,
                EntryHeight * spannedEntries + OffsetSize * (spannedEntries - 1),
                HeaderGumpID
            );
            IncreaseX(width);
        }

        public void AddBlankLine()
        {
            if (m_Offset != null)
            {
                AddImageTiled(m_Offset.X, CurrentY, m_Offset.Width, EntryHeight, BackGumpID + 4);
            }

            AddNewLine();
        }

        public void AddEntryButton(int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight)
        {
            AddEntryButton(width, normalID, pressedID, buttonID, buttonWidth, buttonHeight, 1);
        }

        public void AddEntryButton(
            int width, int normalID, int pressedID, int buttonID, int buttonWidth, int buttonHeight,
            int spannedEntries
        )
        {
            AddImageTiled(
                CurrentX,
                CurrentY,
                width,
                EntryHeight * spannedEntries + OffsetSize * (spannedEntries - 1),
                HeaderGumpID
            );
            AddButton(
                CurrentX + (width - buttonWidth) / 2,
                CurrentY + (EntryHeight * spannedEntries + OffsetSize * (spannedEntries - 1) - buttonHeight) / 2,
                normalID,
                pressedID,
                buttonID
            );

            IncreaseX(width);
        }

        public void AddEntryPageButton(int width, int normalID, int pressedID, int page, int buttonWidth, int buttonHeight)
        {
            AddImageTiled(CurrentX, CurrentY, width, EntryHeight, HeaderGumpID);
            AddButton(
                CurrentX + (width - buttonWidth) / 2,
                CurrentY + (EntryHeight - buttonHeight) / 2,
                normalID,
                pressedID,
                0,
                GumpButtonType.Page,
                page
            );

            IncreaseX(width);
        }

        public void AddEntryText(int width, int entryID, string initialText)
        {
            AddImageTiled(CurrentX, CurrentY, width, EntryHeight, EntryGumpID);
            AddTextEntry(CurrentX + TextOffsetX, CurrentY, width - TextOffsetX, EntryHeight, TextHue, entryID, initialText);

            IncreaseX(width);
        }
    }
}
