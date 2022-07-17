using System;
using Server.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public class ArcaneCircleSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new(
            "Arcane Circle",
            "Myrshalee",
            -1
        );

        public ArcaneCircleSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(0.5);

        public override double RequiredSkill => 0.0;
        public override int RequiredMana => 24;

        public override bool CheckCast()
        {
            if (!IsValidLocation(Caster.Location, Caster.Map))
            {
                // You must be standing on an arcane circle, pentagram or abattoir to use this spell.
                Caster.SendLocalizedMessage(1072705);
                return false;
            }

            if (!CheckArcanists())
            {
                Caster.SendLocalizedMessage(1080452); // There are not enough spellweavers present to create an Arcane Focus.
                return false;
            }

            return base.CheckCast();
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.FixedParticles(0x3779, 10, 20, 0x0, EffectLayer.Waist);
                Caster.PlaySound(0x5C0);

                var spellWeaving = Caster.Skills.Spellweaving.Value;
                using var pool = GetArcanists(spellWeaving);

                var duration = TimeSpan.FromHours(Math.Max(1, (int)(spellWeaving / 24)));

                var strengthBonus =
                    Math.Min(pool.Count, IsSanctuary(Caster.Location, Caster.Map) ? 6 : 5);

                while (pool.Count > 0)
                {
                    var m = pool.Dequeue();
                    GiveArcaneFocus(m, duration, strengthBonus);
                }
            }

            FinishSequence();
        }

        private static bool IsSanctuary(Point3D p, Map m) =>
            (m == Map.Trammel || m == Map.Felucca) && p.X == 6267 && p.Y == 131;

        private static bool IsValidLocation(Point3D location, Map map)
        {
            var lt = map.Tiles.GetLandTile(location.X, location.Y); // Land   Tiles

            if (IsValidTile(lt.ID) && lt.Z == location.Z)
            {
                return true;
            }

            var tiles = map.Tiles.GetStaticTiles(location.X, location.Y); // Static Tiles

            for (var i = 0; i < tiles.Length; ++i)
            {
                var t = tiles[i];
                var id = TileData.ItemTable[t.ID & TileData.MaxItemValue];

                var tand = t.ID;

                if (t.Z + id.CalcHeight != location.Z)
                {
                    continue;
                }

                if (IsValidTile(tand))
                {
                    return true;
                }
            }

            var eable = map.GetItemsInRange(location, 0);

            foreach (var item in eable)
            {
                if (item.Z + item.ItemData.CalcHeight == location.Z && IsValidTile(item.ItemID))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();

            return false;
        }

        public static bool IsValidTile(int itemID) =>
            itemID is 0xFEA or 0x1216 or 0x307F or 0x1D10 or 0x1D0F or 0x1D1F or 0x1D12;

        private bool CheckArcanists()
        {
            var spellWeaving = Caster.Skills.Spellweaving.Value;
            var eable = Caster.GetMobilesInRange(1);
            foreach (var m in eable)
            {
                if (m != Caster && m is PlayerMobile && Caster.CanBeBeneficial(m, false) &&
                    Math.Abs(spellWeaving - m.Skills.Spellweaving.Value) <= 20)
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private PooledRefQueue<Mobile> GetArcanists(double spellWeaving)
        {
            // OSI Verified: Even enemies/combatants count
            // Everyone gets the Arcane Focus, power capped elsewhere

            var pool = PooledRefQueue<Mobile>.Create();
            var eable = Caster.GetMobilesInRange(1);

            foreach (var m in eable)
            {
                if (m != Caster && m is PlayerMobile && Caster.CanBeBeneficial(m, false) &&
                    Math.Abs(spellWeaving - m.Skills.Spellweaving.Value) <= 20)
                {
                    pool.Enqueue(m);
                }
            }

            eable.Free();
            return pool;
        }

        private void GiveArcaneFocus(Mobile to, TimeSpan duration, int strengthBonus)
        {
            if (to == null) // Sanity
            {
                return;
            }

            var focus = FindArcaneFocus(to);

            if (focus == null)
            {
                focus = new ArcaneFocus(duration, strengthBonus);
                if (to.PlaceInBackpack(focus))
                {
                    focus.SendTimeRemainingMessage(to);
                    to.SendLocalizedMessage(1072740); // An arcane focus appears in your backpack.
                }
                else
                {
                    focus.Delete();
                }
            }
            else // OSI renewal rules: the new one will override the old one, always.
            {
                to.SendLocalizedMessage(1072828); // Your arcane focus is renewed.
                focus.LifeSpan = duration;
                focus.CreationTime = Core.Now;
                focus.StrengthBonus = strengthBonus;
                focus.InvalidateProperties();
                focus.SendTimeRemainingMessage(to);
            }
        }
    }
}
