/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CollectionThrowStrings.cs                                       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Collections;

public static class CollectionThrowStrings
{
    public const string ArgumentOutOfRange_Index =
        "Index was out of range. Must be non-negative and less than the size of the collection.";

    public const string ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

    public const string Argument_InvalidOffLen =
        "Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.";

    public const string Argument_AddingDuplicate = "An item with the same value has already been added. Value: {0}";

    public const string Arg_ArrayPlusOffTooSmall =
        "Destination array is not long enough to copy all the items in the collection. Check array index and length.";

    public const string InvalidOperation_ConcurrentOperationsNotSupported =
        "Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.";

    public const string InvalidOperation_EnumFailedVersion =
        "Collection was modified after the enumerator was instantiated.";

    public const string InvalidOperation_EmptyQueue = "Queue empty.";

    public const string InvalidOperation_EnumNotStarted = "Enumeration has not started. Call MoveNext.";

    public const string InvalidOperation_EnumEnded = "Enumeration already finished.";

    public const string Argument_ArrayTooLarge =
        "The input array length must not exceed Int32.MaxValue / {0}. Otherwise BitArray.Length would exceed Int32.MaxValue.";

    public const string Arg_ArrayLengthsDiffer = "Array lengths must be the same.";

    public const string Arg_RankMultiDimNotSupported =
        "Only single dimensional arrays are supported for the requested action.";

    public const string Arg_BitArrayTypeUnsupported =
        "Only supported array types for CopyTo on BitArrays are Boolean[], Int32[] and Byte[].";
}
