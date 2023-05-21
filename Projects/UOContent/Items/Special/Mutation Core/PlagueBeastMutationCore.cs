using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastMutationCore : Item, IScissorable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _cut;

    [Constructible]
    public PlagueBeastMutationCore() : base(0x1CF0)
    {
        _cut = true;
        Weight = 1.0;
        Hue = 0x480;
    }

    public override int LabelNumber => 1153760; // a plague beast mutation core

    public virtual bool Scissor(Mobile from, Scissors scissors)
    {
        if (!_cut)
        {
            var owner = RootParent as PlagueBeastLord;

            Cut = true;
            Movable = true;

            from.AddToBackpack(this);

            // * You remove the plague mutation core from the plague beast, causing it to dissolve into a pile of goo *
            from.LocalOverheadMessage(MessageType.Regular, 0x34, 1071906);

            if (owner != null)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(1),
                    () =>
                    {
                        owner.Unfreeze();
                        owner.Kill();
                    }
                );
            }

            return true;
        }

        return false;
    }
}
