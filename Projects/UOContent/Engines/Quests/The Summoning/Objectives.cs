using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Doom
{
    public class CollectBonesObjective : QuestObjective
    {
        public override object Message => 1050026;

        public override int MaxProgress => 1000;

        public override void OnComplete()
        {
            var victoria = ((TheSummoningQuest)System).Victoria;

            if (victoria == null)
            {
                System.From.SendMessage("Internal error: unable to find Victoria. Quest unable to continue.");
                System.Cancel();
            }
            else
            {
                var altar = victoria.Altar;

                if (altar == null)
                {
                    System.From.SendMessage("Internal error: unable to find summoning altar. Quest unable to continue.");
                    System.Cancel();
                }
                else if (altar.Daemon?.Alive != true)
                {
                    System.AddConversation(new VanquishDaemonConversation());
                }
                else
                {
                    victoria.SayTo(
                        System.From,
                        "The devourer has already been summoned. Return when the devourer has been slain and I will summon it for you."
                    );
                    ((TheSummoningQuest)System).WaitForSummon = true;
                }
            }
        }

        public override void RenderMessage(BaseQuestGump gump)
        {
            if (CurProgress > 0 && CurProgress < MaxProgress)
            {
                // Victoria has accepted the Daemon bones, but the requirement is not yet met.
                gump.AddHtmlObject(70, 130, 300, 100, 1050028, BaseQuestGump.Blue, false, false);
            }
            else
            {
                base.RenderMessage(gump);
            }
        }

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (CurProgress > 0 && CurProgress < MaxProgress)
            {
                // Number of bones collected:
                gump.AddHtmlObject(70, 260, 270, 100, 1050019, BaseQuestGump.Blue, false, false);

                gump.AddLabel(70, 280, 100, CurProgress.ToString());
                gump.AddLabel(100, 280, 100, "/");
                gump.AddLabel(130, 280, 100, MaxProgress.ToString());
            }
            else
            {
                base.RenderProgress(gump);
            }
        }
    }

    public class VanquishDaemonObjective : QuestObjective
    {
        private BoneDemon m_Daemon;

        public VanquishDaemonObjective(BoneDemon daemon) => m_Daemon = daemon;

        // Serialization
        public VanquishDaemonObjective()
        {
        }

        public Corpse CorpseWithSkull { get; set; }

        public override object Message => 1050037;

        public override void CheckProgress()
        {
            if (m_Daemon?.Alive != true)
            {
                Complete();
            }
        }

        public override void OnComplete()
        {
            var victoria = ((TheSummoningQuest)System).Victoria;

            var altar = victoria?.Altar;

            altar?.CheckDaemon();

            var from = System.From;

            if (!from.Alive)
            {
                // The devourer lies dead, unfortunately so do you.  You cannot claim your reward while dead.  You will need to face him again.
                from.SendLocalizedMessage(1050033);
                ((TheSummoningQuest)System).WaitForSummon = true;
            }
            else
            {
                var hasRights = false;

                if (m_Daemon != null)
                {
                    var lootingRights =
                        BaseCreature.GetLootingRights(m_Daemon.DamageEntries, m_Daemon.HitsMax);

                    for (var i = 0; i < lootingRights.Count; ++i)
                    {
                        var ds = lootingRights[i];

                        if (ds.m_HasRight && ds.m_Mobile == from)
                        {
                            hasRights = true;
                            break;
                        }
                    }
                }

                if (!hasRights)
                {
                    // The devourer lies dead.  Unfortunately you did not sufficiently prove your worth in combating the devourer.  Victoria shall summon another incarnation of the devourer to the circle of stones.  Try again noble adventurer.
                    from.SendLocalizedMessage(1050034);
                    ((TheSummoningQuest)System).WaitForSummon = true;
                }
                else
                {
                    from.SendLocalizedMessage(1050035); // The devourer lies dead.  Search his corpse to claim your prize!

                    if (m_Daemon != null)
                    {
                        CorpseWithSkull = m_Daemon.Corpse as Corpse;
                    }
                }
            }
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Daemon = reader.ReadEntity<BoneDemon>();
            CorpseWithSkull = reader.ReadEntity<Corpse>();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(m_Daemon);
            writer.Write(CorpseWithSkull);
        }
    }
}
