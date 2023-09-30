using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.MLQuests.Items;

[SerializationGenerator(0, false)]
public abstract partial class QuestGiverItem : Item, IQuestGiver
{
    private List<MLQuest> m_MLQuests;

    public QuestGiverItem(int itemId) : base(itemId)
    {
    }

    public bool CanGiveMLQuest => MLQuests.Count != 0;

    public override bool Nontransferable => true;

    public List<MLQuest> MLQuests => m_MLQuests ??
                                     (m_MLQuests = MLQuestSystem.FindQuestList(GetType()) ?? MLQuestSystem.EmptyList);

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        AddQuestItemProperty(list);

        if (CanGiveMLQuest)
        {
            list.Add(1072269); // Quest Giver
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042593); // That is not in your backpack.
        }
        else if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
        {
            MLQuestSystem.OnDoubleClick(this, mobile);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (MLQuestSystem.Enabled)
        {
            MLQuestSystem.HandleDeletion(this);
        }
    }
}

[SerializationGenerator(0, false)]
public abstract partial class TransientQuestGiverItem : TransientItem, IQuestGiver
{
    private List<MLQuest> m_MLQuests;

    public TransientQuestGiverItem(int itemId, TimeSpan lifeSpan)
        : base(itemId, lifeSpan)
    {
    }

    public bool CanGiveMLQuest => MLQuests.Count != 0;

    public override bool Nontransferable => true;

    public List<MLQuest> MLQuests => m_MLQuests ??
                                     (m_MLQuests = MLQuestSystem.FindQuestList(GetType()) ?? MLQuestSystem.EmptyList);

    public override void HandleInvalidTransfer(Mobile from)
    {
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        AddQuestItemProperty(list);

        if (CanGiveMLQuest)
        {
            list.Add(1072269); // Quest Giver
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042593); // That is not in your backpack.
        }
        else if (MLQuestSystem.Enabled && CanGiveMLQuest && from is PlayerMobile mobile)
        {
            MLQuestSystem.OnDoubleClick(this, mobile);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (MLQuestSystem.Enabled)
        {
            MLQuestSystem.HandleDeletion(this);
        }
    }
}
