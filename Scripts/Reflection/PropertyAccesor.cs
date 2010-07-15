using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Server.Reflection
{
	public sealed class PropertyAccessor
	{
		private Type m_Type;
		private PropertyInfo m_Property;
		private bool m_PropertyEmpty;

		public Type Type { get { return m_Type; } }
		public PropertyInfo PropertyInfo { get { return m_Property; } }
		public bool IsPropertyEmpty { get { return m_PropertyEmpty; } }

		private InvokeHandler m_GetMethodHandler;
		private InvokeHandler m_SetMethodHandler;

		public PropertyAccessor( Type type, string propertyName )
		{
			m_Type = type;

			m_GetMethodHandler = BasePropertyAccessor.GetPropertyInvoker( m_Type, propertyName );
			m_SetMethodHandler = BasePropertyAccessor.SetPropertyInvoker( m_Type, propertyName );

			if ( m_GetMethodHandler == null || m_SetMethodHandler == null )
				m_PropertyEmpty = true;
		}

		public PropertyAccessor( Type type, PropertyInfo property )
		{
			m_Type = type;
			m_Property = property;

			if ( property == null )
			{
				m_PropertyEmpty = true;
				return;
			}

			m_GetMethodHandler = BasePropertyAccessor.GetPropertyInvoker( m_Type, property );
			m_SetMethodHandler = BasePropertyAccessor.SetPropertyInvoker( m_Type, property );

			if ( m_GetMethodHandler == null || m_SetMethodHandler == null )
				m_PropertyEmpty = true;
		}

		public object GetValue( object target, params object[] parameters )
		{
			if ( m_PropertyEmpty )
				return null;

			return m_GetMethodHandler( target, parameters );
		}

		public void SetValue( object target, params object[] parameters )
		{
			if ( m_PropertyEmpty )
				return;

			m_SetMethodHandler( target, parameters );
		}
	}
}
