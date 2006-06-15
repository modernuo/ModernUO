using System;
using Server;

namespace Server.Misc
{
	[AttributeUsage( AttributeTargets.Class )]
	public class DispellableFieldAttribute : Attribute
	{
	}
}