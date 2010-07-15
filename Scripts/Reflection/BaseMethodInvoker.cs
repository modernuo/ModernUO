using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Server.Reflection
{
	public static class BaseMethodInvoker
	{
		public static InvokeHandler GetMethodInvoker( MethodInfo method )
		{
			DynamicMethod dynamicMethod = new DynamicMethod( string.Empty,
				typeof( object ), new Type[] { typeof( object ), typeof( object[] ) }, method.DeclaringType.Module );

			ILGenerator il = dynamicMethod.GetILGenerator();
			ParameterInfo[] ps = method.GetParameters();
			Type[] paramTypes = new Type[ps.Length];
			for ( int i = 0; i < paramTypes.Length; i++ )
			{
				if ( ps[i].ParameterType.IsByRef )
					paramTypes[i] = ps[i].ParameterType.GetElementType();
				else
					paramTypes[i] = ps[i].ParameterType;
			}
			LocalBuilder[] locals = new LocalBuilder[paramTypes.Length];

			for ( int i = 0; i < paramTypes.Length; i++ )
				locals[i] = il.DeclareLocal( paramTypes[i], true );

			for ( int i = 0; i < paramTypes.Length; i++ )
			{
				il.Emit( OpCodes.Ldarg_1 );
				EmitFastInt( il, i );
				il.Emit( OpCodes.Ldelem_Ref );
				EmitCastToReference( il, paramTypes[i] );
				il.Emit( OpCodes.Stloc, locals[i] );
			}

			if ( !method.IsStatic )
				il.Emit( OpCodes.Ldarg_0 );

			for ( int i = 0; i < paramTypes.Length; i++ )
			{
				if ( ps[i].ParameterType.IsByRef )
					il.Emit( OpCodes.Ldloca_S, locals[i] );
				else
					il.Emit( OpCodes.Ldloc, locals[i] );
			}

			if ( method.IsStatic )
				il.EmitCall( OpCodes.Call, method, null );
			else
				il.EmitCall( OpCodes.Callvirt, method, null );

			if ( method.ReturnType == typeof( void ) )
				il.Emit( OpCodes.Ldnull );
			else
				EmitBoxIfNeeded( il, method.ReturnType );

			for ( int i = 0; i < paramTypes.Length; i++ )
			{
				if ( ps[i].ParameterType.IsByRef )
				{
					il.Emit( OpCodes.Ldarg_1 );
					EmitFastInt( il, i );
					il.Emit( OpCodes.Ldloc, locals[i] );
					if ( locals[i].LocalType.IsValueType )
						il.Emit( OpCodes.Box, locals[i].LocalType );
					il.Emit( OpCodes.Stelem_Ref );
				}
			}

			il.Emit( OpCodes.Ret );
			InvokeHandler invoder = (InvokeHandler)dynamicMethod.CreateDelegate( typeof( InvokeHandler ) );
			return invoder;
		}

		public static InvokeHandler GetMethodInvoker( Type type, string methodName )
		{
			MethodInfo methodInfo = type.GetMethod( methodName );
			return GetMethodInvoker( methodInfo );
		}

		private static void EmitCastToReference( ILGenerator il, System.Type type )
		{
			if ( type.IsValueType )
			{
				il.Emit( OpCodes.Unbox_Any, type );
			}
			else
			{
				il.Emit( OpCodes.Castclass, type );
			}
		}

		private static void EmitBoxIfNeeded( ILGenerator il, System.Type type )
		{
			if ( type.IsValueType )
			{
				il.Emit( OpCodes.Box, type );
			}
		}

		private static void EmitFastInt( ILGenerator il, int value )
		{
			switch ( value )
			{
				case -1:
					il.Emit( OpCodes.Ldc_I4_M1 );
					return;
				case 0:
					il.Emit( OpCodes.Ldc_I4_0 );
					return;
				case 1:
					il.Emit( OpCodes.Ldc_I4_1 );
					return;
				case 2:
					il.Emit( OpCodes.Ldc_I4_2 );
					return;
				case 3:
					il.Emit( OpCodes.Ldc_I4_3 );
					return;
				case 4:
					il.Emit( OpCodes.Ldc_I4_4 );
					return;
				case 5:
					il.Emit( OpCodes.Ldc_I4_5 );
					return;
				case 6:
					il.Emit( OpCodes.Ldc_I4_6 );
					return;
				case 7:
					il.Emit( OpCodes.Ldc_I4_7 );
					return;
				case 8:
					il.Emit( OpCodes.Ldc_I4_8 );
					return;
			}

			if ( value > -129 && value < 128 )
			{
				il.Emit( OpCodes.Ldc_I4_S, (SByte)value );
			}
			else
			{
				il.Emit( OpCodes.Ldc_I4, value );
			}
		}
	}
}
