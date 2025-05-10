using Server.Network;

namespace Server.Menus.Questions;

public class QuestionMenu : BaseMenu
{
    public QuestionMenu(string question, string[] answers)
    {
        Question = question?.Trim() ?? "";
        Answers = answers;
    }

    public string Question { get; }

    public string[] Answers { get; }

    public override int EntryLength => Answers.Length;

    public override void SendTo(NetState state)
    {
        state.AddMenu(this);
        state.SendDisplayQuestionMenu(this);
    }
}
