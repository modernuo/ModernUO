using System;
using ModernUO.Serialization;
using Server.Engines.PlayerMurderSystem;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class BountyGuard : BaseCreature
{
    [Constructible]
    public BountyGuard() : base(AIType.AI_Animal, FightMode.None)
    {
        Title = "the guard";
        InitStats(100, 125, 25);
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");

            AddItem(Utility.RandomBool() ? new LeatherSkirt() : new LeatherShorts());

            AddItem(
                Utility.Random(5) switch
                {
                    0 => new FemaleLeatherChest(),
                    1 => new FemaleStuddedChest(),
                    2 => new LeatherBustierArms(),
                    3 => new StuddedBustierArms(),
                    _ => new FemalePlateChest() // 4
                }
            );
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());

            AddItem(
                Utility.Random(3) switch
                {
                    0 => new Doublet(Utility.RandomNondyedHue()),
                    1 => new Tunic(Utility.RandomNondyedHue()),
                    _ => new BodySash(Utility.RandomNondyedHue()) // 3
                }
            );
        }

        Utility.AssignRandomHair(this);

        if (Utility.RandomBool())
        {
            Utility.AssignRandomFacialHair(this, HairHue);
        }

        var weapon = new Halberd
        {
            Movable = false,
            Crafter = Name,
            Quality = WeaponQuality.Exceptional
        };

        AddItem(weapon);

        var pack = new Backpack
        {
            Movable = false
        };

        pack.DropItem(new Gold(10, 25));
        AddItem(pack);

        Skills.Anatomy.Base = 120.0;
        Skills.Tactics.Base = 120.0;
        Skills.Swords.Base = 120.0;
        Skills.MagicResist.Base = 120.0;
        Skills.DetectHidden.Base = 100.0;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (PlayerMurderSystem.BountiesEnabled && dropped is Head head && head.PlayerName != null)
        {
            var target = head.BountyTarget;

            if (target == null || Core.Now - head.CarvedTime > TimeSpan.FromHours(24))
            {
                SayNonBountyHeadResponse();
                head.Delete();
                return true;
            }

            Say(500670); // Ah, a head!  Let me check to see if there is a bounty on this.
            head.Delete();
            ClaimBounty(from, target);
            return true;
        }

        return base.OnDragDrop(from, dropped);
    }

    private void ClaimBounty(Mobile from, PlayerMobile target)
    {
        var bounty = PlayerMurderSystem.GetBounty(target);

        if (bounty > 0)
        {
            PlayerMurderSystem.ClearBounty(target);
            Banker.Deposit(from, bounty);
            Titles.AwardKarma(from, 2000, true);

            Say(1042855, $"{target.Name}\t{bounty}"); // The bounty on ~1_PLAYER_NAME~ was ~2_AMOUNT~ gold, and has been credited to your account.
        }
        else
        {
            Say(1042854, target.Name); // There was no bounty on ~1_PLAYER_NAME~.
        }
    }

    private void SayNonBountyHeadResponse()
    {
        if (Utility.Random(5) == 0)
        {
            Say(500661 + Utility.Random(9)); // 500661–500669: silly guard responses
        }
        else
        {
            Say(500654 + Utility.Random(7)); // 500654–500660: normal guard responses
        }
    }
}
