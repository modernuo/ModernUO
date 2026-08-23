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

    [SerializableField(0, fieldChanged: nameof(OnDaemonChanged))]
    private BoneDemon _daemon;

    private void OnDaemonChanged(BoneDemon oldValue, BoneDemon newValue)
    {
        CheckDaemon();
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
