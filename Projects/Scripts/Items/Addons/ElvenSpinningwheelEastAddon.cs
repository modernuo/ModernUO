using System;

namespace Server.Items
{
  public class ElvenSpinningwheelEastAddon : BaseAddon, ISpinningWheel
  {
    private Timer m_Timer;

    [Constructible]
    public ElvenSpinningwheelEastAddon()
    {
      AddComponent(new AddonComponent(0x2DD9), 0, 0, 0);
    }

    public ElvenSpinningwheelEastAddon(Serial serial) : base(serial)
    {
    }

    public override BaseAddonDeed Deed => new ElvenSpinningwheelEastDeed();

    public bool Spinning => m_Timer != null;

    public void BeginSpin(SpinCallback callback, Mobile from, int hue)
    {
      m_Timer = new SpinTimer(this, callback, from, hue);
      m_Timer.Start();

      foreach (AddonComponent c in Components)
        if (c.ItemID == 0x2DD9 || c.ItemID == 0x101C || c.ItemID == 0x10A4)
          ++c.ItemID;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }

    public override void OnComponentLoaded(AddonComponent c)
    {
      switch (c.ItemID)
      {
        case 0x2E3D:
        case 0x101D:
        case 0x10A5:
          --c.ItemID;
          break;
      }
    }

    public void EndSpin(SpinCallback callback, Mobile from, int hue)
    {
      m_Timer?.Stop();

      m_Timer = null;

      foreach (AddonComponent c in Components)
        if (c.ItemID == 0x1016 || c.ItemID == 0x101A || c.ItemID == 0x101D || c.ItemID == 0x10A5)
          --c.ItemID;

      callback?.Invoke(this, from, hue);
    }

    private class SpinTimer : Timer
    {
      private SpinCallback m_Callback;
      private Mobile m_From;
      private int m_Hue;
      private ElvenSpinningwheelEastAddon m_Wheel;

      public SpinTimer(ElvenSpinningwheelEastAddon wheel, SpinCallback callback, Mobile from, int hue) : base(
        TimeSpan.FromSeconds(3.0))
      {
        m_Wheel = wheel;
        m_Callback = callback;
        m_From = from;
        m_Hue = hue;
        Priority = TimerPriority.TwoFiftyMS;
      }

      protected override void OnTick()
      {
        m_Wheel.EndSpin(m_Callback, m_From, m_Hue);
      }
    }
  }

  public class ElvenSpinningwheelEastDeed : BaseAddonDeed
  {
    [Constructible]
    public ElvenSpinningwheelEastDeed()
    {
    }

    public ElvenSpinningwheelEastDeed(Serial serial) : base(serial)
    {
    }

    public override BaseAddon Addon => new ElvenSpinningwheelEastAddon();
    public override int LabelNumber => 1073393; // elven spinning wheel (east)

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.WriteEncodedInt(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadEncodedInt();
    }
  }
}
