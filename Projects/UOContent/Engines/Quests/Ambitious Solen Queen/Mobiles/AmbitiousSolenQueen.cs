using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ambitious;

[SerializationGenerator(0)]
public abstract partial class BaseAmbitiousSolenQueen : BaseQuester
{
    public BaseAmbitiousSolenQueen()
    {
    }

    public abstract bool RedSolen { get; }
    public override string DefaultName => "an ambitious solen queen";
    public override bool DisallowAllMoves => false;

    public override void InitBody()
    {
        Body = 0x30F;

        if (!RedSolen)
        {
            Hue = 0x453;
        }

        SpeechHue = 0;
    }

    public override int GetIdleSound() => 0x10D;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        Direction = GetDirectionTo(player);

        if (player.Quest is AmbitiousQueenQuest qs && qs.RedSolen == RedSolen)
        {
            if (qs.IsObjectiveInProgress(typeof(KillQueensObjective)))
            {
                qs.AddConversation(new DuringKillQueensConversation());
            }
            else
            {
                QuestObjective obj = qs.FindObjective<ReturnAfterKillsObjective>();

                if (obj?.Completed == false)
                {
                    obj.Complete();
                }
                else if (qs.IsObjectiveInProgress(typeof(GatherFungiObjective)))
                {
                    qs.AddConversation(new DuringFungiGatheringConversation());
                }
                else
                {
                    var lastObj = qs.FindObjective<GetRewardObjective>();

                    if (lastObj?.Completed == false)
                    {
                        var bagOfSending = lastObj.BagOfSending;
                        var powderOfTranslocation = lastObj.PowderOfTranslocation;
                        var gold = lastObj.Gold;

                        AmbitiousQueenQuest.GiveRewardTo(player, ref bagOfSending, ref powderOfTranslocation, ref gold);

                        lastObj.BagOfSending = bagOfSending;
                        lastObj.PowderOfTranslocation = powderOfTranslocation;
                        lastObj.Gold = gold;

                        if (!bagOfSending && !powderOfTranslocation && !gold)
                        {
                            lastObj.Complete();
                        }
                        else
                        {
                            qs.AddConversation(
                                new FullBackpackConversation(
                                    false,
                                    lastObj.BagOfSending,
                                    lastObj.PowderOfTranslocation,
                                    lastObj.Gold
                                )
                            );
                        }
                    }
                }
            }
        }
        else
        {
            QuestSystem newQuest = new AmbitiousQueenQuest(player, RedSolen);

            if (player.Quest == null && QuestSystem.CanOfferQuest(player, typeof(AmbitiousQueenQuest)))
            {
                newQuest.SendOffer();
            }
            else
            {
                newQuest.AddConversation(new DontOfferConversation());
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        Direction = GetDirectionTo(from);

        if (from is not PlayerMobile { Quest: AmbitiousQueenQuest qs } player || qs.RedSolen != RedSolen)
        {
            return base.OnDragDrop(from, dropped);
        }

        QuestObjective obj = qs.FindObjective<GatherFungiObjective>();

        if (obj?.Completed != false || dropped is not ZoogiFungus fungi)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (fungi.Amount < 50)
        {
            // Our arrangement was for 50 of the zoogi fungus. Please return to me when you have that amount.
            SayTo(player, 1054072);
            return false;
        }

        obj.Complete();

        fungi.Consume(50);
        return fungi.Deleted;
    }
}

[SerializationGenerator(0)]
public partial class RedAmbitiousSolenQueen : BaseAmbitiousSolenQueen
{
    [Constructible]
    public RedAmbitiousSolenQueen()
    {
    }

    public override bool RedSolen => true;
}

[SerializationGenerator(0)]
public partial class BlackAmbitiousSolenQueen : BaseAmbitiousSolenQueen
{
    [Constructible]
    public BlackAmbitiousSolenQueen()
    {
    }

    public override bool RedSolen => false;
}
