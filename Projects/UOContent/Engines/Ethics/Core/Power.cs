namespace Server.Ethics;

public abstract class Power
{
    public PowerDefinition Definition { get; protected set; }

    public virtual bool CheckInvoke(Player from)
    {
        if (!from.Mobile.CheckAlive())
        {
            return false;
        }

        if (from.Power < Definition.Power)
        {
            from.Mobile.LocalOverheadMessage(
                MessageType.Regular,
                0x3B2,
                false,
                "You lack the power to invoke this ability."
            );
            return false;
        }

        return true;
    }

    public abstract void BeginInvoke(Player from);

    public virtual void FinishInvoke(Player from) => from.Power -= Definition.Power;
}
