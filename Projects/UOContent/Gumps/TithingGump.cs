using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps
{
  public class TithingGump : Gump
  {
    private readonly Mobile m_From;
    private int m_Offer;

    public TithingGump(Mobile from, int offer) : base(160, 40)
    {
      int totalGold = from.TotalGold;

      offer = Math.Clamp(offer, 0, totalGold);

      m_From = from;
      m_Offer = offer;

      AddPage(0);

      AddImage(30, 30, 102);

      AddHtmlLocalized(95, 100, 120, 100, 1060198, 0); // May your wealth bring blessings to those in need, if tithed upon this most sacred site.

      AddLabel(57, 274, 0, "Gold:");
      AddLabel(87, 274, 53, (totalGold - offer).ToString());

      AddLabel(137, 274, 0, "Tithe:");
      AddLabel(172, 274, 53, offer.ToString());

      AddButton(105, 230, 5220, 5220, 2);
      AddButton(113, 230, 5222, 5222, 2);
      AddLabel(108, 228, 0, "<");
      AddLabel(112, 228, 0, "<");

      AddButton(127, 230, 5223, 5223, 1);
      AddLabel(131, 228, 0, "<");

      AddButton(147, 230, 5224, 5224, 3);
      AddLabel(153, 228, 0, ">");

      AddButton(168, 230, 5220, 5220, 4);
      AddButton(176, 230, 5222, 5222, 4);
      AddLabel(172, 228, 0, ">");
      AddLabel(176, 228, 0, ">");

      AddButton(217, 272, 4023, 4024, 5);
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      switch (info.ButtonID)
      {
        case 0:
          {
            // You have decided to tithe no gold to the shrine.
            m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060193);
            break;
          }
        case 1:
        case 2:
        case 3:
        case 4:
          {
            var offer = info.ButtonID switch
            {
              1 => m_Offer - 100,
              2 => 0,
              3 => m_Offer + 100,
              4 => m_From.TotalGold,
              _ => 0
            };

            m_From.SendGump(new TithingGump(m_From, offer));
            break;
          }
        case 5:
          {
            int totalGold = m_From.TotalGold;

            m_Offer = Math.Clamp(m_Offer, 0, totalGold);

            if (m_From.TithingPoints + m_Offer > 100000) // TODO: What's the maximum?
              m_Offer = 100000 - m_From.TithingPoints;

            if (m_Offer <= 0)
            {
              // You have decided to tithe no gold to the shrine.
              m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060193);
              break;
            }

            Container pack = m_From.Backpack;

            if (pack?.ConsumeTotal(typeof(Gold), m_Offer) == true)
            {
              // You tithe gold to the shrine as a sign of devotion.
              m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060195);
              m_From.TithingPoints += m_Offer;

              m_From.PlaySound(0x243);
              m_From.PlaySound(0x2E6);
            }
            else
            {
              // You do not have enough gold to tithe that amount!
              m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060194);
            }

            break;
          }
      }
    }
  }
}
