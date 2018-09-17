using System;
using System.Collections;

namespace Server.Engines.Craft
{
  public class CraftSubResCol : CollectionBase
  {
    public CraftSubResCol()
    {
      Init = false;
    }

    public bool Init{ get; set; }

    public Type ResType{ get; set; }

    public string NameString{ get; set; }

    public int NameNumber{ get; set; }

    public void Add(CraftSubRes craftSubRes)
    {
      List.Add(craftSubRes);
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

    public CraftSubRes GetAt(int index)
    {
      return (CraftSubRes)List[index];
    }

    public CraftSubRes SearchFor(Type type)
    {
      for (int i = 0; i < List.Count; i++)
      {
        CraftSubRes craftSubRes = (CraftSubRes)List[i];
        if (craftSubRes.ItemType == type) return craftSubRes;
      }

      return null;
    }
  }
}