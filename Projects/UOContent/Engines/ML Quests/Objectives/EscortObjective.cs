using System;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Objectives
{
    public class EscortObjective : BaseObjective
    {
        public EscortObjective(QuestArea destination = null) => Destination = destination;

        public QuestArea Destination { get; set; }

        public override bool CanOffer(IQuestGiver quester, PlayerMobile pm, bool message)
        {
            if (quester is BaseCreature creature && creature.Controlled ||
                quester is BaseEscortable escortable && escortable.IsBeingDeleted)
            {
                return false;
            }

            var context = MLQuestSystem.GetContext(pm);

            if (context != null)
            {
                foreach (var instance in context.QuestInstances)
                {
                    if (instance.Quest.IsEscort)
                    {
                        if (message)
                        {
                            MLQuestSystem.Tell(quester, pm, 500896); // I see you already have an escort.
                        }

                        return false;
                    }
                }
            }

            var nextEscort = pm.LastEscortTime + BaseEscortable.EscortDelay;

            if (nextEscort > Core.Now)
            {
                if (message)
                {
                    var minutes = (int)Math.Ceiling((nextEscort - Core.Now).TotalMinutes);

                    if (minutes == 1)
                    {
                        MLQuestSystem.Tell(quester, pm, 1042722);
                    }
                    else
                    {
                        // You must rest ~1_minsleft~ minutes before we set out on this journey.
                        MLQuestSystem.Tell(quester, pm, 1071195, minutes.ToString());
                    }
                }

                return false;
            }

            return true;
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            g.AddHtmlLocalized(98, y, 312, 16, 1072206, 0x15F90); // Escort to

            if (Destination.Name.Number > 0)
            {
                g.AddHtmlLocalized(173, y, 312, 20, Destination.Name.Number, 0xFFFFFF);
            }
            else if (Destination.Name.String != null)
            {
                g.AddLabel(173, y, 0x481, Destination.Name.String);
            }

            y += 16;
        }

        public override BaseObjectiveInstance CreateInstance(MLQuestInstance instance)
        {
            if (instance == null || Destination == null)
            {
                return null;
            }

            return new EscortObjectiveInstance(this, instance);
        }
    }

    public class EscortObjectiveInstance : BaseObjectiveInstance
    {
        private readonly BaseCreature m_Escort;
        private readonly EscortObjective m_Objective;
        private DateTime m_LastSeenEscorter;
        private TimerExecutionToken _timerToken;

        public EscortObjectiveInstance(EscortObjective objective, MLQuestInstance instance)
            : base(instance, objective)
        {
            m_Objective = objective;
            HasCompleted = false;
            Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), CheckDestination, out _timerToken);
            m_LastSeenEscorter = Core.Now;
            m_Escort = instance.Quester as BaseCreature;

            if (MLQuestSystem.Debug && m_Escort == null && instance.Quester != null)
            {
                Console.WriteLine(
                    "Warning: EscortObjective is not supported for type '{0}'",
                    instance.Quester.GetType().Name
                );
            }
        }

        public bool HasCompleted { get; set; }

        public override DataType ExtraDataType => DataType.EscortObjective;

        public override bool IsCompleted() => HasCompleted;

        private void CheckDestination()
        {
            if (m_Escort == null || HasCompleted) // Completed by deserialization
            {
                StopTimer();
                return;
            }

            var instance = Instance;
            var pm = instance.Player;

            if (instance.Removed)
            {
                Abandon();
            }
            else if (m_Objective.Destination.Contains(m_Escort))
            {
                // We have arrived! I thank thee, ~1_PLAYER_NAME~! I have no further need of thy services. Here is thy pay.
                m_Escort.Say(1042809, pm.Name);

                if (pm.Young || m_Escort.Region.IsPartOf("Haven Island"))
                {
                    Titles.AwardFame(pm, 10, true);
                }
                else
                {
                    VirtueHelper.AwardVirtue(
                        pm,
                        VirtueName.Compassion,
                        m_Escort is BaseEscortable escortable && escortable.IsPrisoner ? 400 : 200
                    );
                }

                EndFollow(m_Escort);
                StopTimer();

                HasCompleted = true;
                CheckComplete();

                // Auto claim reward
                MLQuestSystem.OnDoubleClick(m_Escort, pm);
            }
            else if (pm.Map != m_Escort.Map || !pm.InRange(m_Escort, 30)) // TODO: verify range
            {
                if (m_LastSeenEscorter + BaseEscortable.AbandonDelay <= Core.Now)
                {
                    Abandon();
                }
            }
            else
            {
                m_LastSeenEscorter = Core.Now;
            }
        }

        private void StopTimer()
        {
            _timerToken.Cancel();
        }

        public static void BeginFollow(BaseCreature quester, PlayerMobile pm)
        {
            quester.ControlSlots = 0;
            quester.SetControlMaster(pm);

            quester.ActiveSpeed = 0.1;
            quester.PassiveSpeed = 0.2;

            quester.ControlOrder = OrderType.Follow;
            quester.ControlTarget = pm;

            quester.CantWalk = false;
            quester.CurrentSpeed = 0.1;
        }

        public static void EndFollow(BaseCreature quester)
        {
            quester.ActiveSpeed = 0.2;
            quester.PassiveSpeed = 1.0;

            quester.ControlOrder = OrderType.None;
            quester.ControlTarget = null;

            quester.CurrentSpeed = 1.0;

            quester.SetControlMaster(null);

            (quester as BaseEscortable)?.BeginDelete();
        }

        public override void OnQuestAccepted()
        {
            var instance = Instance;
            var pm = instance.Player;

            pm.LastEscortTime = Core.Now;

            if (m_Escort != null)
            {
                BeginFollow(m_Escort, pm);
            }
        }

        public void Abandon()
        {
            StopTimer();

            var instance = Instance;
            var pm = instance.Player;

            if (m_Escort?.Deleted == false)
            {
                if (!pm.Alive)
                {
                    m_Escort.Say(500901); // Ack!  My escort has come to haunt me!
                }
                else
                {
                    m_Escort.Say(500902); // My escort seems to have abandoned me!
                }

                EndFollow(m_Escort);
            }

            // Note: this sound is sent twice on OSI (once here and once in Cancel())
            // m_Player.SendSound(0x5B3); // private sound
            pm.SendLocalizedMessage(1071194); // You have failed your escort quest...

            if (!instance.Removed)
            {
                instance.Cancel();
            }
        }

        public override void OnQuesterDeleted()
        {
            if (IsCompleted() || Instance.Removed)
            {
                return;
            }

            Abandon();
        }

        public override void OnPlayerDeath()
        {
            // Note: OSI also cancels it when the quest is already complete
            if ( /*IsCompleted() ||*/ Instance.Removed)
            {
                return;
            }

            Instance.Cancel();
        }

        public override void OnExpire()
        {
            Abandon();
        }

        public override void WriteToGump(Gump g, ref int y)
        {
            m_Objective.WriteToGump(g, ref y);

            base.WriteToGump(g, ref y);

            // No extra instance stuff printed for this objective
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(HasCompleted);
        }
    }
}
