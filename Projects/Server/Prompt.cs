namespace Server.Prompts;

public abstract class Prompt
{
    private static int m_Serials;

    protected Prompt()
    {
        do
        {
            Serial = ++m_Serials;
        } while (Serial == 0);
    }

    public int Serial { get; }

    public virtual void OnCancel(Mobile from)
    {
    }

    public virtual void OnResponse(Mobile from, string text)
    {
    }
}
