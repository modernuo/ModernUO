using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server
{
  public delegate void OnVirtueUsed(Mobile from);

  public class VirtueGump : Gump
  {
    private static Dictionary<int, OnVirtueUsed> m_Callbacks = new Dictionary<int, OnVirtueUsed>();

    private static int[] m_Table = {
      0x0481, 0x0963, 0x0965,
      0x060A, 0x060F, 0x002A,
      0x08A4, 0x08A7, 0x0034,
      0x0965, 0x08FD, 0x0480,
      0x00EA, 0x0845, 0x0020,
      0x0011, 0x0269, 0x013D,
      0x08A1, 0x08A3, 0x0042,
      0x0543, 0x0547, 0x0061
    };

    private Mobile m_Beholder, m_Beheld;

    public VirtueGump(Mobile beholder, Mobile beheld) : base(0, 0)
    {
      m_Beholder = beholder;
      m_Beheld = beheld;

      Serial = beheld.Serial;

      AddPage(0);

      AddImage(30, 40, 104);

      AddPage(1);

      Add(new GumpImage(61, 71, 108, GetHueFor(0), "VirtueGumpItem")); // Humility
      Add(new GumpImage(123, 46, 112, GetHueFor(4), "VirtueGumpItem")); // Valor
      Add(new GumpImage(187, 70, 107, GetHueFor(5), "VirtueGumpItem")); // Honor
      Add(new GumpImage(35, 135, 110, GetHueFor(1), "VirtueGumpItem")); // Sacrifice
      Add(new GumpImage(211, 133, 105, GetHueFor(2), "VirtueGumpItem")); // Compassion
      Add(new GumpImage(61, 195, 111, GetHueFor(3), "VirtueGumpItem")); // Spiritulaity
      Add(new GumpImage(186, 195, 109, GetHueFor(6), "VirtueGumpItem")); // Justice
      Add(new GumpImage(121, 221, 106, GetHueFor(7), "VirtueGumpItem")); // Honesty

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

    private static void EventSink_VirtueItemRequest(VirtueItemRequestEventArgs e)
    {
      if (e.Beholder != e.Beheld)
        return;

      e.Beholder.CloseGump<VirtueGump>();

      if (e.Beholder.Kills >= 5)
      {
        e.Beholder.SendLocalizedMessage(1049609); // Murderers cannot invoke this virtue.
        return;
      }

      if (m_Callbacks.TryGetValue(e.GumpID, out OnVirtueUsed callback))
        callback(e.Beholder);
      else
        e.Beholder.SendLocalizedMessage(1052066); // That virtue is not active yet.
    }


    private static void EventSink_VirtueMacroRequest(VirtueMacroRequestEventArgs e)
    {
      int virtueID = 0;

      switch (e.VirtueID)
      {
        case 0: // Honor
          virtueID = 107;
          break;
        case 1: // Sacrifice
          virtueID = 110;
          break;
        case 2: // Valor;
          virtueID = 112;
          break;
      }

      EventSink_VirtueItemRequest(new VirtueItemRequestEventArgs(e.Mobile, e.Mobile, virtueID));
    }

    private static void EventSink_VirtueGumpRequest(VirtueGumpRequestEventArgs e)
    {
      Mobile beholder = e.Beholder;
      Mobile beheld = e.Beheld;

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
        return 2402;

      int value = m_Beheld.Virtues.GetValue(index);

      if (value < 4000)
        return 2402;

      if (value >= 30000)
        value = 20000; //Sanity


      int vl;

      if (value < 10000)
        vl = 0;
      else if (value >= 20000 && index == 5)
        vl = 2;
      else if (value >= 21000 && index != 1)
        vl = 2;
      else if (value >= 22000 && index == 1)
        vl = 2;
      else
        vl = 1;


      return m_Table[index * 3 + vl];
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      if (info.ButtonID == 1 && m_Beholder == m_Beheld)
        m_Beholder.SendGump(new VirtueStatusGump(m_Beholder));
    }
  }
}
