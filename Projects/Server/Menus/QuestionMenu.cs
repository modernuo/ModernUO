using Server.Network;

namespace Server.Menus.Questions;

public class QuestionMenu : IMenu
{
    private static int m_NextSerial;

    public QuestionMenu(string question, string[] answers)
    {
        Question = question?.Trim() ?? "";
        Answers = answers;

        do
        {
            Serial = ++m_NextSerial;
            Serial &= 0x7FFFFFFF;
        } while (Serial == 0);
    }

    public string Question { get; }

    public string[] Answers { get; }

    public int Serial { get; }

    public int EntryLength => Answers.Length;

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }

    public void SendTo(NetState state)
    {
        state.AddMenu(this);
        state.SendDisplayQuestionMenu(this);
    }
}
