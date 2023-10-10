using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Regions;

namespace Server.Engines.Quests;

[SerializationGenerator(0, false)]
public partial class HornOfRetreat : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _destLoc;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Map _destMap;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _charges;

    private TimerExecutionToken _timerToken;

    [Constructible]
    public HornOfRetreat() : base(0xFC4)
    {
        Hue = 0x482;
        Weight = 1.0;
        _charges = 10;
    }

    public override int LabelNumber => 1049117; // Horn of Retreat

    public virtual bool ValidateUse(Mobile from) => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060741, _charges); // charges: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            SendLocalizedMessageTo(from, 1042001); // That must be in your pack for you to use it.
            return;
        }

        if (!ValidateUse(from))
        {
            SendLocalizedMessageTo(from, 500309); // Nothing Happens.
        }
        else if (Core.ML && from.Map != Map.Trammel && from.Map != Map.Malas)
        {
            from.SendLocalizedMessage(1076154); // You can only use this in Trammel and Malas.
        }
        else if (_timerToken.Running)
        {
            SendLocalizedMessageTo(from, 1042144); // This is currently in use.
        }
        else if (Charges > 0)
        {
            from.Animate(34, 7, 1, true, false, 0);
            from.PlaySound(0xFF);
            from.SendLocalizedMessage(1049115); // You play the horn and a sense of peace overcomes you...

            --Charges;

            Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => PlayTimer_Callback(from), out _timerToken);
        }
        else
        {
            SendLocalizedMessageTo(from, 1042544); // This item is out of charges.
        }
    }

    public virtual void PlayTimer_Callback(Mobile from)
    {
        _timerToken.Cancel();

        var gate = new HornOfRetreatMoongate(DestLoc, DestMap, from, Hue);

        gate.MoveToWorld(from.Location, from.Map);

        from.PlaySound(0x20E);

        gate.SendLocalizedMessageTo(from, 1049102, from.Name); // Quickly ~1_NAME~! Onward through the gate!
    }
}

[SerializationGenerator(0, false)]
public partial class HornOfRetreatMoongate : Moongate
{
    private readonly Mobile _caster;

    public HornOfRetreatMoongate(Point3D destLoc, Map destMap, Mobile caster, int hue)
    {
        _caster = caster;

        Target = destLoc;
        TargetMap = destMap;

        Hue = hue;
        Light = LightType.Circle300;

        Dispellable = false;

        Timer.StartTimer(TimeSpan.FromSeconds(10.0), Delete);
    }

    public override int LabelNumber => 1049114; // Sanctuary Gate

    public override void BeginConfirmation(Mobile from)
    {
        EndConfirmation(from);
    }

    public override void UseGate(Mobile m)
    {
        if (m.Region.IsPartOf<JailRegion>())
        {
            m.SendLocalizedMessage(1114345); // You'll need a better jailbreak plan than that!
        }
        else if (m == _caster)
        {
            base.UseGate(m);
            Delete();
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }
}
