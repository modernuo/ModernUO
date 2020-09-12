using Server.Network;

namespace Server.HuePickers
{
    public class HuePicker
    {
        private static int m_NextSerial = 1;

        public HuePicker(int itemID)
        {
            do
            {
                Serial = m_NextSerial++;
            } while (Serial == 0);

            ItemID = itemID;
        }

        public int Serial { get; }

        public int ItemID { get; }

        public virtual void OnResponse(int hue)
        {
        }

        public void SendTo(NetState state)
        {
            state.Send(new DisplayHuePicker(this));
            state.AddHuePicker(this);
        }
    }
}
