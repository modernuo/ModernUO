using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class ArcherGuard : BaseGuard
{
    private bool _shooting;

    [Constructible]
    public ArcherGuard(Mobile target = null) : base(target)
    {
        InitStats(100, 125, 25);
        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        new Horse().Rider = this;

        AddItem(new StuddedChest());
        AddItem(new StuddedArms());
        AddItem(new StuddedGloves());
        AddItem(new StuddedGorget());
        AddItem(new StuddedLegs());
        AddItem(new Boots());
        AddItem(new SkullCap());

        var bow = new Bow();

        bow.Movable = false;
        bow.Crafter = Name;
        bow.Quality = WeaponQuality.Exceptional;

        AddItem(bow);

        Container pack = new Backpack();

        pack.Movable = false;

        var arrows = new Arrow(250);

        arrows.LootType = LootType.Newbied;

        pack.DropItem(arrows);
        pack.DropItem(new Gold(10, 25));

        AddItem(pack);

        Skills.Anatomy.Base = 120.0;
        Skills.Tactics.Base = 120.0;
        Skills.Archery.Base = 120.0;
        Skills.MagicResist.Base = 120.0;
        Skills.DetectHidden.Base = 100.0;

        NextCombatTime = Core.TickCount + 500;
        Focus = target;
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
                    AttackTimer.DoOnTick();
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
            _shooting = false;
            Focus = null;
            return;
        }

        if (!InLOS(target))
        {
            _shooting = false;
            TeleportTo(this, target.Location);
            return;
        }

        if (!CanSee(target))
        {
            _shooting = false;

            if (InRange(target, 2))
            {
                if (UseSkill(SkillName.DetectHidden))
                {
                    Say("Reveal!");
                }
            }
            else if (!Move(GetDirectionTo(target) | Direction.Running) && OutOfMaxDistance(target))
            {
                TeleportTo(this, target.Location);
            }

            return;
        }

        if (_shooting)
        {
            if (TimeToSpare() || OutOfMaxDistance(target))
            {
                _shooting = false;
            }

            return;
        }

        if (InRange(target, 1) && !Move(GetDirectionTo(target) - 4 | Direction.Running) && OutOfMaxDistance(target))
        {
            TeleportTo(this, target.Location);
            return;
        }

        if (InMinDistance(target))
        {
            _shooting = true;
        }
    }

    private bool TimeToSpare() => NextCombatTime - Core.TickCount > 1000;

    private bool OutOfMaxDistance(IPoint2D target) => !InRange(target, Weapon.MaxRange);

    private bool InMinDistance(IPoint2D target) => InRange(target, 4);
}
