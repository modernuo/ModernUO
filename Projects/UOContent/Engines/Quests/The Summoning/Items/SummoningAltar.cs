using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class SummoningAltar : AbbatoirAddon
{
    [Constructible]
    public SummoningAltar()
    {
    }

    [SerializableProperty(0)]
    public BoneDemon Daemon
    {
        get => _daemon;
        set
        {
            _daemon = value;
            CheckDaemon();
            this.MarkDirty();
        }
    }

    public void CheckDaemon()
    {
        if (_daemon?.Alive != true)
        {
            _daemon = null;
            Hue = 0;
        }
        else
        {
            Hue = 0x66D;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        CheckDaemon();
    }
}
