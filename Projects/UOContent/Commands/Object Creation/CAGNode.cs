namespace Server.Commands
{
  public abstract class CAGNode
  {
    public abstract string Title { get; }
    public abstract void OnClick(Mobile from, int page);
  }
}
