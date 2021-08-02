using System;
using System.Reflection;
using System.Security;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Xanthos.Utilities
{
	public class Misc
	{
		public static object InvokeParameterlessMethod( object target, string method )
		{
			object result = null;

			try
			{
				Type objectType = target.GetType();
				MethodInfo methodInfo = objectType.GetMethod( method );

				result = methodInfo.Invoke( target, null );
			}
			catch ( SecurityException exc )
			{
				Console.WriteLine( "SecurityException: " + exc.Message );
			}
			return result;
		}
	}
}