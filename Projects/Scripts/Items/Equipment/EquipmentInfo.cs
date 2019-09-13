namespace Server.Items
{
  public class EquipInfoAttribute
  {
    public EquipInfoAttribute(int number, int charges = -1)
    {
      Number = number;
      Charges = charges;
    }

    public int Number { get; }

    public int Charges { get; }
  }

  public class EquipmentInfo
  {
    public EquipmentInfo(int number, Mobile crafter, bool unidentified, EquipInfoAttribute[] attributes)
    {
      Number = number;
      Crafter = crafter;
      Unidentified = unidentified;
      Attributes = attributes;
    }

    public int Number { get; }

    public Mobile Crafter { get; }

    public bool Unidentified { get; }

    public EquipInfoAttribute[] Attributes { get; }
  }
}
