using Server.Collections;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

public class DestroyEquipment : MonsterAbilityAttack
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.DestroyEquipment;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;
    public override double ChanceToTrigger => 0.05;

    public virtual int AttackRange => 1;

    protected override void OnAttack(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        base.OnAttack(trigger, source, defender);
        using var queue = PooledRefQueue<Item>.Create();

        for (var i = 0; i < defender.Items.Count; i++)
        {
            var item = defender.Items[i];
            if (item.Deleted || item.LootType is LootType.Blessed or LootType.Newbied || item.BlessedFor != null)
            {
                continue;
            }

            if (!(Mobile.InsuranceEnabled && item.Insured) && item is IDurability { MaxHitPoints: > 0 })
            {
                queue.Enqueue(item);
            }
        }

        if (queue.Count == 0)
        {
            return;
        }

        var toDestroy = queue.PeekRandom();
        var name = toDestroy.Name?.Trim().DefaultIfNullOrEmpty(toDestroy.ItemData.Name);

        // TODO: Is there supposed to be a special effect?
        // TODO: Is there supposed to be a special sound?
        toDestroy.Delete();

        // Their ~1_NAME~ is destroyed by the attack.
        defender.NonlocalOverheadMessage(MessageType.Regular, 0x3B2, 1080034, name);

        // Your ~1_NAME~ is destroyed by the attack.
        defender.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1080035, name);
    }

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        defender.Player && defender.AccessLevel == AccessLevel.Player && source.InRange(defender, AttackRange);
}
