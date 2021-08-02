using System;
using Server;
using Server.Mobiles;
using Xanthos.Interfaces;

namespace Xanthos.Interfaces
{
	public interface IEvoCreature
	{
		Type GetEvoDustType();
		int Ep { get; }
		int Stage { get; }
	}

	public interface IEvoGuardian
	{
	}
}