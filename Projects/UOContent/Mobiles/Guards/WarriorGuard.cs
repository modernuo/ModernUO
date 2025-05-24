using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class WarriorGuard : BaseGuard
{
    [Constructible]
    public WarriorGuard(Mobile target = null) : base(target)
    {
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

        NextCombatTime = Core.TickCount + 500;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public override Mobile Focus
    {
        get => _focus;
        set
        {
            if (Deleted)
            {
                return;
            }

            var oldFocus = _focus;

            if (oldFocus != value)
            {
                _focus = value;

                if (value != null)
                {
                    AggressiveAction(value);
                }

                Combatant = value;

                if (oldFocus?.Alive == false)
                {
                    Say("Thou hast suffered thy punishment, scoundrel.");
                }

                if (value != null)
                {
                    Say(500131); // Thou wilt regret thine actions, swine!
                }

                AttackTimer = null;
                IdleTimer = null;

                if (_focus != null)
                {
                    AttackTimer = new GuardAttackTimer(this);
                    AttackTimer.Start();
                }
                else
                {
                    IdleTimer = new GuardIdleTimer(this);
                }
            }
            else if (_focus == null && IdleTimer == null && Spawner != null)
            {
                IdleTimer = new GuardIdleTimer(this);
            }

            this.MarkDirty();
        }
    }

    public override bool OnBeforeDeath()
    {
        if (_focus?.Alive == true)
        {
            new GuardAvengeTimer(_focus).Start(); // If a guard dies, three more guards will spawn
        }

        return base.OnBeforeDeath();
    }

    public override void NonLethalAttack(Mobile target)
    {
        if (!InRange(target, 20))
        {
            Focus = null;
        }
        else if (!InRange(target, 10) || !InLOS(target))
        {
            TeleportTo(this, target.Location);
        }
        else if (!InRange(target, 1))
        {
            if (!Move(GetDirectionTo(target) | Direction.Running))
            {
                TeleportTo(this, target.Location);
            }
        }
        else if (!CanSee(target) && UseSkill(SkillName.DetectHidden))
        {
            Say("Reveal!");
        }
    }
}
