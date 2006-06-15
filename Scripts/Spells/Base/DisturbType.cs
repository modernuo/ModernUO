using System;
using Server;

namespace Server.Spells
{
	public enum DisturbType
	{
		Unspecified,
		EquipRequest,
		UseRequest,
		Hurt,
		Kill,
		NewCast
	}
}