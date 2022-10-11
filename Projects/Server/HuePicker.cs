using Server.Network;

namespace Server.HuePickers;

public class HuePicker
{
    private static Serial _nextSerial = (Serial)1;

    public HuePicker(int itemID)
    {
        do
        {
            Serial = _nextSerial++;
        } while (Serial == 0);

        ItemID = itemID;
    }

    public Serial Serial { get; }

    public int ItemID { get; }

    public virtual void OnResponse(int hue)
    {
    }

    public void SendTo(NetState state)
    {
        state.SendDisplayHuePicker(Serial, ItemID);
        state.AddHuePicker(this);
    }
}
