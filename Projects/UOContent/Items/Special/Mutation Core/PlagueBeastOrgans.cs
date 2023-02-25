using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class PlagueBeastOrgan : PlagueBeastInnard
    {
        private bool _opening;

        public PlagueBeastOrgan(int itemID = 1, int hue = 0) : base(itemID, hue)
        {
            Components = new List<PlagueBeastComponent>();
            Opened = false;
            Movable = false;
            Visible = itemID <= 1;

            Timer.StartTimer(Initialize);
        }

        public PlagueBeastOrgan(Serial serial) : base(serial)
        {
        }

        public virtual bool IsCuttable => false;

        public List<PlagueBeastComponent> Components { get; private set; }

        public int BrainHue { get; set; }

        public bool Opened { get; set; }

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

            Components.Add(c);
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Components);
            writer.Write(BrainHue);
            writer.Write(Opened);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            Components = reader.ReadEntityList<PlagueBeastComponent>();
            BrainHue = reader.ReadInt();
            Opened = reader.ReadBool();
        }
    }

    public class PlagueBeastMaidenOrgan : PlagueBeastOrgan
    {
        public PlagueBeastMaidenOrgan() : base(0x124D)
        {
        }

        public PlagueBeastMaidenOrgan(Serial serial) : base(serial)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class PlagueBeastRockOrgan : PlagueBeastOrgan
    {
        public PlagueBeastRockOrgan() : base(0x177A, 0x60)
        {
        }

        public PlagueBeastRockOrgan(Serial serial) : base(serial)
        {
        }

        public override bool IsCuttable => true;

        public override void Carve(Mobile from, Item with)
        {
            if (IsAccessibleTo(from))
            {
                with.PublicOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1071896 // This is too crude an implement for such a procedure.
                );
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();
        }
    }

    public class PlagueBeastRubbleOrgan : PlagueBeastOrgan
    {
        private static readonly int[] m_Hues =
        {
            0xD, 0x17, 0x2B, 0x42, 0x54, 0x5D
        };

        private int m_Veins;

        public PlagueBeastRubbleOrgan() => m_Veins = 3;

        public PlagueBeastRubbleOrgan(Serial serial) : base(serial)
        {
        }

        public override void Initialize()
        {
            Hue = m_Hues.RandomElement();

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
                var hue = m_Hues.RandomElement();

                if (hue != exclude)
                {
                    return hue;
                }
            }

            return 0xD;
        }

        public virtual void OnVeinCut(Mobile from, PlagueBeastVein vein)
        {
            if (vein.Hue != Hue)
            {
                if (!Opened && m_Veins > 0 && --m_Veins == 0)
                {
                    FinishOpening(from);
                }
            }
            else
            {
                from.LocalOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1071901 // * As you cut the vein, a cloud of poison is expelled from the plague beast's organ, and the plague beast dissolves into a puddle of goo *
                );
                from.ApplyPoison(from, Poison.Greater);
                from.PlaySound(0x22F);

                if (Owner != null)
                {
                    Owner.Unfreeze();
                    Owner.Kill();
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Veins);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Veins = reader.ReadInt();
        }
    }

    public class PlagueBeastBackupOrgan : PlagueBeastOrgan
    {
        private Item m_Gland;

        public PlagueBeastBackupOrgan() : base(0x1362, 0x6)
        {
        }

        public PlagueBeastBackupOrgan(Serial serial) : base(serial)
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
                with.PublicOverheadMessage(
                    MessageType.Regular,
                    0x3B2,
                    1071896 // This is too crude an implement for such a procedure.
                );
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
                m_Gland = null;
                return true;
            }

            return c.IsGland;
        }

        public override bool OnDropped(Mobile from, Item item, PlagueBeastComponent to)
        {
            if (to.Hue == 0x1 && m_Gland == null && item is PlagueBeastGland)
            {
                m_Gland = item;
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

            m_Gland = new PlagueBeastComponent(0x1CEF, 0x3F, true);
            AddComponent((PlagueBeastComponent)m_Gland, -4, 16);
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Gland);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Gland = reader.ReadEntity<Item>();
        }
    }

    public class PlagueBeastMainOrgan : PlagueBeastOrgan
    {
        private int m_Brains;

        public PlagueBeastMainOrgan() => m_Brains = 0;

        public PlagueBeastMainOrgan(Serial serial) : base(serial)
        {
        }

        public bool Complete => m_Brains >= 4;

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
                m_Brains--;
            }

            return true;
        }

        public override bool OnDropped(Mobile from, Item item, PlagueBeastComponent to)
        {
            if (!Opened && to.IsReceptacle && item.Hue == to.Hue)
            {
                to.Organ = this;
                m_Brains++;
                from.LocalOverheadMessage(
                    MessageType.Regular,
                    0x34,
                    1071913 // You place the organ in the fleshy receptacle near the core.
                );

                if (Owner != null)
                {
                    Owner.PlaySound(0x1BA);

                    if (Owner.IsBleeding)
                    {
                        from.LocalOverheadMessage(
                            MessageType.Regular,
                            0x34,
                            1071922 // The plague beast is still bleeding from open wounds.  You must seal any bleeding wounds before the core will open!
                        );
                        return true;
                    }
                }

                if (m_Brains == 4)
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

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Brains);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Brains = reader.ReadInt();
        }
    }
}
