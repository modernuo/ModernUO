using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ambitious
{
    public abstract class BaseAmbitiousSolenQueen : BaseQuester
    {
        public BaseAmbitiousSolenQueen()
        {
        }

        public BaseAmbitiousSolenQueen(Serial serial) : base(serial)
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

            if (from is PlayerMobile player)
            {
                if (player.Quest is AmbitiousQueenQuest qs && qs.RedSolen == RedSolen)
                {
                    QuestObjective obj = qs.FindObjective<GatherFungiObjective>();

                    if (obj?.Completed == false)
                    {
                        if (dropped is ZoogiFungus fungi)
                        {
                            if (fungi.Amount >= 50)
                            {
                                obj.Complete();

                                fungi.Amount -= 50;

                                if (fungi.Amount == 0)
                                {
                                    fungi.Delete();
                                    return true;
                                }

                                return false;
                            }

                            SayTo(
                                player,
                                1054072
                            ); // Our arrangement was for 50 of the zoogi fungus. Please return to me when you have that amount.
                            return false;
                        }
                    }
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class RedAmbitiousSolenQueen : BaseAmbitiousSolenQueen
    {
        [Constructible]
        public RedAmbitiousSolenQueen()
        {
        }

        public RedAmbitiousSolenQueen(Serial serial) : base(serial)
        {
        }

        public override bool RedSolen => true;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class BlackAmbitiousSolenQueen : BaseAmbitiousSolenQueen
    {
        [Constructible]
        public BlackAmbitiousSolenQueen()
        {
        }

        public BlackAmbitiousSolenQueen(Serial serial) : base(serial)
        {
        }

        public override bool RedSolen => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }
}
