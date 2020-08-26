using System;

namespace Server.Items
{
  public class TransientItem : Item
  {
    private Timer m_Timer;

    [Constructible]
    public TransientItem(int itemID, TimeSpan lifeSpan)
      : base(itemID)
    {
      CreationTime = DateTime.UtcNow;
      LifeSpan = lifeSpan;

      m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry);
    }

    public TransientItem(Serial serial)
      : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public TimeSpan LifeSpan { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime CreationTime { get; set; }

    public override bool Nontransferable => true;

    public virtual TextDefinition InvalidTransferMessage => null;

    public override void HandleInvalidTransfer(Mobile from)
    {
      if (InvalidTransferMessage != null)
        TextDefinition.SendMessageTo(from, InvalidTransferMessage);

      Delete();
    }

    public virtual void Expire(Mobile parent)
    {
      parent?.SendLocalizedMessage(1072515, Name ?? $"#{LabelNumber}"); // The ~1_name~ expired...

      Effects.PlaySound(GetWorldLocation(), Map, 0x201);

      Delete();
    }

    public virtual void SendTimeRemainingMessage(Mobile to)
    {
      to.SendLocalizedMessage(1072516,
        $"{Name ?? $"#{LabelNumber}"}\t{(int)LifeSpan.TotalSeconds}"); // ~1_name~ will expire in ~2_val~ seconds!
    }

    public override void OnDelete()
    {
      m_Timer?.Stop();

      base.OnDelete();
    }

    public virtual void CheckExpiry()
    {
      if (CreationTime + LifeSpan < DateTime.UtcNow)
        Expire(RootParent as Mobile);
      else
        InvalidateProperties();
    }

    public override void GetProperties(ObjectPropertyList list)
    {
      base.GetProperties(list);

      TimeSpan remaining = CreationTime + LifeSpan - DateTime.UtcNow;

      list.Add(1072517, ((int)remaining.TotalSeconds).ToString()); // Lifespan: ~1_val~ seconds
    }

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);

      writer.Write(LifeSpan);
      writer.Write(CreationTime);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();

      LifeSpan = reader.ReadTimeSpan();
      CreationTime = reader.ReadDateTime();

      m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckExpiry);
    }
  }
}