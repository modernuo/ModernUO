using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Plants;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Quests.Matriarch;

[SerializationGenerator(0)]
public abstract partial class BaseSolenMatriarch : BaseQuester
{
    public BaseSolenMatriarch()
    {
        Body = 0x328;

        if (!RedSolen)
        {
            Hue = 0x44E;
        }

        SpeechHue = 0;
    }

    public abstract bool RedSolen { get; }
    public override string DefaultName => "the solen matriarch";
    public override bool DisallowAllMoves => false;

    public override int GetIdleSound() => 0x10D;

    public override bool CanTalkTo(PlayerMobile to) =>
        SolenMatriarchQuest.IsFriend(to, RedSolen) || to.Quest is SolenMatriarchQuest qs && qs.RedSolen == RedSolen;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        Direction = GetDirectionTo(player);

        if (player.Quest is not SolenMatriarchQuest qs || qs.RedSolen != RedSolen)
        {
            if (SolenMatriarchQuest.IsFriend(player, RedSolen))
            {
                var newQuest = new SolenMatriarchQuest(player, RedSolen);

                if (player.Quest == null && QuestSystem.CanOfferQuest(player, typeof(SolenMatriarchQuest)))
                {
                    newQuest.SendOffer();
                }
                else
                {
                    newQuest.AddConversation(new DontOfferConversation(true));
                }
            }

            return;
        }

        if (qs.IsObjectiveInProgress(typeof(KillInfiltratorsObjective)))
        {
            qs.AddConversation(new DuringKillInfiltratorsConversation());
            return;
        }

        if (qs.FindObjective<ReturnAfterKillsObjective>() is { Completed: false } obj1)
        {
            obj1.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(GatherWaterObjective)))
        {
            qs.AddConversation(new DuringWaterGatheringConversation());
            return;
        }

        if (qs.FindObjective<ReturnAfterWaterObjective>() is { Completed: false } obj2)
        {
            obj2.Complete();
            return;
        }

        if (qs.IsObjectiveInProgress(typeof(ProcessFungiObjective)))
        {
            qs.AddConversation(new DuringFungiProcessConversation());
            return;
        }

        if (qs.FindObjective<GetRewardObjective>() is { Completed: false } obj3)
        {
            if (SolenMatriarchQuest.GiveRewardTo(player))
            {
                obj3.Complete();
            }
            else
            {
                qs.AddConversation(new FullBackpackConversation(false));
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from is not PlayerMobile player)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (dropped is not Seed)
        {
            if (dropped is ZoogiFungus fungus)
            {
                OnGivenFungi(player, fungus);

                return fungus.Deleted;
            }

            return base.OnDragDrop(from, dropped);
        }

        if (player.Quest is SolenMatriarchQuest qs && qs.RedSolen == RedSolen)
        {
            SayTo(player, 1054080); // Thank you for that plant seed. Those have such wonderful flavor.
        }
        else
        {
            var newQuest = new SolenMatriarchQuest(player, RedSolen);

            if (player.Quest == null && QuestSystem.CanOfferQuest(player, typeof(SolenMatriarchQuest)))
            {
                newQuest.SendOffer();
            }
            else
            {
                newQuest.AddConversation(
                    new DontOfferConversation(SolenMatriarchQuest.IsFriend(player, RedSolen))
                );
            }
        }

        dropped.Delete();
        return true;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from.Alive && from is PlayerMobile pm && pm.Quest is SolenMatriarchQuest qs && qs.RedSolen == RedSolen &&
            qs.IsObjectiveInProgress(typeof(ProcessFungiObjective)))
        {
            list.Add(new ProcessZoogiFungusEntry());
        }
    }

    public void OnGivenFungi(PlayerMobile player, ZoogiFungus fungi)
    {
        Direction = GetDirectionTo(player);

        if (player.Quest is not SolenMatriarchQuest qs || qs.RedSolen != RedSolen)
        {
            return;
        }

        var obj = qs.FindObjective<ProcessFungiObjective>();

        if (obj?.Completed != false)
        {
            return;
        }

        var amount = fungi.Amount / 2;

        if (amount > 100)
        {
            amount = 100;
        }

        if (amount > 0)
        {
            if (amount * 2 >= fungi.Amount)
            {
                fungi.Delete();
            }
            else
            {
                fungi.Amount -= amount * 2;
            }

            var powder = new PowderOfTranslocation(amount);
            player.AddToBackpack(powder);

            player.SendLocalizedMessage(1054100); // You receive some powder of translocation.

            obj.Complete();
        }
    }

    private class ProcessZoogiFungusEntry : ContextMenuEntry
    {
        public ProcessZoogiFungusEntry() : base(6184)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.Alive && from is PlayerMobile pm && target is BaseSolenMatriarch matriarch)
            {
                from.Target = new ProcessFungiTarget(matriarch, pm);
            }
        }
    }

    private class ProcessFungiTarget : Target
    {
        private readonly PlayerMobile _from;
        private readonly BaseSolenMatriarch _matriarch;

        public ProcessFungiTarget(BaseSolenMatriarch matriarch, PlayerMobile from) : base(-1, false, TargetFlags.None)
        {
            _matriarch = matriarch;
            _from = from;
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.SendLocalizedMessage(1042021, "", 0x59); // Cancelled.
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is ZoogiFungus fungus)
            {
                if (fungus.IsChildOf(_from.Backpack))
                {
                    _matriarch.OnGivenFungi(_from, fungus);
                }
                else
                {
                    _from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class RedSolenMatriarch : BaseSolenMatriarch
{
    [Constructible]
    public RedSolenMatriarch()
    {
    }

    public override bool RedSolen => true;
}

[SerializationGenerator(0)]
public partial class BlackSolenMatriarch : BaseSolenMatriarch
{
    [Constructible]
    public BlackSolenMatriarch()
    {
    }

    public override bool RedSolen => false;
}
