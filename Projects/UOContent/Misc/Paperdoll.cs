using Server.Network;

namespace Server.Misc
{
    public static class Paperdoll
    {
        public static void Initialize()
        {
            EventSink.PaperdollRequest += EventSink_PaperdollRequest;
        }

        public static void EventSink_PaperdollRequest(Mobile beholder, Mobile beheld)
        {
            beholder.NetState.SendDisplayPaperdoll(
                beheld.Serial,
                Titles.ComputeTitle(beholder, beheld),
                beheld.Warmode,
                beheld.AllowEquipFrom(beholder)
            );

            for (var i = 0; i < beheld.Items.Count; ++i)
            {
                beheld.Items[i].SendOPLPacketTo(beholder.NetState);
            }

            // NOTE: OSI sends MobileUpdate when opening your own paperdoll.
            // It has a very bad rubber-banding affect. What positive affects does it have?
        }
    }
}
