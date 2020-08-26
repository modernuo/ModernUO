// Copyright (c) Harry Pierson. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Buffers
{
    public ref struct BufferReader<T>
    {
        private readonly bool usingSequence;
        private readonly ReadOnlySequence<T> sequence;
        private SequencePosition currentPosition;
        private SequencePosition nextPosition;
        private bool moreData;
        private readonly long length;

        public BufferReader(ReadOnlySpan<T> span)
        {
            usingSequence = false;
            CurrentSpanIndex = 0;
            Consumed = 0;
            sequence = default;
            currentPosition = default;
            length = span.Length;

            CurrentSpan = span;
            nextPosition = default;
            moreData = span.Length > 0;
        }

        public BufferReader(in ReadOnlySequence<T> sequence)
        {
            usingSequence = true;
            CurrentSpanIndex = 0;
            Consumed = 0;
            this.sequence = sequence;
            currentPosition = sequence.Start;
            length = -1;

            var first = sequence.First.Span;
            CurrentSpan = first;
            nextPosition = sequence.GetPosition(first.Length);
            moreData = first.Length > 0;

            if (!moreData && !sequence.IsSingleSegment)
            {
                moreData = true;
                GetNextSpan();
            }
        }

        public readonly bool End => !moreData;

        public ReadOnlySpan<T> CurrentSpan { get; private set; }

        public int CurrentSpanIndex { get; private set; }

        public readonly ReadOnlySpan<T> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CurrentSpan.Slice(CurrentSpanIndex);
        }

        public long Consumed { get; private set; }

        public readonly long Remaining => Length - Consumed;

        public readonly long Length
        {
            get
            {
                if (length < 0)
                    // Cast-away readonly to initialize lazy field
                    Volatile.Write(ref Unsafe.AsRef(length), sequence.Length);

                return length;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryPeek([MaybeNullWhen(false)] out T value)
        {
            if (moreData)
            {
                value = CurrentSpan[CurrentSpanIndex];
                return true;
            }

            value = default!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead([MaybeNullWhen(false)] out T value)
        {
            if (End)
            {
                value = default!;
                return false;
            }

            value = CurrentSpan[CurrentSpanIndex];
            CurrentSpanIndex++;
            Consumed++;

            if (CurrentSpanIndex >= CurrentSpan.Length)
            {
                if (usingSequence)
                    GetNextSpan();
                else
                    moreData = false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Rewind(long count)
        {
            if ((ulong)count > (ulong)Consumed) throw new ArgumentOutOfRangeException(nameof(count));

            Consumed -= count;

            if (CurrentSpanIndex >= count)
            {
                CurrentSpanIndex -= (int)count;
                moreData = true;
            }
            else if (usingSequence)
            {
                // Current segment doesn't have enough data, scan backward through segments
                RetreatToPreviousSpan(Consumed);
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    $"Rewind went past the start of the memory by {count}."
                );
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RetreatToPreviousSpan(long consumed)
        {
            ResetReader();
            Advance(consumed);
        }

        private void ResetReader()
        {
            CurrentSpanIndex = 0;
            Consumed = 0;
            currentPosition = sequence.Start;
            nextPosition = currentPosition;

            if (sequence.TryGet(ref nextPosition, out var memory))
            {
                moreData = true;

                if (memory.Length == 0)
                {
                    CurrentSpan = default;
                    // No data in the first span, move to one with data
                    GetNextSpan();
                }
                else
                {
                    CurrentSpan = memory.Span;
                }
            }
            else
            {
                // No data in any spans and at end of sequence
                moreData = false;
                CurrentSpan = default;
            }
        }

        private void GetNextSpan()
        {
            if (!sequence.IsSingleSegment)
            {
                var previousNextPosition = nextPosition;
                while (sequence.TryGet(ref nextPosition, out var memory))
                {
                    currentPosition = previousNextPosition;
                    if (memory.Length > 0)
                    {
                        CurrentSpan = memory.Span;
                        CurrentSpanIndex = 0;
                        return;
                    }

                    CurrentSpan = default;
                    CurrentSpanIndex = 0;
                    previousNextPosition = nextPosition;
                }
            }

            moreData = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(long count)
        {
            const long TooBigOrNegative = unchecked((long)0xFFFFFFFF80000000);
            if ((count & TooBigOrNegative) == 0 && CurrentSpan.Length - CurrentSpanIndex > (int)count)
            {
                CurrentSpanIndex += (int)count;
                Consumed += count;
            }
            else if (usingSequence)
            {
                // Can't satisfy from the current span
                AdvanceToNextSpan(count);
            }
            else if (CurrentSpan.Length - CurrentSpanIndex == (int)count)
            {
                CurrentSpanIndex += (int)count;
                Consumed += count;
                moreData = false;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        private void AdvanceToNextSpan(long count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            Consumed += count;
            while (moreData)
            {
                var remaining = CurrentSpan.Length - CurrentSpanIndex;

                if (remaining > count)
                {
                    CurrentSpanIndex += (int)count;
                    count = 0;
                    break;
                }

                // As there may not be any further segments we need to
                // push the current index to the end of the span.
                CurrentSpanIndex += remaining;
                count -= remaining;

                GetNextSpan();

                if (count == 0) break;
            }

            if (count != 0)
            {
                // Not enough data left- adjust for where we actually ended and throw
                Consumed -= count;
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<T> destination)
        {
            // This API doesn't advance to facilitate conditional advancement based on the data returned.
            // We don't provide an advance option to allow easier utilizing of stack allocated destination spans.
            // (Because we can make this method readonly we can guarantee that we won't capture the span.)

            var firstSpan = UnreadSpan;
            if (firstSpan.Length >= destination.Length)
            {
                firstSpan.Slice(0, destination.Length).CopyTo(destination);
                return true;
            }

            // Not enough in the current span to satisfy the request, fall through to the slow path
            return TryCopyMultisegment(destination);
        }

        internal readonly bool TryCopyMultisegment(Span<T> destination)
        {
            // If we don't have enough to fill the requested buffer, return false
            if (Remaining < destination.Length)
                return false;

            var firstSpan = UnreadSpan;
            firstSpan.CopyTo(destination);
            var copied = firstSpan.Length;

            var next = nextPosition;
            while (sequence.TryGet(ref next, out var nextSegment))
                if (nextSegment.Length > 0)
                {
                    var nextSpan = nextSegment.Span;
                    var toCopy = Math.Min(nextSpan.Length, destination.Length - copied);
                    nextSpan.Slice(0, toCopy).CopyTo(destination.Slice(copied));
                    copied += toCopy;
                    if (copied >= destination.Length) break;
                }

            return true;
        }
    }
}
