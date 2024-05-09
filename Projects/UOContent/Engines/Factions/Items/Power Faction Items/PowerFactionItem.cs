using System;
using System.IO;
using ModernUO.Serialization;
using Server.Factions;
using Server.Mobiles;
using Server.Random;

namespace Server;

[SerializationGenerator(0, false)]
public abstract partial class PowerFactionItem : Item
{
    private static readonly WeightedValue<Type>[] _items =
    {
        new(30, typeof(GemOfEmpowerment)),
        new(25, typeof(BloodRose)),
        new(20, typeof(ClarityPotion)),
        new(15, typeof(UrnOfAscension)),
        new(10, typeof(StormsEye))
    };

    private const int TotalWeight = 30 + 25 + 20 + 15 + 10;

    public PowerFactionItem(int itemId) : base(itemId)
    {
    }

    public abstract bool Use(Mobile mob);

    public static void CheckSpawn(Mobile killer, Mobile victim)
    {
        if (killer == null || victim == null)
        {
            return;
        }

        var ps = PlayerState.Find(victim);

        if (ps == null)
        {
            return;
        }

        var chance = ps.Rank.Rank;

        if (chance <= Utility.Random(100))
        {
            return;
        }

        var weight = Utility.Random(TotalWeight);

        foreach (var item in _items)
        {
            if (weight < item.Weight)
            {
                var obj = item.Value.CreateInstance<Item>();

                if (obj != null)
                {
                    killer.AddToBackpack(obj);

                    killer.SendSound(1470);
                    killer.LocalOverheadMessage(
                        MessageType.Regular,
                        2119,
                        false,
                        "You notice a strange item on the corpse, and decide to pick it up."
                    );

                    try
                    {
                        using var op = new StreamWriter("faction-power-items.log", true);
                        op.WriteLine($"{Core.Now}\t{killer}\t{victim}\t{obj}");
                    }
                    catch
                    {
                        // ignored
                    }
                }

                break;
            }

            weight -= item.Weight;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042038); // You must have the object in your backpack to use it.
        }
        else if (from is PlayerMobile mobile && mobile.DuelContext != null)
        {
            mobile.SendMessage("You can't use that.");
        }
        else if (Faction.Find(from) == null)
        {
            from.LocalOverheadMessage(
                MessageType.Regular,
                2119,
                false,
                "The object vanishes from your hands as you touch it."
            );

            Timer.StartTimer(
                TimeSpan.FromSeconds(1.0),
                () => from.LocalOverheadMessage(
                    MessageType.Regular,
                    2118,
                    false,
                    "You feel a strange tingling sensation throughout your body."
                )
            );

            Timer.StartTimer(
                TimeSpan.FromSeconds(4.0),
                () => { from.LocalOverheadMessage(MessageType.Regular, 2118, false, "Your skin begins to burn."); }
            );

            new DestructionTimer(from).Start();
            Delete();

            // from.SendMessage( "You must be in a faction to use this item." );
        }
        else if (Use(from))
        {
            from.RevealingAction();
            Consume();
        }
    }

    private sealed class DestructionTimer : Timer
    {
        private readonly Mobile _mobile;

        private bool _screamed;

        public DestructionTimer(Mobile mob) : base(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0.1), 10) =>
            _mobile = mob;

        protected override void OnTick()
        {
            if (!_mobile.Alive)
            {
                return;
            }

            if (!_screamed)
            {
                _screamed = true;

                _mobile.PlaySound(_mobile.Female ? 814 : 1088);
                _mobile.PublicOverheadMessage(MessageType.Regular, 2118, false, "Aaaaah!");
            }

            _mobile.Damage(Utility.Dice(2, 6, 0));
        }
    }
}
