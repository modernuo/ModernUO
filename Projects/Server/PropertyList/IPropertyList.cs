namespace Server;

public interface IPropertyList
{
    public void Terminate();
    // TODO: Use string interpolator
    public void Add(int number, string arguments = null);
    public void Add(string text);
}
