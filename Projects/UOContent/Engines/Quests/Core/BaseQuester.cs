using System.Collections.Generic;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests
{
    public class TalkEntry : ContextMenuEntry
    {
        private readonly BaseQuester m_Quester;

        public TalkEntry(BaseQuester quester) : base(quester.TalkNumber) => m_Quester = quester;

        public override void OnClick()
        {
            var from = Owner.From;

            if (from.CheckAlive() && from is PlayerMobile mobile && m_Quester.CanTalkTo(mobile))
            {
                m_Quester.OnTalk(mobile, true);
            }
        }
    }

    public abstract class BaseQuester : BaseVendor
    {
        protected List<SBInfo> m_SBInfos = new();

        public BaseQuester(string title = null) : base(title)
        {
        }

        public BaseQuester(Serial serial) : base(serial)
        {
        }

        protected override List<SBInfo> SBInfos => m_SBInfos;

        public override bool IsActiveVendor => false;
        public override bool IsInvulnerable => true;
        public override bool DisallowAllMoves => true;
        public override bool ClickTitle => false;
        public override bool CanTeach => false;

        public virtual int TalkNumber // Talk
            => 6146;

        public override void InitSBInfo()
        {
        }

        public abstract void OnTalk(PlayerMobile player, bool contextMenu);

        public virtual bool CanTalkTo(PlayerMobile to) => true;

        public virtual int GetAutoTalkRange(PlayerMobile m) => -1;

        public override bool CanBeDamaged() => false;

        protected Item SetHue(Item item, int hue)
        {
            item.Hue = hue;
            return item;
        }

        public override void AddCustomContextEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.AddCustomContextEntries(from, list);

            if (from.Alive && from is PlayerMobile mobile && TalkNumber > 0 && CanTalkTo(mobile))
            {
                list.Add(new TalkEntry(this));
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m.Alive && m is PlayerMobile pm)
            {
                var range = GetAutoTalkRange(pm);

                if (pm.Alive && range >= 0 && InRange(m, range) && !InRange(oldLocation, range) && CanTalkTo(pm))
                {
                    OnTalk(pm, false);
                }
            }
        }

        public void FocusTo(Mobile to)
        {
            QuestSystem.FocusTo(this, to);
        }

        public static Container GetNewContainer()
        {
            var bag = new Bag();
            bag.Hue = QuestSystem.RandomBrightHue();
            return bag;
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
