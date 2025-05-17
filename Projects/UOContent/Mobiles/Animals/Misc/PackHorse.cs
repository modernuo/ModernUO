using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class PackHorse : BaseCreature
    {
        [Constructible]
        public PackHorse() : base(AIType.AI_Animal, FightMode.Aggressor)
        {
            Body = 291;
            BaseSoundID = 0xA8;

            SetStr(44, 120);
            SetDex(36, 55);
            SetInt(6, 10);

            SetHits(61, 80);
            SetStam(81, 100);
            SetMana(0);

            SetDamage(5, 11);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 20, 25);
            SetResistance(ResistanceType.Fire, 10, 15);
            SetResistance(ResistanceType.Cold, 20, 25);
            SetResistance(ResistanceType.Poison, 10, 15);
            SetResistance(ResistanceType.Energy, 10, 15);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 0;
            Karma = 200;

            VirtualArmor = 16;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;

            var pack = Backpack;

            pack?.Delete();

            pack = new StrongBackpack();
            pack.Movable = false;

            AddItem(pack);
        }

        public override string CorpseName => "a horse corpse";
        public override string DefaultName => "a pack horse";

        public override int Meat => 3;
        public override int Hides => 10;
        public override FoodType FavoriteFood => FoodType.FruitsAndVeggies | FoodType.GrainsAndHay;

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
            {
                return false;
            }

            PackAnimal.CombineBackpacks(this);

            return true;
        }

        public override DeathMoveResult GetInventoryMoveResultFor(Item item) => DeathMoveResult.MoveToCorpse;

        public override bool IsSnoop(Mobile from) => !PackAnimal.CheckAccess(this, from) && base.IsSnoop(from);

        public override bool OnDragDrop(Mobile from, Item item)
        {
            if (CheckFeed(from, item))
            {
                return true;
            }

            if (PackAnimal.CheckAccess(this, from))
            {
                AddToBackpack(item);
                return true;
            }

            return base.OnDragDrop(from, item);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target) => PackAnimal.CheckAccess(this, from);

        public override bool CheckNonlocalLift(Mobile from, Item item) => PackAnimal.CheckAccess(this, from);

        public override void OnDoubleClick(Mobile from)
        {
            PackAnimal.TryPackOpen(this, from);
        }

        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            PackAnimal.GetContextMenuEntries(this, from, ref list);
        }
    }

    public class PackAnimalBackpackEntry : ContextMenuEntry
    {
        public PackAnimalBackpackEntry(bool enabled) : base(6145, 3) => Enabled = enabled;

        public override void OnClick(Mobile from, IEntity target)
        {
            if (target is BaseCreature bc)
            {
                PackAnimal.TryPackOpen(bc, from);
            }
        }
    }

    public static class PackAnimal
    {
        public static void GetContextMenuEntries(BaseCreature animal, Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            if (CheckAccess(animal, from))
            {
                list.Add(new PackAnimalBackpackEntry(!animal.IsDeadPet));
            }
        }

        public static bool CheckAccess(BaseCreature animal, Mobile from) =>
            from == animal || from.AccessLevel >= AccessLevel.GameMaster || from.Alive && animal.Controlled &&
            !animal.IsDeadPet &&
            (from == animal.ControlMaster || from == animal.SummonMaster || animal.IsPetFriend(from));

        public static void CombineBackpacks(BaseCreature animal)
        {
            if (Core.AOS)
            {
                return;
            }

            if (animal.IsBonded || animal.IsDeadPet)
            {
                return;
            }

            var pack = animal.Backpack;

            if (pack != null)
            {
                var newPack = new Backpack();

                for (var i = pack.Items.Count - 1; i >= 0; --i)
                {
                    if (i >= pack.Items.Count)
                    {
                        continue;
                    }

                    newPack.DropItem(pack.Items[i]);
                }

                pack.DropItem(newPack);
            }
        }

        public static void TryPackOpen(BaseCreature animal, Mobile from)
        {
            if (animal.IsDeadPet)
            {
                return;
            }

            var item = animal.Backpack;

            if (item != null)
            {
                from.Use(item);
            }
        }
    }
}
