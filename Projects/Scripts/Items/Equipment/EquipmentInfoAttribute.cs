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
}
