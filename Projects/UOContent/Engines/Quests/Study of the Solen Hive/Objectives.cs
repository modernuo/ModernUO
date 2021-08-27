using System;
using System.Collections.Generic;

namespace Server.Engines.Quests.Naturalist
{
    public class StudyNestsObjective : QuestObjective
    {
        private readonly List<NestArea> m_StudiedNests = new();
        private NestArea m_CurrentNest;
        private DateTime m_StudyBegin;
        private StudyState m_StudyState;

        public override object Message => 1054044;

        public override int MaxProgress => NestArea.NonSpecialCount;

        public bool StudiedSpecialNest { get; private set; }

        public override bool GetTimerEvent() => true;

        public override void CheckProgress()
        {
            var from = System.From;

            if (m_CurrentNest != null)
            {
                var nest = m_CurrentNest;

                if ((from.Map == Map.Trammel || from.Map == Map.Felucca) && nest.Contains(from.Location))
                {
                    if (m_StudyState != StudyState.Inactive)
                    {
                        var time = Core.Now - m_StudyBegin;

                        if (time > TimeSpan.FromSeconds(30.0))
                        {
                            m_StudiedNests.Add(nest);
                            m_StudyState = StudyState.Inactive;

                            if (m_CurrentNest.Special)
                            {
                                // You complete your examination of this bizarre Egg Nest. The Naturalist will undoubtedly be quite interested in these notes!
                                from.SendLocalizedMessage(1054057);
                                StudiedSpecialNest = true;
                            }
                            else
                            {
                                // You have completed your study of this Solen Egg Nest. You put your notes away.
                                from.SendLocalizedMessage(1054054);
                                CurProgress++;
                            }
                        }
                        else if (m_StudyState == StudyState.FirstStep && time > TimeSpan.FromSeconds(15.0))
                        {
                            if (!nest.Special)
                            {
                                // You begin recording your completed notes on a bit of parchment.
                                from.SendLocalizedMessage(1054058);
                            }

                            m_StudyState = StudyState.SecondStep;
                        }
                    }
                }
                else
                {
                    if (m_StudyState != StudyState.Inactive)
                    {
                        // You abandon your study of the Solen Egg Nest without gathering the needed information.
                        from.SendLocalizedMessage(1054046);
                    }

                    m_CurrentNest = null;
                }
            }
            else if (from.Map == Map.Trammel || from.Map == Map.Felucca)
            {
                var nest = NestArea.Find(from.Location);

                if (nest != null)
                {
                    m_CurrentNest = nest;
                    m_StudyBegin = Core.Now;

                    if (m_StudiedNests.Contains(nest))
                    {
                        m_StudyState = StudyState.Inactive;

                        // You glance at the Egg Nest, realizing you've already studied this one.
                        from.SendLocalizedMessage(1054047);
                    }
                    else
                    {
                        m_StudyState = StudyState.FirstStep;

                        if (nest.Special)
                        {
                            // You notice something very odd about this Solen Egg Nest. You begin taking notes.
                            from.SendLocalizedMessage(105405);
                        }
                        else
                        {
                            // You begin studying the Solen Egg Nest to gather information.
                            from.SendLocalizedMessage(1054045);
                        }

                        if (from.Female)
                        {
                            from.PlaySound(0x30B);
                        }
                        else
                        {
                            from.PlaySound(0x419);
                        }
                    }
                }
            }
        }

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (!Completed)
            {
                gump.AddHtmlLocalized(70, 260, 270, 100, 1054055, BaseQuestGump.Blue); // Solen Nests Studied :
                gump.AddLabel(70, 280, 0x64, CurProgress.ToString());
                gump.AddLabel(100, 280, 0x64, "/");
                gump.AddLabel(130, 280, 0x64, MaxProgress.ToString());
            }
            else
            {
                base.RenderProgress(gump);
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ReturnToNaturalistObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var nest = NestArea.GetByID(reader.ReadEncodedInt());
                m_StudiedNests.Add(nest);
            }

            StudiedSpecialNest = reader.ReadBool();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_StudiedNests.Count);
            foreach (var nest in m_StudiedNests)
            {
                writer.WriteEncodedInt(nest.ID);
            }

            writer.Write(StudiedSpecialNest);
        }

        private enum StudyState
        {
            Inactive,
            FirstStep,
            SecondStep
        }
    }

    public class ReturnToNaturalistObjective : QuestObjective
    {
        public override object Message => 1054048;

        public override void RenderProgress(BaseQuestGump gump)
        {
            var count = NestArea.NonSpecialCount.ToString();

            gump.AddHtmlLocalized(70, 260, 270, 100, 1054055, BaseQuestGump.Blue); // Solen Nests Studied :
            gump.AddLabel(70, 280, 0x64, count);
            gump.AddLabel(100, 280, 0x64, "/");
            gump.AddLabel(130, 280, 0x64, count);
        }
    }
}
