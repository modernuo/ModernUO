using System;

namespace Server.Engines.Harvest
{
  public class HarvestTimer : Timer
  {
    private readonly HarvestDefinition m_Definition;
    private readonly Mobile m_From;
    private int m_Index;
    private readonly int m_Count;
    private readonly HarvestSystem m_System;
    private readonly object m_ToHarvest;
    private readonly object m_Locked;
    private readonly Item m_Tool;

    public HarvestTimer(Mobile from, Item tool, HarvestSystem system, HarvestDefinition def, object toHarvest,
      object locked) : base(TimeSpan.Zero, def.EffectDelay)
    {
      m_From = from;
      m_Tool = tool;
      m_System = system;
      m_Definition = def;
      m_ToHarvest = toHarvest;
      m_Locked = locked;
      m_Count = def.EffectCounts.RandomElement();
    }

    protected override void OnTick()
    {
      if (!m_System.OnHarvesting(m_From, m_Tool, m_Definition, m_ToHarvest, m_Locked, ++m_Index == m_Count))
        Stop();
    }
  }
}
