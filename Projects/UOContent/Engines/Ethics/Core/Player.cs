using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Ethics;

[PropertyObject]
[SerializationGenerator(1)]
public partial class Player : EthicsEntity
{
    [SerializableField(0, setter: "private")]
    private Mobile _mobile;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private int _power;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private int _history;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private Mobile _steed;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private Mobile _familiar;

    [DeltaDateTime]
    [SerializableField(5, setter: "private")]
    private DateTime _shield;

    [SerializableField(6, setter: "private")]
    private Ethic _ethic;

    public Player(Ethic ethic, Mobile mobile)
    {
        _ethic = ethic;
        _mobile = mobile;

        _power = 5;
        _history = 5;
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        // Don't call the base Deserialize

        _mobile = reader.ReadEntity<Mobile>();

        _power = reader.ReadEncodedInt();
        _history = reader.ReadEncodedInt();

        _steed = reader.ReadEntity<Mobile>();
        _familiar = reader.ReadEntity<Mobile>();

        _shield = reader.ReadDeltaTime();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsShielded
    {
        get
        {
            if (_shield == DateTime.MinValue)
            {
                return false;
            }

            if (Core.Now < _shield + TimeSpan.FromHours(1.0))
            {
                return true;
            }

            FinishShield();
            return false;
        }
    }

    public static Player Find(Mobile mob) => Find(mob, false);

    public static Player Find(Mobile mob, bool inherit)
    {
        var pm = mob as PlayerMobile;

        if (pm == null)
        {
            if (inherit && mob is BaseCreature bc)
            {
                pm = bc.GetMaster() as PlayerMobile;
            }

            if (pm == null)
            {
                return null;
            }
        }

        var pl = pm.EthicPlayer;

        if (pl?.Ethic.IsEligible(pl.Mobile) == false)
        {
            pm.EthicPlayer = pl = null;
        }

        return pl;
    }

    public void BeginShield() => _shield = Core.Now;

    public void FinishShield() => _shield = DateTime.MinValue;

    public void CheckAttach()
    {
        if (Ethic.IsEligible(Mobile))
        {
            Attach();
        }
    }

    public void Attach()
    {
        if (Mobile is PlayerMobile mobile)
        {
            mobile.EthicPlayer = this;
        }

        Ethic.Players.Add(this);
    }

    public void Detach()
    {
        if (Mobile is PlayerMobile mobile)
        {
            mobile.EthicPlayer = null;
        }

        Ethic.Players.Remove(this);
    }
}
