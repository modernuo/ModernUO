using System.Collections.Generic;

namespace Server.Engines.Craft
{
  public class CraftGroup
  {
    public CraftGroup(TextDefinition groupName)
    {
      NameNumber = groupName;
      NameString = groupName;
      CraftItems = new List<CraftItem>();
    }

    public List<CraftItem> CraftItems { get; }

    public string NameString { get; }

    public int NameNumber { get; }

    public void AddCraftItem(CraftItem craftItem)
    {
      CraftItems.Add(craftItem);
    }
  }
}
