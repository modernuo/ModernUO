using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class TinkerGuildmaster : BaseGuildmaster
    {
        [Constructible]
        public TinkerGuildmaster() : base("tinker")
        {
            SetSkill(SkillName.Lockpicking, 65.0, 88.0);
            SetSkill(SkillName.Tinkering, 90.0, 100.0);
            SetSkill(SkillName.RemoveTrap, 85.0, 100.0);
        }

        public override NpcGuild NpcGuild => NpcGuild.TinkersGuild;

        public override void AddCustomContextEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            if (Core.ML && from.Alive)
            {
                var entry = new RechargeEntry();

                if (WeaponEngravingTool.Find(from) == null)
                {
                    entry.Enabled = false;
                }

                list.Add(entry);
            }

            base.AddCustomContextEntries(from, ref list);
        }

        private class RechargeEntry : ContextMenuEntry
        {
            public RechargeEntry() : base(6271, 6)
            {
            }

            public override void OnClick(Mobile from, IEntity target)
            {
                if (!Core.ML || target is not Mobile vendor || vendor.Deleted)
                {
                    return;
                }

                var tool = WeaponEngravingTool.Find(from);

                if (!(tool?.UsesRemaining <= 0))
                {
                    // I can only help with this if you are carrying an engraving tool that needs repair.
                    vendor.Say(1076164);
                    return;
                }

                if (Banker.GetBalance(from) >= 100000)
                {
                    from.SendGump(new WeaponEngravingTool.ConfirmGump(tool, vendor));
                }
                else
                {
                    vendor.Say(1076167); // You need a 100,000 gold and a blue diamond to recharge the weapon engraver.
                }
            }
        }
    }
}
