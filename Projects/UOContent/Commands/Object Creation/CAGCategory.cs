using Server.Gumps;

namespace Server.Commands
{
  public class CAGCategory : CAGNode
  {
    private static CAGCategory m_Root;

    public CAGCategory(string title, CAGCategory parent = null)
    {
      Title = title;
      Parent = parent;
    }

    public override string Title { get; }

    public CAGNode[] Nodes { get; set; }

    public CAGCategory Parent { get; }

    public static CAGCategory Root => m_Root ??= CAGLoader.Load();

    public override void OnClick(Mobile from, int page)
    {
      from.SendGump(new CategorizedAddGump(from, this));
    }
  }
}
