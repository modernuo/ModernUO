using System.Collections;

namespace Server.Engines.Craft
{
  public class CraftResCol : CollectionBase
  {
    public void Add(CraftRes craftRes)
    {
      List.Add(craftRes);
    }

    public void Remove(int index)
    {
      if (index > Count - 1 || index < 0)
      {
      }
      else
      {
        List.RemoveAt(index);
      }
    }

    public CraftRes GetAt(int index) => (CraftRes)List[index];
  }
}