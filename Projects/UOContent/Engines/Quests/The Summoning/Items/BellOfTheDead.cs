using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom;

[SerializationGenerator(0, false)]
public partial class BellOfTheDead : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private Chyloth _chyloth;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    private SkeletalDragon _dragon;

    [Constructible]
    public BellOfTheDead() : base(0x91A)
    {
        Hue = 0x835;
        Movable = false;
    }

    public override int LabelNumber => 1050018; // bell of the dead

    [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
    public bool Summoning { get; set; }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            BeginSummon(from);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    public virtual void BeginSummon(Mobile from)
    {
        if (_chyloth?.Deleted == false)
        {
            // The ferry man has already been summoned.  There is no need to ring for him again.
            from.SendLocalizedMessage(1050010);
        }
        else if (_dragon?.Deleted == false)
        {
            // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
            from.SendLocalizedMessage(1050017);
        }
        else if (!Summoning)
        {
            Summoning = true;

            Effects.PlaySound(GetWorldLocation(), Map, 0x100);

            Timer.StartTimer(TimeSpan.FromSeconds(8.0), () => EndSummon(from));
        }
    }

    public virtual void EndSummon(Mobile from)
    {
        if (_chyloth?.Deleted == false)
        {
            // The ferry man has already been summoned.  There is no need to ring for him again.
            from.SendLocalizedMessage(1050010);
        }
        else if (_dragon?.Deleted == false)
        {
            // The ferryman has recently been summoned already.  You decide against ringing the bell again so soon.
            from.SendLocalizedMessage(1050017);
        }
        else if (Summoning)
        {
            Summoning = false;

            var loc = GetWorldLocation();

            loc.Z -= 16;

            Effects.SendLocationParticles(
                EffectItem.Create(loc, Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                0,
                0,
                2023,
                0
            );
            Effects.PlaySound(loc, Map, 0x1FE);

            Chyloth = new Chyloth { Direction = (Direction)(7 & (4 + (int)from.GetDirectionTo(loc))) };

            Chyloth.MoveToWorld(loc, Map);

            Chyloth.Bell = this;
            Chyloth.AngryAt = from;
            Chyloth.BeginGiveWarning();
            Chyloth.BeginRemove(TimeSpan.FromSeconds(40.0));
        }
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _chyloth?.Delete();
    }
}
