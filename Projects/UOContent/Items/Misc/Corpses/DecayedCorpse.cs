using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(2, false)]
public partial class DecayedCorpse : Container
{
    private static TimeSpan _defaultDecayTime = TimeSpan.FromMinutes(7.0);

    [TimerDrift]
    [SerializableField(0, getter: "private", setter: "private")]
    private Timer _decayTimer;

    [DeserializeTimerField(0)]
    private void DeserializeDecayTimer(TimeSpan delay) => BeginDecay(delay);

    public DecayedCorpse(string name) : base(Utility.Random(0xECA, 9))
    {
        Movable = false;
        Name = name;

        BeginDecay(_defaultDecayTime);
    }

    // Do not display (x items, y stones)
    public override bool DisplaysContent => false;

    public void BeginDecay(TimeSpan delay)
    {
        _decayTimer?.Stop();
        DecayTimer = new InternalTimer(this, delay);
        DecayTimer.Start();
    }

    public override void OnAfterDelete()
    {
        _decayTimer?.Stop();
        _decayTimer = null;
    }

    // Do not display (x items, y stones)
    public override bool CheckContentDisplay(Mobile from) => false;

    public override void AddNameProperty(IPropertyList list)
    {
        list.Add(1046414, Name); // the remains of ~1_NAME~
    }

    public override void OnSingleClick(Mobile from)
    {
        LabelTo(from, 1046414, Name); // the remains of ~1_NAME~
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        if (reader.ReadBool())
        {
            BeginDecay(reader.ReadDeltaTime() - Core.Now);
        }
    }

    private class InternalTimer : Timer
    {
        private DecayedCorpse _corpse;

        public InternalTimer(DecayedCorpse c, TimeSpan delay) : base(delay) => _corpse = c;

        protected override void OnTick()
        {
            _corpse.Delete();
        }
    }
}
