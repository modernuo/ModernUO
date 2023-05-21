using System;
using System.Collections.Generic;
using Server.Engines.MLQuests.Gumps;
using Server.Engines.MLQuests.Objectives;
using Server.Mobiles;

namespace Server.Engines.MLQuests
{
    [Flags]
    public enum MLQuestInstanceFlags : byte
    {
        None = 0x00,
        ClaimReward = 0x01,
        Removed = 0x02,
        Failed = 0x04
    }

    public class MLQuestInstance
    {
        private MLQuestInstanceFlags m_Flags;
        private IQuestGiver m_Quester;

        private TimerExecutionToken _timerToken;

        public MLQuestInstance(MLQuest quest, IQuestGiver quester, PlayerMobile player)
        {
            Quest = quest;

            m_Quester = quester;
            QuesterType = quester?.GetType();
            Player = player;

            Accepted = Core.Now;
            m_Flags = MLQuestInstanceFlags.None;

            Objectives = new BaseObjectiveInstance[quest.Objectives.Count];

            BaseObjectiveInstance obj;
            var timed = false;

            for (var i = 0; i < quest.Objectives.Count; ++i)
            {
                Objectives[i] = obj = quest.Objectives[i].CreateInstance(this);

                if (obj.IsTimed)
                {
                    timed = true;
                }
            }

            Register();

            if (timed)
            {
                Timer.StartTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Slice, out _timerToken);
            }
        }

        public MLQuest Quest { get; set; }

        public IQuestGiver Quester
        {
            get => m_Quester;
            set
            {
                m_Quester = value;
                QuesterType = value?.GetType();
            }
        }

        public Type QuesterType { get; private set; }

        public PlayerMobile Player { get; set; }

        public MLQuestContext PlayerContext => MLQuestSystem.GetOrCreateContext(Player);

        public DateTime Accepted { get; set; }

        public bool ClaimReward
        {
            get => GetFlag(MLQuestInstanceFlags.ClaimReward);
            set => SetFlag(MLQuestInstanceFlags.ClaimReward, value);
        }

        public bool Removed
        {
            get => GetFlag(MLQuestInstanceFlags.Removed);
            set => SetFlag(MLQuestInstanceFlags.Removed, value);
        }

        public bool Failed
        {
            get => GetFlag(MLQuestInstanceFlags.Failed);
            set => SetFlag(MLQuestInstanceFlags.Failed, value);
        }

        public BaseObjectiveInstance[] Objectives { get; set; }

        public bool SkipReportBack => Quest.CompletionMessage.IsNullOrEmpty();

        private void Register()
        {
            Quest?.Instances?.Add(this);

            if (Player != null)
            {
                PlayerContext.QuestInstances.Add(this);
            }
        }

        private void Unregister()
        {
            Quest?.Instances?.Remove(this);

            if (Player != null)
            {
                PlayerContext.QuestInstances.Remove(this);
            }

            Removed = true;
        }

        public bool AllowsQuestItem(Item item, Type type)
        {
            foreach (var objective in Objectives)
            {
                if (!objective.Expired && objective.AllowsQuestItem(item, type))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsCompleted()
        {
            var requiresAll = Quest.ObjectiveType == ObjectiveType.All;

            foreach (var obj in Objectives)
            {
                var complete = obj.IsCompleted();

                if (complete && !requiresAll)
                {
                    return true;
                }

                if (!complete && requiresAll)
                {
                    return false;
                }
            }

            return requiresAll;
        }

        public void CheckComplete()
        {
            if (IsCompleted())
            {
                Player.PlaySound(0x5B5); // public sound

                foreach (var obj in Objectives)
                {
                    obj.OnQuestCompleted();
                }

                Quest.CompletionNotice.SendMessageTo(Player, 0x23);

                /*
                 * Advance to the ClaimReward=true stage if this quest has no
                 * completion message to show anyway. This suppresses further
                 * triggers of CheckComplete.
                 *
                 * For quests that require collections, this is done later when
                 * the player double clicks the quester.
                 */
                // An OnQuestCompleted can potentially have removed this instance already
                if (!Removed && SkipReportBack && !Quest.RequiresCollection)
                {
                    ContinueReportBack(false);
                }
            }
        }

        public void Fail()
        {
            Failed = true;
        }

        private void Slice()
        {
            if (ClaimReward || Removed)
            {
                StopTimer();
                return;
            }

            var hasAnyFails = false;
            var hasAnyLeft = false;

            foreach (var obj in Objectives)
            {
                if (!obj.Expired)
                {
                    if (obj.IsTimed && obj.EndTime <= Core.Now)
                    {
                        Player.SendLocalizedMessage(1072258); // You failed to complete an objective in time!

                        obj.Expired = true;
                        obj.OnExpire();

                        hasAnyFails = true;
                    }
                    else
                    {
                        hasAnyLeft = true;
                    }
                }
            }

            if (Quest.ObjectiveType == ObjectiveType.All && hasAnyFails || !hasAnyLeft)
            {
                Fail();
            }

            if (!hasAnyLeft)
            {
                StopTimer();
            }
        }

        public void SendProgressGump()
        {
            Player.SendGump(new QuestConversationGump(Quest, Player, Quest.InProgressMessage));
        }

        public void SendRewardOffer()
        {
            Quest.GetRewards(this);
        }

        // TODO: Split next quest stuff from SendRewardGump stuff?
        public void SendRewardGump()
        {
            var nextQuestType = Quest.NextQuest;

            if (nextQuestType != null)
            {
                ClaimRewards(); // skip reward gump

                if (Removed) // rewards were claimed successfully
                {
                    var nextQuest = MLQuestSystem.FindQuest(nextQuestType);

                    nextQuest?.SendOffer(m_Quester, Player);
                }
            }
            else
            {
                Player.SendGump(new QuestRewardGump(this));
            }
        }

        public void SendReportBackGump()
        {
            if (SkipReportBack)
            {
                ContinueReportBack(true); // skip ahead
            }
            else
            {
                Player.SendGump(new QuestReportBackGump(this));
            }
        }

        public void ContinueReportBack(bool sendRewardGump)
        {
            // There is a backpack check here on OSI for the rewards as well (even though it's not needed...)

            if (Quest.ObjectiveType == ObjectiveType.All)
            {
                // TODO: 1115877 - You no longer have the required items to complete this quest.
                foreach (var objective in Objectives)
                {
                    if (!objective.IsCompleted())
                    {
                        return;
                    }
                }

                foreach (var objective in Objectives)
                {
                    if (!objective.OnBeforeClaimReward())
                    {
                        return;
                    }
                }

                foreach (var objective in Objectives)
                {
                    objective.OnClaimReward();
                }
            }
            else
            {
                /* The following behavior is unverified, as OSI (currently) has no collect quest requiring
                 * only one objective to be completed. It is assumed that only one objective is claimed
                 * (the first completed one), even when multiple are complete.
                 */
                var complete = false;

                foreach (var objective in Objectives)
                {
                    if (objective.IsCompleted())
                    {
                        if (objective.OnBeforeClaimReward())
                        {
                            complete = true;
                            objective.OnClaimReward();
                        }

                        break;
                    }
                }

                if (!complete)
                {
                    return;
                }
            }

            ClaimReward = true;

            if (Quest.HasRestartDelay)
            {
                PlayerContext.SetDoneQuest(Quest, Core.Now + Quest.GetRestartDelay());
            }

            // This is correct for ObjectiveType.Any as well
            foreach (var objective in Objectives)
            {
                objective.OnAfterClaimReward();
            }

            if (sendRewardGump)
            {
                SendRewardOffer();
            }
        }

        public void ClaimRewards()
        {
            if (Quest == null || Player?.Deleted != false || !ClaimReward || Removed)
            {
                return;
            }

            var rewards = new List<Item>();

            foreach (var reward in Quest.Rewards)
            {
                reward.AddRewardItems(Player, rewards);
            }

            if (rewards.Count != 0)
            {
                // On OSI a more naive method of checking is used.
                // For containers, only the actual container item counts.
                var canFit = true;

                foreach (var rewardItem in rewards)
                {
                    if (!Player.AddToBackpack(rewardItem))
                    {
                        canFit = false;
                        break;
                    }
                }

                if (!canFit)
                {
                    foreach (var rewardItem in rewards)
                    {
                        rewardItem.Delete();
                    }

                    // Your backpack is full. You cannot complete the quest and receive your reward.
                    Player.SendLocalizedMessage(1078524);
                    return;
                }

                foreach (var rewardItem in rewards)
                {
                    var rewardName = rewardItem.Name ?? $"#{rewardItem.LabelNumber}";

                    if (rewardItem.Stackable)
                    {
                        // You receive a reward: ~1_QUANTITY~ ~2_ITEM~
                        Player.SendLocalizedMessage(1115917, $"{rewardItem.Amount}\t{rewardName}");
                    }
                    else
                    {
                        Player.SendLocalizedMessage(1074360, rewardName); // You receive a reward: ~1_REWARD~
                    }
                }
            }

            foreach (var objective in Objectives)
            {
                objective.OnRewardClaimed();
            }

            Quest.OnRewardClaimed(this);

            var context = PlayerContext;

            if (Quest.RecordCompletion && !Quest.HasRestartDelay) // Quests with restart delays are logged earlier as per OSI
            {
                context.SetDoneQuest(Quest);
            }

            if (Quest.IsChainTriggered)
            {
                context.ChainOffers.Remove(Quest);
            }

            var nextQuestType = Quest.NextQuest;

            if (nextQuestType != null)
            {
                var nextQuest = MLQuestSystem.FindQuest(nextQuestType);

                if (nextQuest != null && !context.ChainOffers.Contains(nextQuest))
                {
                    context.ChainOffers.Add(nextQuest);
                }
            }

            Remove();
        }

        public void Cancel()
        {
            Cancel(false);
        }

        public void Cancel(bool removeChain)
        {
            Remove();

            Player.SendSound(0x5B3); // private sound

            foreach (var obj in Objectives)
            {
                obj.OnQuestCancelled();
            }

            Quest.OnCancel(this);

            if (removeChain)
            {
                PlayerContext.ChainOffers.Remove(Quest);
            }
        }

        public void Remove()
        {
            Unregister();
            StopTimer();
        }

        private void StopTimer()
        {
            _timerToken.Cancel();
        }

        public void OnQuesterDeleted()
        {
            foreach (var obj in Objectives)
            {
                obj.OnQuesterDeleted();
            }

            Quest.OnQuesterDeleted(this);
        }

        public void OnPlayerDeath()
        {
            foreach (var obj in Objectives)
            {
                obj.OnPlayerDeath();
            }

            Quest.OnPlayerDeath(this);
        }

        private bool GetFlag(MLQuestInstanceFlags flag) => (m_Flags & flag) != 0;

        private void SetFlag(MLQuestInstanceFlags flag, bool value)
        {
            if (value)
            {
                m_Flags |= flag;
            }
            else
            {
                m_Flags &= ~flag;
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            // Version info is written in MLQuestPersistence.Serialize

            MLQuestSystem.WriteQuestRef(writer, Quest);

            writer.Write(m_Quester?.Deleted != false ? Serial.MinusOne : m_Quester.Serial);

            writer.Write(ClaimReward);
            writer.Write(Objectives.Length);

            foreach (var objInstance in Objectives)
            {
                objInstance.Serialize(writer);
            }
        }

        public static MLQuestInstance Deserialize(IGenericReader reader, int version, PlayerMobile pm)
        {
            var quest = MLQuestSystem.ReadQuestRef(reader);

            // TODO: Serialize quester TYPE too, the quest giver reference then becomes optional (only for escorts)
            var quester = reader.ReadEntity<IEntity>() as IQuestGiver;

            var claimReward = reader.ReadBool();
            var objectives = reader.ReadInt();

            MLQuestInstance instance;

            if (quest != null && quester != null && pm != null)
            {
                instance = quest.CreateInstance(quester, pm);
                instance.ClaimReward = claimReward;
            }
            else
            {
                instance = null;
            }

            for (var i = 0; i < objectives; ++i)
            {
                BaseObjectiveInstance.Deserialize(
                    reader,
                    version,
                    i < instance?.Objectives.Length ? instance.Objectives[i] : null
                );
            }

            instance?.Slice();

            return instance;
        }
    }
}
