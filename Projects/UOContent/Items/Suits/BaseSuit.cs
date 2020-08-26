namespace Server.Items
{
  public abstract class BaseSuit : Item
  {
    public BaseSuit(AccessLevel level, int hue, int itemID) : base(itemID)
    {
      Hue = hue;
      Weight = 1.0;
      Movable = false;
      LootType = LootType.Newbied;
      Layer = Layer.OuterTorso;

      AccessLevel = level;
    }

    public BaseSuit(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.Administrator)]
    public AccessLevel AccessLevel { get; set; }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      writer.Write((int)AccessLevel);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      switch (version)
      {
        case 0:
          {
            AccessLevel = (AccessLevel)reader.ReadInt();
            break;
          }
      }
    }

    public bool Validate()
    {
      if (!(RootParent is Mobile mobile) || mobile.AccessLevel >= AccessLevel)
        return true;

      Delete();
      return false;
    }

    public override void OnSingleClick(Mobile from)
    {
      if (Validate())
        base.OnSingleClick(from);
    }

    public override void OnDoubleClick(Mobile from)
    {
      if (Validate())
        base.OnDoubleClick(from);
    }

    public override bool VerifyMove(Mobile from) => from.AccessLevel >= AccessLevel;

    public override bool OnEquip(Mobile from)
    {
      if (from.AccessLevel < AccessLevel)
        from.SendMessage("You may not wear this.");

      return from.AccessLevel >= AccessLevel;
    }
  }
}
