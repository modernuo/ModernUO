using System;

namespace Server.Gumps
{
    public abstract class PropertyException : Exception
    {
        protected Property m_Property;

        public PropertyException(Property property, string message)
            : base(message) =>
            m_Property = property;

        public Property Property => m_Property;
    }

    public abstract class BindingException : PropertyException
    {
        public BindingException(Property property, string message)
            : base(property, message)
        {
        }
    }

    public sealed class NotYetBoundException : BindingException
    {
        public NotYetBoundException(Property property)
            : base(property, "Property has not yet been bound.")
        {
        }
    }

    public sealed class AlreadyBoundException : BindingException
    {
        public AlreadyBoundException(Property property)
            : base(property, "Property has already been bound.")
        {
        }
    }

    public sealed class UnknownPropertyException : BindingException
    {
        public UnknownPropertyException(Property property, string current)
            : base(property, $"Property '{current}' not found.")
        {
        }
    }

    public sealed class ReadOnlyException : BindingException
    {
        public ReadOnlyException(Property property)
            : base(property, "Property is read-only.")
        {
        }
    }

    public sealed class WriteOnlyException : BindingException
    {
        public WriteOnlyException(Property property)
            : base(property, "Property is write-only.")
        {
        }
    }

    public abstract class AccessException : PropertyException
    {
        public AccessException(Property property, string message)
            : base(property, message)
        {
        }
    }

    public sealed class InternalAccessException : AccessException
    {
        public InternalAccessException(Property property)
            : base(property, "Property is internal.")
        {
        }
    }

    public abstract class ClearanceException : AccessException
    {
        public ClearanceException(Property property, AccessLevel playerAccess, AccessLevel neededAccess, string accessType)
            : base(
                property,
                $"You must be at least {Mobile.GetAccessLevelName(neededAccess)} to {accessType} this property."
            )
        {
        }

        public AccessLevel PlayerAccess { get; set; }
        public AccessLevel NeededAccess { get; set; }
    }

    public sealed class ReadAccessException : ClearanceException
    {
        public ReadAccessException(Property property, AccessLevel playerAccess, AccessLevel neededAccess)
            : base(property, playerAccess, neededAccess, "read")
        {
        }
    }

    public sealed class WriteAccessException : ClearanceException
    {
        public WriteAccessException(Property property, AccessLevel playerAccess, AccessLevel neededAccess)
            : base(property, playerAccess, neededAccess, "write")
        {
        }
    }
}
