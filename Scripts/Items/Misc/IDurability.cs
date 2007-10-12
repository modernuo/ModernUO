using System;
using Server;

namespace Server.Items
{
	interface IDurability
	{
		int InitMinHits { get; }
		int InitMaxHits { get; }

		int HitPoints { get; set; }
		int MaxHitPoints { get; set; }

		//Maybe a scale/unscale durability?
	}

	interface IWearableDurability : IDurability
	{
		int OnHit( BaseWeapon weapon, int damageTaken );
	}
}