using Server.Gumps;
using Server.Network;

namespace Server.Engines.ConPVP
{
    public class ReadyGump : Gump
    {
        private DuelContext m_Context;
        private int m_Count;
        private Mobile m_From;

        public ReadyGump(Mobile from, DuelContext context, int count) : base(50, 50)
        {
            m_From = from;
            m_Context = context;
            m_Count = count;

            var parts = context.Participants;

            var height = 25 + 20;

            for (var i = 0; i < parts.Count; ++i)
            {
                var p = parts[i];

                height += 4;

                if (p.Players.Length > 1)
                {
                    height += 22;
                }

                height += p.Players.Length * 22;
            }

            height += 25;

            Closable = false;
            Draggable = false;

            AddPage(0);

            AddBackground(0, 0, 260, height, 9250);
            AddBackground(10, 10, 240, height - 20, 0xDAC);

            if (count == -1)
            {
                AddHtml(35, 25, 190, 20, Center("Ready"));
            }
            else
            {
                AddHtml(35, 25, 190, 20, Center("Starting"));
                AddHtml(35, 25, 190, 20, $"<DIV ALIGN=RIGHT>{count}");
            }

            var y = 25 + 20;

            for (var i = 0; i < parts.Count; ++i)
            {
                var p = parts[i];

                y += 4;

                var isAllReady = true;
                var yStore = y;
                var offset = 0;

                if (p.Players.Length > 1)
                {
                    AddHtml(35 + 14, y, 176, 20, $"Participant #{i + 1}");
                    y += 22;
                    offset = 10;
                }

                for (var j = 0; j < p.Players.Length; ++j)
                {
                    var pl = p.Players[j];

                    if (pl?.Ready == true)
                    {
                        AddImage(35 + offset, y + 4, 0x939);
                    }
                    else
                    {
                        AddImage(35 + offset, y + 4, 0x938);
                        isAllReady = false;
                    }

                    var name = pl == null ? "(Empty)" : pl.Mobile.Name;

                    AddHtml(35 + offset + 14, y, 166, 20, name);

                    y += 22;
                }

                if (p.Players.Length > 1)
                {
                    AddImage(35, yStore + 4, isAllReady ? 0x939 : 0x938);
                }
            }
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }
    }
}
