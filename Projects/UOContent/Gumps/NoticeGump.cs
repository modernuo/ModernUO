using Server.Network;

namespace Server.Gumps
{
    public delegate void NoticeGumpCallback();

    public class NoticeGump : Gump
    {
        private readonly NoticeGumpCallback m_Callback;

        public NoticeGump(
            int header, int headerColor, TextDefinition content, int contentColor, int width, int height,
            NoticeGumpCallback callback = null
        ) : base((640 - width) / 2, (480 - height) / 2)
        {
            m_Callback = callback;

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, width, height, 5054);

            AddImageTiled(10, 10, width - 20, 20, 2624);
            AddAlphaRegion(10, 10, width - 20, 20);
            AddHtmlLocalized(10, 10, width - 20, 20, header, headerColor);

            AddImageTiled(10, 40, width - 20, height - 80, 2624);
            AddAlphaRegion(10, 40, width - 20, height - 80);

            if (content != null)
            {
                if (content.Number > 0)
                {
                    AddHtmlLocalized(10, 40, width - 20, height - 80, content.Number, contentColor, false, true);
                }
                else
                {
                    AddHtml(
                        10,
                        40,
                        width - 20,
                        height - 80,
                        $"<BASEFONT COLOR=#{contentColor:X6}>{content.String}</BASEFONT>",
                        false,
                        true
                    );
                }
            }

            AddImageTiled(10, height - 30, width - 20, 20, 2624);
            AddAlphaRegion(10, height - 30, width - 20, 20);
            AddButton(10, height - 30, 4005, 4007, 1);
            AddHtmlLocalized(40, height - 30, 120, 20, 1011036, 32767); // OKAY
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                m_Callback?.Invoke();
            }
        }
    }
}
