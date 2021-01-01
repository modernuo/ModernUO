using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server
{
    public delegate void OnVirtueUsed(Mobile from);

    public class VirtueGump : Gump
    {
        private static readonly Dictionary<int, OnVirtueUsed> m_Callbacks = new();

        private static readonly int[] m_Table =
        {
            0x0481, 0x0963, 0x0965,
            0x060A, 0x060F, 0x002A,
            0x08A4, 0x08A7, 0x0034,
            0x0965, 0x08FD, 0x0480,
            0x00EA, 0x0845, 0x0020,
            0x0011, 0x0269, 0x013D,
            0x08A1, 0x08A3, 0x0042,
            0x0543, 0x0547, 0x0061
        };

        private readonly Mobile m_Beheld;

        private readonly Mobile m_Beholder;

        public VirtueGump(Mobile beholder, Mobile beheld) : base(0, 0)
        {
            m_Beholder = beholder;
            m_Beheld = beheld;

            Serial = beheld.Serial;

            AddPage(0);

            AddImage(30, 40, 104);

            AddPage(1);

            Add(new VirtueGumpItem(61, 71, 108, GetHueFor(0)));   // Humility
            Add(new VirtueGumpItem(123, 46, 112, GetHueFor(4)));  // Valor
            Add(new VirtueGumpItem(187, 70, 107, GetHueFor(5)));  // Honor
            Add(new VirtueGumpItem(35, 135, 110, GetHueFor(1)));  // Sacrifice
            Add(new VirtueGumpItem(211, 133, 105, GetHueFor(2))); // Compassion
            Add(new VirtueGumpItem(61, 195, 111, GetHueFor(3)));  // Spiritulaity
            Add(new VirtueGumpItem(186, 195, 109, GetHueFor(6))); // Justice
            Add(new VirtueGumpItem(121, 221, 106, GetHueFor(7))); // Honesty

            if (m_Beholder == m_Beheld)
            {
                AddButton(57, 269, 2027, 2027, 1);
                AddButton(186, 269, 2071, 2071, 2);
            }
        }

        public static void Initialize()
        {
            EventSink.VirtueGumpRequest += EventSink_VirtueGumpRequest;
            EventSink.VirtueItemRequest += EventSink_VirtueItemRequest;
            EventSink.VirtueMacroRequest += EventSink_VirtueMacroRequest;
        }

        public static void Register(int gumpID, OnVirtueUsed callback)
        {
            m_Callbacks[gumpID] = callback;
        }

        private static void EventSink_VirtueItemRequest(Mobile beholder, Mobile beheld, int gumpID)
        {
            if (beholder != beheld)
            {
                return;
            }

            beholder.CloseGump<VirtueGump>();

            if (beholder.Kills >= 5)
            {
                beholder.SendLocalizedMessage(1049609); // Murderers cannot invoke this virtue.
                return;
            }

            if (m_Callbacks.TryGetValue(gumpID, out var callback))
            {
                callback(beholder);
            }
            else
            {
                beholder.SendLocalizedMessage(1052066); // That virtue is not active yet.
            }
        }

        private static void EventSink_VirtueMacroRequest(Mobile beholder, int virtue)
        {
            var virtueID = virtue switch
            {
                0 => 107, // Honor
                1 => 110, // Sacrifice
                2 => 112, // Valor;
                _ => 0
            };

            EventSink_VirtueItemRequest(beholder, beholder, virtueID);
        }

        private static void EventSink_VirtueGumpRequest(Mobile beholder, Mobile beheld)
        {
            if (beholder == beheld && beholder.Kills >= 5)
            {
                beholder.SendLocalizedMessage(1049609); // Murderers cannot invoke this virtue.
            }
            else if (beholder.Map == beheld.Map && beholder.InRange(beheld, 12))
            {
                beholder.CloseGump<VirtueGump>();
                beholder.SendGump(new VirtueGump(beholder, beheld));
            }
        }

        private int GetHueFor(int index)
        {
            if (m_Beheld.Virtues.GetValue(index) == 0)
            {
                return 2402;
            }

            var value = m_Beheld.Virtues.GetValue(index);

            if (value < 4000)
            {
                return 2402;
            }

            if (value >= 30000)
            {
                value = 20000; // Sanity
            }

            int vl;

            if (value < 10000)
            {
                vl = 0;
            }
            else if (value >= 20000 && index == 5)
            {
                vl = 2;
            }
            else if (value >= 21000 && index != 1)
            {
                vl = 2;
            }
            else if (value >= 22000 && index == 1)
            {
                vl = 2;
            }
            else
            {
                vl = 1;
            }

            return m_Table[index * 3 + vl];
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 1 && m_Beholder == m_Beheld)
            {
                m_Beholder.SendGump(new VirtueStatusGump(m_Beholder));
            }
        }

        private class VirtueGumpItem : GumpImage
        {
            public VirtueGumpItem(int x, int y, int gumpID, int hue) : base(x, y, gumpID, hue, "VirtueGumpItem")
            {
            }
        }
    }
}
