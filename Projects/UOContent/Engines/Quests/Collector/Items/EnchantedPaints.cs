using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class EnchantedPaints : QuestItem
{
    [Constructible]
    public EnchantedPaints() : base(0xFC1)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override bool CanDrop(PlayerMobile player) => player.Quest is not CollectorQuest;

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile player)
        {
            var qs = player.Quest;

            if (qs is CollectorQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(CaptureImagesObjective)))
                {
                    player.SendAsciiMessage(0x59, "Target the creature whose image you wish to create.");
                    player.Target = new InternalTarget(this);

                    return;
                }
            }
        }

        from.SendLocalizedMessage(1010085); // You cannot use this.
    }

    private class InternalTarget : Target
    {
        private readonly EnchantedPaints m_Paints;

        public InternalTarget(EnchantedPaints paints) : base(-1, false, TargetFlags.None)
        {
            CheckLOS = false;
            m_Paints = paints;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Paints.Deleted || !m_Paints.IsChildOf(from.Backpack))
            {
                return;
            }

            if (from is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is not CollectorQuest)
                {
                    return;
                }

                var obj = qs.FindObjective<CaptureImagesObjective>();

                if (obj?.Completed != false)
                {
                    return;
                }

                if (targeted is Mobile)
                {
                    var response = obj.CaptureImage(
                        targeted is GreaterMongbat ? typeof(Mongbat) : targeted.GetType(),
                        out var image
                    );

                    switch (response)
                    {
                        case CaptureResponse.Valid:
                            {
                                // The enchanted paints swirl for a moment then an image begins to take shape. *Click*
                                player.SendLocalizedMessage(1055125);
                                player.AddToBackpack(new PaintedImage(image));

                                break;
                            }
                        case CaptureResponse.AlreadyDone:
                            {
                                player.SendAsciiMessage(0x2C, "You have already captured the image of this creature");

                                break;
                            }
                        case CaptureResponse.Invalid:
                            {
                                // You have no interest in capturing the image of this creature.
                                player.SendLocalizedMessage(1055124);

                                break;
                            }
                    }
                }
                else
                {
                    player.SendAsciiMessage(0x35, "You have no interest in that.");
                }

                return;
            }

            from.SendLocalizedMessage(1010085); // You cannot use this.
        }
    }
}
