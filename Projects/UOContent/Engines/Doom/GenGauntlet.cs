using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Doom
{
    public static class GenGauntlet
    {
        public static void Initialize()
        {
            CommandSystem.Register("GenGauntlet", AccessLevel.Administrator, GenGauntlet_OnCommand);
            CommandSystem.Register("RemGauntlet", AccessLevel.Administrator, RemoveGauntlet);
        }

        public static void RemoveGauntlet(CommandEventArgs e)
        {
            RemoveMobile<PricedHealer>(387, 400);
            RemoveTeleporter(390, 407, 394, 405);
            RemoveDoorSet(393, 404);
            RemoveItem<MorphItem>(433, 371);
            RemoveItem<MorphItem>(433, 372);
            RemoveMobile<VarietyDealer>(492, 369);

            for (var x = 434; x <= 478; ++x)
            {
                for (var y = 371; y <= 372; ++y)
                {
                    RemoveItem<Static>(x, y);
                }
            }

            RemoveTeleporter(471, 428, 474, 428);
            RemoveTeleporter(462, 494, 462, 498);
            RemoveTeleporter(403, 502, 399, 506);
            RemoveTeleporter(357, 476, 356, 480);
            RemoveTeleporter(361, 433, 357, 434);

            RemoveItem<GauntletSpawner>(491, 456);
            RemoveItem<GauntletSpawner>(482, 520);
            RemoveItem<GauntletSpawner>(406, 538);
            RemoveItem<GauntletSpawner>(335, 512);
            RemoveItem<GauntletSpawner>(326, 433);
            RemoveItem<GauntletSpawner>(423, 430);

            RemoveItem<ConfirmationMoongate>(433, 326, 4);
        }

        public static void GenGauntlet_OnCommand(CommandEventArgs e)
        {
            RemoveGauntlet(e);

            /* Begin healer room */
            CreatePricedHealer(5000, 387, 400);
            CreateTeleporter(390, 407, 394, 405);

            var healerDoor = CreateDoorSet(393, 404, true, 0x44E);

            healerDoor.Locked = true;
            healerDoor.KeyValue = Key.RandomValue();

            if (healerDoor.Link?.Deleted == false)
            {
                healerDoor.Link.Locked = true;
                healerDoor.Link.KeyValue = Key.RandomValue();
            }
            /* End healer room */

            /* Begin supply room */
            CreateMorphItem(433, 371, 0x29F, 0x116, 3, 0x44E);
            CreateMorphItem(433, 372, 0x29F, 0x115, 3, 0x44E);

            CreateVarietyDealer(492, 369);

            for (var x = 434; x <= 478; ++x)
            {
                for (var y = 371; y <= 372; ++y)
                {
                    var item = new Static(0x524) { Hue = 1 };

                    item.MoveToWorld(new Point3D(x, y, -1), Map.Malas);
                }
            }
            /* End supply room */

            /* Begin gauntlet cycle */
            CreateTeleporter(471, 428, 474, 428);
            CreateTeleporter(462, 494, 462, 498);
            CreateTeleporter(403, 502, 399, 506);
            CreateTeleporter(357, 476, 356, 480);
            CreateTeleporter(361, 433, 357, 434);

            var sp1 = CreateSpawner("DarknightCreeper", 491, 456, 473, 432, 417, 426, true, 473, 412, 39, 60);
            var sp2 = CreateSpawner("FleshRenderer", 482, 520, 468, 496, 426, 422, false, 448, 496, 56, 48);
            var sp3 = CreateSpawner("Impaler", 406, 538, 408, 504, 432, 430, false, 376, 504, 64, 48);
            var sp4 = CreateSpawner("ShadowKnight", 335, 512, 360, 478, 424, 439, false, 300, 478, 72, 64);
            var sp5 = CreateSpawner("AbysmalHorror", 326, 433, 360, 429, 416, 435, true, 300, 408, 60, 56);
            var sp6 = CreateSpawner("DemonKnight", 423, 430, 0, 0, 423, 430, true, 392, 392, 72, 96);

            sp1.Sequence = sp2;
            sp2.Sequence = sp3;
            sp3.Sequence = sp4;
            sp4.Sequence = sp5;
            sp5.Sequence = sp6;
            sp6.Sequence = sp1;

            sp1.State = GauntletSpawnerState.InProgress;
            /* End gauntlet cycle */

            /* Begin exit gate */
            var gate = new ConfirmationMoongate();

            gate.Dispellable = false;

            gate.Target = new Point3D(2350, 1270, -85);
            gate.TargetMap = Map.Malas;

            gate.GumpWidth = 420;
            gate.GumpHeight = 280;

            gate.MessageColor = 0x7F00;
            gate.Message = 1062109; // You are about to exit Dungeon Doom.  Do you wish to continue?

            gate.TitleColor = 0x7800;
            gate.TitleNumber = 1062108; // Please verify...

            gate.Hue = 0x44E;

            gate.MoveToWorld(new Point3D(433, 326, 4), Map.Malas);
            /* End exit gate */
        }

        public static GauntletSpawner CreateSpawner(
            string typeName, int xSpawner, int ySpawner, int xDoor, int yDoor,
            int xPentagram, int yPentagram, bool doorEastToWest, int xStart, int yStart, int xWidth, int yHeight
        )
        {
            var spawner = new GauntletSpawner(typeName);

            spawner.MoveToWorld(new Point3D(xSpawner, ySpawner, -1), Map.Malas);

            if (xDoor > 0 && yDoor > 0)
            {
                spawner.Door = CreateDoorSet(xDoor, yDoor, doorEastToWest, 0);
            }

            spawner.RegionBounds = new Rectangle2D(xStart, yStart, xWidth, yHeight);

            if (xPentagram > 0 && yPentagram > 0)
            {
                var pentagram = new PentagramAddon();

                pentagram.MoveToWorld(new Point3D(xPentagram, yPentagram, -1), Map.Malas);

                spawner.Addon = pentagram;
            }

            return spawner;
        }

        public static BaseDoor CreateDoorSet(int xDoor, int yDoor, bool doorEastToWest, int hue)
        {
            BaseDoor hiDoor = new MetalDoor(doorEastToWest ? DoorFacing.NorthCCW : DoorFacing.WestCW);
            BaseDoor loDoor = new MetalDoor(doorEastToWest ? DoorFacing.SouthCW : DoorFacing.EastCCW);

            hiDoor.MoveToWorld(new Point3D(xDoor, yDoor, -1), Map.Malas);
            loDoor.MoveToWorld(
                new Point3D(xDoor + (doorEastToWest ? 0 : 1), yDoor + (doorEastToWest ? 1 : 0), -1),
                Map.Malas
            );

            hiDoor.Link = loDoor;
            loDoor.Link = hiDoor;

            hiDoor.Hue = hue;
            loDoor.Hue = hue;

            return hiDoor;
        }

        public static void RemoveDoorSet(int x, int y)
        {
            var loc = new Point3D(x, y, -1);
            foreach (var item in Map.Malas.GetItemsInRange(loc, 0))
            {
                if (item is BaseDoor door)
                {
                    door.Link?.Delete();
                    door.Delete();
                    break;
                }
            }
        }

        public static void CreateTeleporter(int xFrom, int yFrom, int xTo, int yTo)
        {
            var telePad = new Static(0x1822);
            var teleItem = new Teleporter(new Point3D(xTo, yTo, -1), Map.Malas);

            telePad.Hue = 0x482;
            telePad.MoveToWorld(new Point3D(xFrom, yFrom, -1), Map.Malas);

            teleItem.MoveToWorld(new Point3D(xFrom, yFrom, -1), Map.Malas);

            teleItem.SourceEffect = true;
            teleItem.DestEffect = true;
            teleItem.SoundID = 0x1FE;
        }

        public static void RemoveTeleporter(int xFrom, int yFrom, int xTo, int yTo)
        {
            RemoveItem<Teleporter>(xFrom, yFrom);
            RemoveItem<Static>(xFrom, yFrom);
        }

        public static void RemoveMobile<T>(int x, int y) where T : BaseCreature
        {
            foreach (var mobile in World.Mobiles.Values)
            {
                if (mobile.Map == Map.Malas && (mobile as T)?.Home == new Point3D(x, y, -1))
                {
                    mobile.Delete();
                    break;
                }
            }
        }

        public static void RemoveItem<T>(int x, int y, int z = -1) where T : Item
        {
            foreach (var item in Map.Malas.GetItemsInRange(new Point3D(x, y, z), 0))
            {
                if (item is T)
                {
                    item.Delete();
                    break;
                }
            }
        }

        public static void CreatePricedHealer(int price, int x, int y)
        {
            var healer = new PricedHealer(price);

            healer.MoveToWorld(new Point3D(x, y, -1), Map.Malas);

            healer.Home = healer.Location;
            healer.RangeHome = 5;
        }

        public static void CreateMorphItem(int x, int y, int inactiveItemID, int activeItemID, int range, int hue)
        {
            var item = new MorphItem(inactiveItemID, activeItemID, range) { Hue = hue };

            item.MoveToWorld(new Point3D(x, y, -1), Map.Malas);
        }

        public static void CreateVarietyDealer(int x, int y)
        {
            var dealer = new VarietyDealer
            {
                Name = "Nix",
                Title = "the Variety Dealer",
                Body = 400,
                Female = false,
                Hue = 0x8835,
                HairItemID = 0x2049, // Pig Tails
                HairHue = 0x482,
                FacialHairItemID = 0x203E,
                FacialHairHue = 0x482
            };

            var items = new List<Item>(dealer.Items);

            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];

                if (item.Layer is not Layer.ShopBuy and not Layer.ShopResale and not Layer.ShopSell)
                {
                    item.Delete();
                }
            }

            dealer.AddItem(new FloppyHat(1));
            dealer.AddItem(new Robe(1));
            dealer.AddItem(new LanternOfSouls());
            dealer.AddItem(new Sandals(0x482));
            /* End outfit */

            dealer.MoveToWorld(new Point3D(x, y, -1), Map.Malas);

            dealer.Home = dealer.Location;
            dealer.RangeHome = 2;
        }
    }
}
