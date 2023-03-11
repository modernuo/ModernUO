using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0)]
public partial class PlagueBeastOrgan : PlagueBeastInnard
{
    private bool _opening;

    [SerializableField(0, setter: "private")]
    private List<PlagueBeastComponent> _components;

    [SerializableField(1)]
    private int _brainHue;

    [SerializableField(2)]
    private bool _opened;

    public PlagueBeastOrgan(int itemID = 1, int hue = 0) : base(itemID, hue)
    {
        Components = new List<PlagueBeastComponent>();
        Opened = false;
        Movable = false;
        Visible = itemID <= 1;

        Timer.StartTimer(Initialize);
    }

    public virtual bool IsCuttable => false;

    public virtual void Initialize()
    {
    }

    public void AddComponent(PlagueBeastComponent c, int x, int y)
    {
        if (Parent is Container pack)
        {
            pack.DropItem(c);
        }

        c.Organ = this;
        c.Location = new Point3D(X + x, Y + y, Z);
        c.Map = Map;

        this.Add(Components, c);
    }

    public override bool Scissor(Mobile from, Scissors scissors)
    {
        if (IsCuttable && IsAccessibleTo(from))
        {
            if (!Opened && !_opening)
            {
                _opening = true;
                void Open()
                {
                    _opening = false;
                    if (!Deleted)
                    {
                        FinishOpening(from);
                    }
                }

                Timer.StartTimer(TimeSpan.FromSeconds(3), Open);

                scissors.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071897); // You carefully cut into the organ.
                return true;
            }

            scissors.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071898); // You have already cut this organ open.
        }

        return false;
    }

    public virtual bool OnLifted(Mobile from, PlagueBeastComponent c) => c.IsGland || c.IsBrain;

    public virtual bool OnDropped(Mobile from, Item item, PlagueBeastComponent to) => false;

    public virtual void FinishOpening(Mobile from)
    {
        Opened = true;
        Owner?.PlaySound(0x50);
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastMaidenOrgan : PlagueBeastOrgan
{
    public PlagueBeastMaidenOrgan() : base(0x124D)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Opened)
        {
            FinishOpening(from);
        }
    }

    public override void FinishOpening(Mobile from)
    {
        ItemID = 0x1249;

        Owner?.PlaySound(0x187);

        AddComponent(new PlagueBeastComponent(0x1D0D, 0x0), 22, 3);
        AddComponent(new PlagueBeastComponent(0x1D12, 0x0), 15, 18);
        AddComponent(new PlagueBeastComponent(0x1DA3, 0x21), 26, 46);

        if (BrainHue > 0)
        {
            AddComponent(new PlagueBeastComponent(0x1CF0, BrainHue, true), 22, 29);
        }

        Opened = true;
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastRockOrgan : PlagueBeastOrgan
{
    public PlagueBeastRockOrgan() : base(0x177A, 0x60)
    {
    }

    public override bool IsCuttable => true;

    public override void Carve(Mobile from, Item with)
    {
        if (IsAccessibleTo(from))
        {
            // This is too crude an implement for such a procedure.
            with.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071896);
        }
    }

    public override bool OnLifted(Mobile from, PlagueBeastComponent c)
    {
        base.OnLifted(from, c);

        if (c.IsBrain)
        {
            AddComponent(new PlagueBeastBlood(), -7, 24);
            return true;
        }

        return false;
    }

    public override void FinishOpening(Mobile from)
    {
        base.FinishOpening(from);

        AddComponent(new PlagueBeastComponent(0x1775, 0x60), 3, 5);
        AddComponent(new PlagueBeastComponent(0x1777, 0x1), 10, 14);

        if (BrainHue > 0)
        {
            AddComponent(new PlagueBeastComponent(0x1CF0, BrainHue, true), 1, 24); // 22, 29
        }
        else
        {
            AddComponent(new PlagueBeastBlood(), -7, 24);
        }
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastRubbleOrgan : PlagueBeastOrgan
{
    private static readonly int[] _hues = { 0xD, 0x17, 0x2B, 0x42, 0x54, 0x5D };

    [SerializableField(0, getter: "private", setter: "private")]
    private int _veins;

    public PlagueBeastRubbleOrgan() => _veins = 3;

    public override void Initialize()
    {
        Hue = _hues.RandomElement();

        AddComponent(new PlagueBeastComponent(0x3BB, Hue), 0, 0);
        AddComponent(new PlagueBeastComponent(0x3BA, Hue), 4, 6);
        AddComponent(new PlagueBeastComponent(0x3BA, Hue), -6, 17);

        var v = Utility.Random(4);

        AddComponent(new PlagueBeastVein(0x1B1B, v == 0 ? Hue : RandomHue(Hue)), -23, -3);
        AddComponent(new PlagueBeastVein(0x1B1C, v == 1 ? Hue : RandomHue(Hue)), 19, 4);
        AddComponent(new PlagueBeastVein(0x1B1B, v == 2 ? Hue : RandomHue(Hue)), 21, 27);
        AddComponent(new PlagueBeastVein(0x1B1B, v == 3 ? Hue : RandomHue(Hue)), 10, 40);
    }

    public override bool OnLifted(Mobile from, PlagueBeastComponent c)
    {
        if (c.IsBrain)
        {
            AddComponent(new PlagueBeastBlood(), -13, 25);
            return true;
        }

        return false;
    }

    public override void FinishOpening(Mobile from)
    {
        AddComponent(new PlagueBeastComponent(0x1777, 0x1), 5, 14);

        if (BrainHue > 0)
        {
            AddComponent(new PlagueBeastComponent(0x1CF0, BrainHue, true), -5, 22);
        }
        else
        {
            AddComponent(new PlagueBeastBlood(), -13, 25);
        }

        Opened = true;
    }

    private static int RandomHue(int exclude)
    {
        for (var i = 0; i < 20; i++)
        {
            var hue = _hues.RandomElement();

            if (hue != exclude)
            {
                return hue;
            }
        }

        return _hues[0];
    }

    public virtual void OnVeinCut(Mobile from, PlagueBeastVein vein)
    {
        if (vein.Hue != Hue)
        {
            if (!Opened && _veins > 0 && --_veins == 0)
            {
                FinishOpening(from);
            }
        }
        else
        {
            // * As you cut the vein, a cloud of poison is expelled from the plague beast's organ, and the plague beast dissolves into a puddle of goo *
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1071901);
            from.ApplyPoison(from, Poison.Greater);
            from.PlaySound(0x22F);

            if (Owner != null)
            {
                Owner.Unfreeze();
                Owner.Kill();
            }
        }
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastBackupOrgan : PlagueBeastOrgan
{
    [SerializableField(0, getter: "private", setter: "private")]
    private Item _gland;

    public PlagueBeastBackupOrgan() : base(0x1362, 0x6)
    {
    }

    public override bool IsCuttable => true;

    public override void Initialize()
    {
        AddComponent(new PlagueBeastComponent(0x1B1B, 0x42), 16, 39);
        AddComponent(new PlagueBeastComponent(0x1B1B, 0x42), 39, 49);
        AddComponent(new PlagueBeastComponent(0x1B1B, 0x42), 39, 48);
        AddComponent(new PlagueBeastComponent(0x1B1B, 0x42), 44, 42);
        AddComponent(new PlagueBeastComponent(0x1CF2, 0x42), 20, 34);
        AddComponent(new PlagueBeastComponent(0x135F, 0x42), 47, 58);
        AddComponent(new PlagueBeastComponent(0x1360, 0x42), 70, 68);
    }

    public override void Carve(Mobile from, Item with)
    {
        if (IsAccessibleTo(from))
        {
            // This is too crude an implement for such a procedure.
            with.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1071896);
        }
    }

    public override bool OnLifted(Mobile from, PlagueBeastComponent c)
    {
        if (c.IsBrain)
        {
            AddComponent(new PlagueBeastBlood(), 47, 72);
            return true;
        }

        if (c.IsGland)
        {
            _gland = null;
            return true;
        }

        return c.IsGland;
    }

    public override bool OnDropped(Mobile from, Item item, PlagueBeastComponent to)
    {
        if (to.Hue == 0x1 && _gland == null && item is PlagueBeastGland)
        {
            Gland = item;
            Timer.StartTimer(TimeSpan.FromSeconds(3), FinishHealing);
            from.SendAsciiMessage(0x3B2, "* You place the healthy gland inside the organ sac *");
            item.Movable = false;

            Owner?.PlaySound(0x20);

            return true;
        }

        return false;
    }

    public override void FinishOpening(Mobile from)
    {
        base.FinishOpening(from);

        AddComponent(new PlagueBeastComponent(0x1363, 0xF), -3, 3);
        AddComponent(new PlagueBeastComponent(0x1365, 0x1), -3, 10);

        Gland = new PlagueBeastComponent(0x1CEF, 0x3F, true);
        AddComponent((PlagueBeastComponent)_gland, -4, 16);
    }

    public void FinishHealing()
    {
        for (var i = 0; i < 7 && i < Components.Count; i++)
        {
            Components[i].Hue = 0x6;
        }

        Timer.StartTimer(TimeSpan.FromSeconds(2), OpenOrgan);
    }

    public void OpenOrgan()
    {
        AddComponent(new PlagueBeastComponent(0x1367, 0xF), 55, 61);
        AddComponent(new PlagueBeastComponent(0x1366, 0x1), 57, 66);

        if (BrainHue > 0)
        {
            AddComponent(new PlagueBeastComponent(0x1CF0, BrainHue, true), 55, 69);
        }
    }
}

[SerializationGenerator(0)]
public partial class PlagueBeastMainOrgan : PlagueBeastOrgan
{
    [SerializableField(0, getter: "private", setter: "private")]
    private int _brains;

    public PlagueBeastMainOrgan() => _brains = 0;

    public bool Complete => _brains >= 4;

    public override void Initialize()
    {
        // receptacles
        AddComponent(new PlagueBeastComponent(0x1B1B, 0x42), -36, -2);
        AddComponent(new PlagueBeastComponent(0x1FB3, 0x42), -42, 0);
        AddComponent(new PlagueBeastComponent(0x9DF, 0x42), -53, -7);

        AddComponent(new PlagueBeastComponent(0x1B1C, 0x54), 29, 9);
        AddComponent(new PlagueBeastComponent(0x1D06, 0x54), 18, -2);
        AddComponent(new PlagueBeastComponent(0x9DF, 0x54), 36, -1);

        AddComponent(new PlagueBeastComponent(0x1D10, 0x2B), -36, 47);
        AddComponent(new PlagueBeastComponent(0x1B1C, 0x2B), -24, 62);
        AddComponent(new PlagueBeastComponent(0x9DF, 0x2B), -41, 74);

        AddComponent(new PlagueBeastComponent(0x1B1B, 0x60), 39, 56);
        AddComponent(new PlagueBeastComponent(0x1FB4, 0x60), 34, 52);
        AddComponent(new PlagueBeastComponent(0x9DF, 0x60), 45, 71);

        // main part
        AddComponent(new PlagueBeastComponent(0x1351, 0x15), 23, 0);
        AddComponent(new PlagueBeastComponent(0x134F, 0x15), -22, 0);
        AddComponent(new PlagueBeastComponent(0x1350, 0x15), 0, 0);
    }

    public override bool OnLifted(Mobile from, PlagueBeastComponent c)
    {
        if (c.IsBrain)
        {
            Brains--;
        }

        return true;
    }

    public override bool OnDropped(Mobile from, Item item, PlagueBeastComponent to)
    {
        if (!Opened && to.IsReceptacle && item.Hue == to.Hue)
        {
            to.Organ = this;
            Brains++;

            // You place the organ in the fleshy receptacle near the core.
            from.LocalOverheadMessage(MessageType.Regular, 0x34, 1071913);

            if (Owner != null)
            {
                Owner.PlaySound(0x1BA);

                if (Owner.IsBleeding)
                {
                    // The plague beast is still bleeding from open wounds.  You must seal any bleeding wounds before the core will open!
                    from.LocalOverheadMessage(MessageType.Regular, 0x34, 1071922);
                    return true;
                }
            }

            if (_brains == 4)
            {
                FinishOpening(from);
            }

            return true;
        }

        return false;
    }

    public override void FinishOpening(Mobile from)
    {
        AddComponent(new PlagueBeastComponent(0x1363, 0x1), 0, 22);
        AddComponent(new PlagueBeastComponent(0x1D04, 0xD), 0, 22);

        if (Owner?.Backpack != null)
        {
            var core = new PlagueBeastMutationCore();
            Owner.Backpack.AddItem(core);
            core.Movable = false;
            core.Cut = false;
            core.X = X;
            core.Y = Y + 34;

            Owner.PlaySound(0x21);
            Owner.PlaySound(0x166);
        }

        Opened = true;
    }
}
