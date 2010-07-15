using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Server.Reflection
{
	public sealed class MethodInvoker
	{
		private MethodInfo m_Method;
		private bool m_MethodEmpty;

		public MethodInfo Method { get { return m_Method; } }
		public bool IsMethodEmpty { get { return m_MethodEmpty; } }

		private InvokeHandler m_MethodHandler;

		public MethodInvoker( Type type, string methodname )
		{
			MethodInfo mi = type.GetMethod( methodname );

			if ( mi != null )
				m_MethodHandler = BaseMethodInvoker.GetMethodInvoker( mi );

			m_MethodEmpty = (mi == null) | (m_MethodHandler == null);
		}

		public MethodInvoker( MethodInfo method )
		{
			m_Method = method;

			if ( method != null )
				m_MethodHandler = BaseMethodInvoker.GetMethodInvoker( method );

			m_MethodEmpty = (method == null) | (m_MethodHandler == null);
		}

		public object Invoke( object target, params object[] parameters )
		{
			if ( m_MethodEmpty )
				return null;

			return m_MethodHandler.Invoke( target, parameters );
		}

		public IAsyncResult BeginInvoke( object target, object[] parameters, AsyncCallback callback, object obj )
		{
			if ( m_MethodEmpty )
				return null;

			return m_MethodHandler.BeginInvoke( target, parameters, callback, obj );
		}

		public object EndInvoke( IAsyncResult asyncResult )
		{
			if ( m_MethodEmpty )
				return null;

			return m_MethodHandler.EndInvoke( asyncResult );
		}
	}
}
