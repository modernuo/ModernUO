using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class HordeMinionFamiliar : BaseFamiliar
{
    private DateTime m_NextPickup;

    public HordeMinionFamiliar()
    {
        Body = 776;
        BaseSoundID = 0x39D;

        SetStr(100);
        SetDex(110);
        SetInt(100);

        SetHits(70);
        SetStam(110);
        SetMana(0);

        SetDamage(5, 10);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 50, 55);
        SetResistance(ResistanceType.Poison, 25, 30);
        SetResistance(ResistanceType.Energy, 25, 30);

        SetSkill(SkillName.Wrestling, 70.1, 75.0);
        SetSkill(SkillName.Tactics, 50.0);

        ControlSlots = 1;

        var pack = Backpack;

        pack?.Delete();

        pack = new Backpack();
        pack.Movable = false;
        pack.Weight = 13.0;

        AddItem(pack);
    }

    public override string CorpseName => "a horde minion corpse";
    public override bool DisplayWeight => true;

    public override string DefaultName => "a horde minion";

    public override void OnThink()
    {
        base.OnThink();

        if (Core.Now < m_NextPickup)
        {
            return;
        }

        m_NextPickup = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10));

        var pack = Backpack;

        if (pack == null)
        {
            return;
        }

        var eable = GetItemsInRange(2);
        using var queue = PooledRefQueue<Item>.Create();
        foreach (var item in eable)
        {
            if (item.Movable && item.Stackable)
            {
                queue.Enqueue(item);
            }
        }

        var pickedUp = 3;

        while (pickedUp > 0 && queue.Count > 0)
        {
            var item = queue.Dequeue();

            if (!pack.CheckHold(this, item, false, true))
            {
                return;
            }

            NextActionTime = Core.TickCount;

            Lift(item, item.Amount, out var rejected, out var _);

            if (rejected)
            {
                continue;
            }

            Drop(this, Point3D.Zero);
            pickedUp--;
        }
    }

    private void ConfirmRelease_Callback(Mobile from, bool okay)
    {
        if (okay)
        {
            EndRelease(from);
        }
    }

    public override void BeginRelease(Mobile from)
    {
        if (Backpack?.Items.Count > 0)
        {
            from.SendGump(
                new WarningGump(1060635, 30720, 1061672, 32512, 420, 280, okay => ConfirmRelease_Callback(from, okay))
            );
        }
        else
        {
            EndRelease(from);
        }
    }

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

    public override bool IsSnoop(Mobile from)
    {
        if (PackAnimal.CheckAccess(this, from))
        {
            return false;
        }

        return base.IsSnoop(from);
    }

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

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        PackAnimal.GetContextMenuEntries(this, from, list);
    }
}
