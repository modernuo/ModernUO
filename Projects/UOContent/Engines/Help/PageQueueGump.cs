using System;
using System.Collections.Generic;
using System.IO;
using Server.Gumps;
using Server.Network;

namespace Server.Engines.Help
{
    public class MessageSentGump : Gump
    {
        private readonly Mobile m_Mobile;
        private readonly string m_Name;
        private readonly string m_Text;

        public MessageSentGump(Mobile mobile, string name, string text) : base(30, 30)
        {
            m_Name = name;
            m_Text = text;
            m_Mobile = mobile;

            Closable = false;

            AddPage(0);

            AddBackground(0, 0, 92, 75, 0xA3C);

            if (mobile?.NetState?.IsEnhancedClient == true)
            {
                AddBackground(5, 7, 82, 61, 9300);
            }
            else
            {
                AddImageTiled(5, 7, 82, 61, 0xA40);
                AddAlphaRegion(5, 7, 82, 61);
            }

            AddImageTiled(9, 11, 21, 53, 0xBBC);

            AddButton(10, 12, 0x7D2, 0x7D2, 0);
            AddHtmlLocalized(34, 28, 65, 24, 3001002, 0xFFFFFF); // Message
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            m_Mobile.SendGump(new PageResponseGump(m_Mobile, m_Name, m_Text));

            // m_Mobile.SendMessage( 0x482, "{0} tells you:", m_Name );
            // m_Mobile.SendMessage( 0x482, m_Text );
        }
    }

    public class PageQueueGump : Gump
    {
        private readonly PageEntry[] m_List;

        public PageQueueGump() : base(30, 30)
        {
            Add(new GumpPage(0));
            // Add( new GumpBackground( 0, 0, 410, 448, 9200 ) );
            Add(new GumpImageTiled(0, 0, 410, 448, 0xA40));
            Add(new GumpAlphaRegion(1, 1, 408, 446));

            Add(new GumpLabel(180, 12, 2100, "Page Queue"));

            var list = PageQueue.List;

            for (var i = 0; i < list.Count;)
            {
                var e = list[i];

                if (e.Sender.Deleted || e.Sender.NetState == null)
                    // e.AddResponse(e.Sender, "[Logout]");
                {
                    PageQueue.Remove(e);
                }
                else
                {
                    ++i;
                }
            }

            m_List = list.ToArray();

            if (m_List.Length <= 0)
            {
                Add(new GumpLabel(12, 44, 2100, "The page queue is empty."));
                return;
            }

            Add(new GumpPage(1));

            for (var i = 0; i < m_List.Length; ++i)
            {
                var e = m_List[i];

                if (i >= 5 && i % 5 == 0)
                {
                    Add(new GumpButton(368, 12, 0xFA5, 0xFA7, 0, GumpButtonType.Page, i / 5 + 1));
                    Add(new GumpLabel(298, 12, 2100, "Next Page"));
                    Add(new GumpPage(i / 5 + 1));
                    Add(new GumpButton(12, 12, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / 5));
                    Add(new GumpLabel(48, 12, 2100, "Previous Page"));
                }

                var typeString = PageQueue.GetPageTypeName(e.Type);

                var html =
                    $"[{typeString}] {e.Message} <basefont color=#{(e.Handler == null ? 0xFF0000 : 0xFF):X6}>[<u>{(e.Handler == null ? "Unhandled" : "Handling")}</u>]</basefont>";

                Add(new GumpHtml(12, 44 + i % 5 * 80, 350, 70, html, true, true));
                Add(new GumpButton(370, 44 + i % 5 * 80 + 24, 0xFA5, 0xFA7, i + 1));
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID >= 1 && info.ButtonID <= m_List.Length)
            {
                if (PageQueue.List.IndexOf(m_List[info.ButtonID - 1]) >= 0)
                {
                    var g = new PageEntryGump(state.Mobile, m_List[info.ButtonID - 1]);

                    g.SendTo(state);
                }
                else
                {
                    state.Mobile.SendGump(new PageQueueGump());
                    state.Mobile.SendMessage("That page has been removed.");
                }
            }
        }
    }

    public class PredefinedResponse
    {
        public PredefinedResponse(string title, string message)
        {
            Title = title;
            Message = message;
        }

        public string Title { get; set; }

        public string Message { get; set; }

        public static List<PredefinedResponse> List { get; private set; } = Load();

        public static PredefinedResponse Add(string title, string message)
        {
            var resp = new PredefinedResponse(title, message);

            List.Add(resp);
            Save();

            return resp;
        }

        public static void Save()
        {
            List ??= Load();

            try
            {
                var path = Path.Combine(Core.BaseDirectory, "Data/pageresponse.cfg");

                using var op = new StreamWriter(path);
                for (var i = 0; i < List.Count; ++i)
                {
                    var resp = List[i];

                    op.WriteLine("{0}\t{1}", resp.Title, resp.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static List<PredefinedResponse> Load()
        {
            var path = Path.Combine(Core.BaseDirectory, "Data/pageresponse.cfg");

            if (!File.Exists(path))
            {
                return new List<PredefinedResponse>();
            }

            var list = new List<PredefinedResponse>();

            try
            {
                using var ip = new StreamReader(path);
                string line;

                while ((line = ip.ReadLine()?.Trim()) != null)
                {
                    if (line.Length == 0 || line.StartsWithOrdinal("#"))
                    {
                        continue;
                    }

                    var split = line.Split('\t');

                    if (split.Length == 2)
                    {
                        list.Add(new PredefinedResponse(split[0], split[1]));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return list;
        }
    }

    public class PredefGump : Gump
    {
        private const int LabelColor32 = 0xFFFFFF;

        private readonly Mobile m_From;
        private readonly PredefinedResponse m_Response;

        public PredefGump(Mobile from, PredefinedResponse response) : base(30, 30)
        {
            m_From = from;
            m_Response = response;

            from.CloseGump<PredefGump>();

            var canEdit = from.AccessLevel >= AccessLevel.GameMaster;

            AddPage(0);

            if (response == null)
            {
                if (from.NetState?.IsEnhancedClient == true)
                {
                    AddBackground(1, 1, 408, 446, 9300);
                }
                else
                {
                    AddImageTiled(0, 0, 410, 448, 0xA40);
                    AddAlphaRegion(1, 1, 408, 446);
                }

                AddHtml(10, 10, 390, 20, Color(Center("Predefined Responses"), LabelColor32));

                var list = PredefinedResponse.List;

                AddPage(1);

                int i;

                for (i = 0; i < list.Count; ++i)
                {
                    if (i >= 5 && i % 5 == 0)
                    {
                        AddButton(368, 10, 0xFA5, 0xFA7, 0, GumpButtonType.Page, i / 5 + 1);
                        AddLabel(298, 10, 2100, "Next Page");
                        AddPage(i / 5 + 1);
                        AddButton(12, 10, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / 5);
                        AddLabel(48, 10, 2100, "Previous Page");
                    }

                    var resp = list[i];

                    var html = $"<u>{resp.Title}</u><br>{resp.Message}";

                    AddHtml(12, 44 + i % 5 * 80, 350, 70, html, true, true);

                    if (canEdit)
                    {
                        AddButton(370, 44 + i % 5 * 80 + 24, 0xFA5, 0xFA7, 2 + i * 3);

                        if (i > 0)
                        {
                            AddButton(377, 44 + i % 5 * 80 + 2, 0x15E0, 0x15E4, 3 + i * 3);
                        }
                        else
                        {
                            AddImage(377, 44 + i % 5 * 80 + 2, 0x25E4);
                        }

                        if (i < list.Count - 1)
                        {
                            AddButton(377, 44 + i % 5 * 80 + 70 - 2 - 16, 0x15E2, 0x15E6, 4 + i * 3);
                        }
                        else
                        {
                            AddImage(377, 44 + i % 5 * 80 + 70 - 2 - 16, 0x25E8);
                        }
                    }
                }

                if (canEdit)
                {
                    if (i >= 5 && i % 5 == 0)
                    {
                        AddButton(368, 10, 0xFA5, 0xFA7, 0, GumpButtonType.Page, i / 5 + 1);
                        AddLabel(298, 10, 2100, "Next Page");
                        AddPage(i / 5 + 1);
                        AddButton(12, 10, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / 5);
                        AddLabel(48, 10, 2100, "Previous Page");
                    }

                    AddButton(12, 44 + i % 5 * 80, 0xFAB, 0xFAD, 1);
                    AddHtml(45, 44 + i % 5 * 80, 200, 20, Color("New Response", LabelColor32));
                }
            }
            else if (canEdit)
            {
                AddImageTiled(0, 0, 410, 250, 0xA40);

                if (from.NetState?.IsEnhancedClient == true)
                {
                    AddBackground(1, 1, 408, 248, 9300);
                }
                else
                {
                    AddAlphaRegion(1, 1, 408, 248);
                }

                AddHtml(10, 10, 390, 20, Color(Center("Predefined Response Editor"), LabelColor32));

                AddButton(10, 40, 0xFB1, 0xFB3, 1);
                AddHtml(45, 40, 200, 20, Color("Remove", LabelColor32));

                AddButton(10, 70, 0xFA5, 0xFA7, 2);
                AddHtml(45, 70, 200, 20, Color("Title:", LabelColor32));
                AddTextInput(10, 90, 300, 20, 0, response.Title);

                AddButton(10, 120, 0xFA5, 0xFA7, 3);
                AddHtml(45, 120, 200, 20, Color("Message:", LabelColor32));
                AddTextInput(10, 140, 390, 100, 1, response.Message);
            }
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AddTextInput(int x, int y, int w, int h, int id, string def)
        {
            AddImageTiled(x, y, w, h, 0xA40);
            AddImageTiled(x + 1, y + 1, w - 2, h - 2, 0xBBC);
            AddTextEntry(x + 3, y + 1, w - 4, h - 2, 0x480, id, def);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (m_From.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            if (m_Response == null)
            {
                var index = info.ButtonID - 1;

                if (index == 0)
                {
                    var resp = new PredefinedResponse("", "");

                    var list = PredefinedResponse.List;
                    list.Add(resp);

                    m_From.SendGump(new PredefGump(m_From, resp));
                }
                else
                {
                    --index;

                    var type = index % 3;
                    index /= 3;

                    var list = PredefinedResponse.List;

                    if (index >= 0 && index < list.Count)
                    {
                        var resp = list[index];

                        switch (type)
                        {
                            case 0: // edit
                                {
                                    m_From.SendGump(new PredefGump(m_From, resp));
                                    break;
                                }
                            case 1: // move up
                                {
                                    if (index > 0)
                                    {
                                        list.RemoveAt(index);
                                        list.Insert(index - 1, resp);

                                        PredefinedResponse.Save();
                                        m_From.SendGump(new PredefGump(m_From, null));
                                    }

                                    break;
                                }
                            case 2: // move down
                                {
                                    if (index < list.Count - 1)
                                    {
                                        list.RemoveAt(index);
                                        list.Insert(index + 1, resp);

                                        PredefinedResponse.Save();
                                        m_From.SendGump(new PredefGump(m_From, null));
                                    }

                                    break;
                                }
                        }
                    }
                }
            }
            else
            {
                var list = PredefinedResponse.List;

                switch (info.ButtonID)
                {
                    case 1:
                        {
                            list.Remove(m_Response);

                            PredefinedResponse.Save();
                            m_From.SendGump(new PredefGump(m_From, null));
                            break;
                        }
                    case 2:
                        {
                            var te = info.GetTextEntry(0);

                            if (te != null)
                            {
                                m_Response.Title = te.Text;
                            }

                            PredefinedResponse.Save();
                            m_From.SendGump(new PredefGump(m_From, m_Response));

                            break;
                        }
                    case 3:
                        {
                            var te = info.GetTextEntry(1);

                            if (te != null)
                            {
                                m_Response.Message = te.Text;
                            }

                            PredefinedResponse.Save();
                            m_From.SendGump(new PredefGump(m_From, m_Response));

                            break;
                        }
                }
            }
        }
    }

    public class PageEntryGump : Gump
    {
        private static readonly int[] m_AccessLevelHues =
        {
            2100,
            2122,
            2117,
            2129,
            2415,
            2415,
            2415
        };

        private readonly PageEntry m_Entry;
        private readonly Mobile m_Mobile;

        public PageEntryGump(Mobile m, PageEntry entry) : base(30, 30)
        {
            m_Mobile = m;
            m_Entry = entry;

            var buttons = 0;

            var bottom = 356;

            AddPage(0);

            if (m.NetState?.IsEnhancedClient == true)
            {
                AddBackground(1, 1, 408, 454, 9300);
            }
            else
            {
                AddImageTiled(0, 0, 410, 456, 0xA40);
                AddAlphaRegion(1, 1, 408, 454);
            }

            AddPage(1);

            AddLabel(18, 18, 2100, "Sent:");
            AddLabelCropped(128, 18, 264, 20, 2100, entry.Sent.ToString());

            AddLabel(18, 38, 2100, "Sender:");
            AddLabelCropped(
                128,
                38,
                264,
                20,
                2100,
                $"{entry.Sender.RawName} {entry.Sender.Location} [{entry.Sender.Map}]"
            );

            AddButton(18, bottom - buttons * 22, 0xFAB, 0xFAD, 8);
            AddImageTiled(52, bottom - buttons * 22 + 1, 340, 80, 0xA40 /*0xBBC*/ /*0x2458*/);
            AddImageTiled(53, bottom - buttons * 22 + 2, 338, 78, 0xBBC /*0x2426*/);
            AddTextEntry(55, bottom - buttons++ * 22 + 2, 336, 78, 0x480, 0, "");

            AddButton(18, bottom - buttons * 22, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 2);
            AddLabel(52, bottom - buttons++ * 22, 2100, "Predefined Response");

            if (entry.Sender != m)
            {
                AddButton(18, bottom - buttons * 22, 0xFA5, 0xFA7, 1);
                AddLabel(52, bottom - buttons++ * 22, 2100, "Go to Sender");
            }

            AddLabel(18, 58, 2100, "Handler:");

            if (entry.Handler == null)
            {
                AddLabelCropped(128, 58, 264, 20, 2100, "Unhandled");

                AddButton(18, bottom - buttons * 22, 0xFB1, 0xFB3, 5);
                AddLabel(52, bottom - buttons++ * 22, 2100, "Delete Page");

                AddButton(18, bottom - buttons * 22, 0xFB7, 0xFB9, 4);
                AddLabel(52, bottom - buttons++ * 22, 2100, "Handle Page");
            }
            else
            {
                AddLabelCropped(128, 58, 264, 20, m_AccessLevelHues[(int)entry.Handler.AccessLevel], entry.Handler.Name);

                if (entry.Handler != m)
                {
                    AddButton(18, bottom - buttons * 22, 0xFA5, 0xFA7, 2);
                    AddLabel(52, bottom - buttons++ * 22, 2100, "Go to Handler");
                }
                else
                {
                    AddButton(18, bottom - buttons * 22, 0xFA2, 0xFA4, 6);
                    AddLabel(52, bottom - buttons++ * 22, 2100, "Abandon Page");

                    AddButton(18, bottom - buttons * 22, 0xFB7, 0xFB9, 7);
                    AddLabel(52, bottom - buttons++ * 22, 2100, "Page Handled");
                }
            }

            AddLabel(18, 78, 2100, "Page Location:");
            AddLabelCropped(128, 78, 264, 20, 2100, $"{entry.PageLocation} [{entry.PageMap}]");

            AddButton(18, bottom - buttons * 22, 0xFA5, 0xFA7, 3);
            AddLabel(52, bottom - buttons++ * 22, 2100, "Go to Page Location");

            if (entry.SpeechLog != null)
            {
                AddButton(18, bottom - buttons * 22, 0xFA5, 0xFA7, 10);
                AddLabel(52, bottom - buttons * 22, 2100, "View Speech Log");
            }

            AddLabel(18, 98, 2100, "Page Type:");
            AddLabelCropped(128, 98, 264, 20, 2100, PageQueue.GetPageTypeName(entry.Type));

            AddLabel(18, 118, 2100, "Message:");
            AddHtml(128, 118, 250, 100, entry.Message, true, true);

            AddPage(2);

            var preresp = PredefinedResponse.List;

            AddButton(18, 18, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);
            AddButton(410 - 18 - 32, 18, 0xFAB, 0xFAC, 9);

            if (preresp.Count == 0)
            {
                AddLabel(52, 18, 2100, "There are no predefined responses.");
            }
            else
            {
                AddLabel(52, 18, 2100, "Back");

                for (var i = 0; i < preresp.Count; ++i)
                {
                    AddButton(18, 40 + i * 22, 0xFA5, 0xFA7, 100 + i);
                    AddLabel(52, 40 + i * 22, 2100, preresp[i].Title);
                }
            }
        }

        public void Resend(NetState state)
        {
            var g = new PageEntryGump(m_Mobile, m_Entry);

            g.SendTo(state);
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID != 0 && PageQueue.List.IndexOf(m_Entry) < 0)
            {
                state.Mobile.SendGump(new PageQueueGump());
                state.Mobile.SendMessage("That page has been removed.");
                return;
            }

            switch (info.ButtonID)
            {
                case 0: // close
                    {
                        if (m_Entry.Handler != state.Mobile)
                        {
                            var g = new PageQueueGump();

                            g.SendTo(state);
                        }

                        break;
                    }
                case 1: // go to sender
                    {
                        var m = state.Mobile;

                        if (m_Entry.Sender.Deleted)
                        {
                            m.SendMessage("That character no longer exists.");
                        }
                        else if (m_Entry.Sender.Map == null || m_Entry.Sender.Map == Map.Internal)
                        {
                            m.SendMessage("That character is not in the world.");
                        }
                        else
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Go Sender]");
                            m.MoveToWorld(m_Entry.Sender.Location, m_Entry.Sender.Map);

                            m.SendMessage("You have been teleported to that page's sender.");

                            Resend(state);
                        }

                        break;
                    }
                case 2: // go to handler
                    {
                        var m = state.Mobile;
                        var h = m_Entry.Handler;

                        if (h != null)
                        {
                            if (h.Deleted)
                            {
                                m.SendMessage("That character no longer exists.");
                            }
                            else if (h.Map == null || h.Map == Map.Internal)
                            {
                                m.SendMessage("That character is not in the world.");
                            }
                            else
                            {
                                // m_Entry.AddResponse(state.Mobile, "[Go Handler]");
                                m.MoveToWorld(h.Location, h.Map);

                                m.SendMessage("You have been teleported to that page's handler.");
                                Resend(state);
                            }
                        }
                        else
                        {
                            m.SendMessage("Nobody is handling that page.");
                            Resend(state);
                        }

                        break;
                    }
                case 3: // go to page location
                    {
                        var m = state.Mobile;

                        if (m_Entry.PageMap == null || m_Entry.PageMap == Map.Internal)
                        {
                            m.SendMessage("That location is not in the world.");
                        }
                        else
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Go PageLoc]");
                            m.MoveToWorld(m_Entry.PageLocation, m_Entry.PageMap);

                            state.Mobile.SendMessage("You have been teleported to the original page location.");

                            Resend(state);
                        }

                        break;
                    }
                case 4: // handle page
                    {
                        if (m_Entry.Handler == null)
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Handling]");
                            m_Entry.Handler = state.Mobile;

                            state.Mobile.SendMessage("You are now handling the page.");
                        }
                        else
                        {
                            state.Mobile.SendMessage("Someone is already handling that page.");
                        }

                        Resend(state);

                        break;
                    }
                case 5: // delete page
                    {
                        if (m_Entry.Handler == null)
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Deleting]");
                            PageQueue.Remove(m_Entry);

                            state.Mobile.SendMessage("You delete the page.");

                            var g = new PageQueueGump();

                            g.SendTo(state);
                        }
                        else
                        {
                            state.Mobile.SendMessage("Someone is handling that page, it can not be deleted.");

                            Resend(state);
                        }

                        break;
                    }
                case 6: // abandon page
                    {
                        if (m_Entry.Handler == state.Mobile)
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Abandoning]");
                            state.Mobile.SendMessage("You abandon the page.");

                            m_Entry.Handler = null;
                        }
                        else
                        {
                            state.Mobile.SendMessage("You are not handling that page.");
                        }

                        Resend(state);

                        break;
                    }
                case 7: // page handled
                    {
                        if (m_Entry.Handler == state.Mobile)
                        {
                            // m_Entry.AddResponse(state.Mobile, "[Handled]");
                            PageQueue.Remove(m_Entry);

                            m_Entry.Handler = null;

                            state.Mobile.SendMessage("You mark the page as handled, and remove it from the queue.");

                            var g = new PageQueueGump();

                            g.SendTo(state);
                        }
                        else
                        {
                            state.Mobile.SendMessage("You are not handling that page.");

                            Resend(state);
                        }

                        break;
                    }
                case 8: // Send message
                    {
                        var text = info.GetTextEntry(0);

                        if (text != null)
                            // m_Entry.AddResponse(state.Mobile, "[Response] " + text.Text);
                        {
                            m_Entry.Sender.SendGump(new MessageSentGump(m_Entry.Sender, state.Mobile.Name, text.Text));
                        }
                        // m_Entry.Sender.SendMessage( 0x482, "{0} tells you:", state.Mobile.Name );
                        // m_Entry.Sender.SendMessage( 0x482, text.Text );

                        Resend(state);

                        break;
                    }
                case 9: // predef overview
                    {
                        Resend(state);
                        state.Mobile.SendGump(new PredefGump(state.Mobile, null));

                        break;
                    }
                case 10: // View Speech Log
                    {
                        Resend(state);

                        if (m_Entry.SpeechLog != null)
                        {
                            state.Mobile.SendGump(new SpeechLogGump(m_Entry.Sender, m_Entry.SpeechLog));
                        }

                        break;
                    }
                default:
                    {
                        var index = info.ButtonID - 100;
                        var preresp = PredefinedResponse.List;

                        if (index >= 0 && index < preresp.Count)
                            // m_Entry.AddResponse(state.Mobile, "[PreDef] " + preresp[index].Title);
                        {
                            m_Entry.Sender.SendGump(
                                new MessageSentGump(
                                    m_Entry.Sender,
                                    state.Mobile.Name,
                                    preresp[index].Message
                                )
                            );
                        }

                        Resend(state);

                        break;
                    }
            }
        }
    }
}
