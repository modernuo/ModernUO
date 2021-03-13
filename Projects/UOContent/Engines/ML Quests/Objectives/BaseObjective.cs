using System;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Objectives
{
    public abstract class BaseObjective
    {
        public virtual bool IsTimed => false;
        public virtual TimeSpan Duration => TimeSpan.Zero;

        public virtual bool CanOffer(IQuestGiver quester, PlayerMobile pm, bool message) => true;

        public abstract void WriteToGump(Gump g, ref int y);

        public virtual BaseObjectiveInstance CreateInstance(MLQuestInstance instance) => null;
    }

    public abstract class BaseObjectiveInstance
    {
        public enum DataType : byte
        {
            None,
            EscortObjective,
            KillObjective,
            DeliverObjective
        }

        public BaseObjectiveInstance(MLQuestInstance instance, BaseObjective obj)
        {
            Instance = instance;

            if (obj.IsTimed)
            {
                EndTime = Core.Now + obj.Duration;
            }
        }

        public MLQuestInstance Instance { get; }

        public bool IsTimed => EndTime != DateTime.MinValue;

        public DateTime EndTime { get; set; }

        public bool Expired { get; set; }

        public virtual DataType ExtraDataType => DataType.None;

        public virtual void WriteToGump(Gump g, ref int y)
        {
            if (IsTimed)
            {
                WriteTimeRemaining(g, ref y, Utility.Max(EndTime - Core.Now, TimeSpan.Zero));
            }
        }

        public static void WriteTimeRemaining(Gump g, ref int y, TimeSpan timeRemaining)
        {
            g.AddHtmlLocalized(103, y, 120, 16, 1062379, 0x15F90); // Est. time remaining:
            g.AddLabel(223, y, 0x481, timeRemaining.TotalSeconds.ToString("F0"));
            y += 16;
        }

        public virtual bool AllowsQuestItem(Item item, Type type) => false;

        public virtual bool IsCompleted() => false;

        public virtual void CheckComplete()
        {
            if (IsCompleted())
            {
                Instance.Player.PlaySound(0x5B6); // public sound
                Instance.CheckComplete();
            }
        }

        public virtual void OnQuestAccepted()
        {
        }

        public virtual void OnQuestCancelled()
        {
        }

        public virtual void OnQuestCompleted()
        {
        }

        public virtual bool OnBeforeClaimReward() => true;

        public virtual void OnClaimReward()
        {
        }

        public virtual void OnAfterClaimReward()
        {
        }

        public virtual void OnRewardClaimed()
        {
        }

        public virtual void OnQuesterDeleted()
        {
        }

        public virtual void OnPlayerDeath()
        {
        }

        public virtual void OnExpire()
        {
        }

        public virtual void Serialize(IGenericWriter writer)
        {
            // Version info is written in MLQuestPersistence.Serialize

            if (IsTimed)
            {
                writer.Write(true);
                writer.WriteDeltaTime(EndTime);
            }
            else
            {
                writer.Write(false);
            }

            // For type checks on deserialization
            // (This way quest objectives can be changed without breaking serialization)
            writer.Write((byte)ExtraDataType);
        }

        public static void Deserialize(IGenericReader reader, int version, BaseObjectiveInstance objInstance)
        {
            if (reader.ReadBool())
            {
                var endTime = reader.ReadDeltaTime();

                if (objInstance != null)
                {
                    objInstance.EndTime = endTime;
                }
            }

            var extraDataType = (DataType)reader.ReadByte();

            switch (extraDataType)
            {
                case DataType.EscortObjective:
                    {
                        var completed = reader.ReadBool();

                        if (objInstance is EscortObjectiveInstance instance)
                        {
                            instance.HasCompleted = completed;
                        }

                        break;
                    }
                case DataType.KillObjective:
                    {
                        var slain = reader.ReadInt();

                        if (objInstance is KillObjectiveInstance instance)
                        {
                            instance.Slain = slain;
                        }

                        break;
                    }
                case DataType.DeliverObjective:
                    {
                        var completed = reader.ReadBool();

                        if (objInstance is DeliverObjectiveInstance instance)
                        {
                            instance.HasCompleted = completed;
                        }

                        break;
                    }
            }
        }
    }
}
