/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AuditedDirtyTrackingAttribute.cs                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server;

/// <summary>
/// Declares that a hand-written serializable class calls <see cref="ISerializableExtensions.MarkDirty" />
/// after every mutation of its serialized state, which makes it (and generated classes derived
/// from it) eligible for delta saves. Generated classes need no declaration. Applies to the
/// declaring class only; every class in an inheritance chain must qualify on its own.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditedDirtyTrackingAttribute : Attribute
{
    /// <summary>Who audited the class and when, for the next person who touches it.</summary>
    public string AuditedBy { get; }

    public AuditedDirtyTrackingAttribute(string auditedBy) => AuditedBy = auditedBy;
}
