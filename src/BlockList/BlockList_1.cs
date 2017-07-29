using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Clever.Collections.Internal;
using Clever.Collections.Internal.Diagnostics;
using static Clever.Collections.BlockList;

namespace Clever.Collections
{
    [DebuggerDisplay(DebuggerStrings.DisplayFormat)]
    [DebuggerTypeProxy(typeof(EnumerableDebuggerProxy<>))]
    public partial class BlockList<T> : IList<T>, IReadOnlyList<T>
    {
        private readonly Options _options;

        private SmallList<T[]> _tail; // This is a mutable struct field; do not make it readonly.
        private T[] _head;
        private int _headCount;
        private int _count;
        private int _capacity;
        
        public BlockList()
            : this(DefaultOptions)
        {
        }

        public BlockList(Options options)
        {
            Verify.NotNull(options, nameof(options));

            _head = Array.Empty<T>();
            _options = options;
        }

        public BlockList(IEnumerable<T> items)
            : this(items, DefaultOptions)
        {
        }

        public BlockList(IEnumerable<T> items, Options options)
            : this(options)
        {
            AddRange(items);
        }

        public int BlockCount => _tail.Count + 1;

        public BlockView<T> Blocks => new BlockView<T>(this);

        public int Capacity => _capacity;

        public int Count => _count;

        public bool IsContiguous => BlockCount == 1;

        public bool IsEmpty => _count == 0;

        public bool IsFull => _count == _capacity;

        public Options Options => _options;

        internal Block<T> HeadSpan => new Block<T>(_head, _headCount);

        internal SmallList<T[]> Tail => _tail;

        [ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(Count)} = {Count}, {nameof(HeadCount)} = {HeadCount}, {nameof(HeadCapacity)} = {HeadCapacity}";

        private int HeadCapacity => _head.Length;

        [ExcludeFromCodeCoverage]
        private int HeadCount => _headCount;

        public ref T this[int index]
        {
            get
            {
                Verify.InRange(index >= 0 && index < _count, nameof(index));

                foreach (T[] block in _tail)
                {
                    if (index < block.Length)
                    {
                        return ref block[index];
                    }
                    index -= block.Length;
                }

                Debug.Assert(index < _head.Length);
                return ref _head[index];
            }
        }

        public void Add(T item)
        {
            if (IsFull)
            {
                Resize();
            }

            _head[_headCount++] = item;
            _count++;
        }

        public void AddRange(IEnumerable<T> items)
        {
            Verify.NotNull(items, nameof(items));

            foreach (T item in items)
            {
                Add(item);
            }
        }

        public ReadOnlyCollection<T> AsReadOnly() => new ReadOnlyCollection<T>(this);

        public void Clear()
        {
            // Capture relevant state in local variables, so we can use them after Reset() wipes the fields.
            var head = _head;
            int headCount = _headCount;
            var tail = _tail;
            Reset();

            Array.Clear(head, 0, headCount);

            for (int i = 0; i < tail.Count; i++)
            {
                T[] block = tail[i];
                tail[i] = null;
                Array.Clear(block, 0, block.Length);
            }
        }

        public bool Contains(T item) => IndexOf(item) != -1;

        public void CopyTo(T[] array) => CopyTo(array, 0);

        public void CopyTo(T[] array, int arrayIndex)
        {
            Verify.NotNull(array, nameof(array));
            Verify.InRange(arrayIndex >= 0 && array.Length - arrayIndex >= _count, nameof(arrayIndex));

            foreach (T[] block in _tail)
            {
                Array.Copy(block, 0, array, arrayIndex, block.Length);
                arrayIndex += block.Length;
            }

            Array.Copy(_head, 0, array, arrayIndex, _headCount);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            Verify.InRange(index >= 0 && _count - index >= count, nameof(index));
            Verify.NotNull(array, nameof(array));
            Verify.InRange(arrayIndex >= 0 && array.Length - arrayIndex >= count, nameof(arrayIndex));
            Verify.InRange(count >= 0, nameof(count));

            if (count == 0)
            {
                return;
            }

            var copyPos = GetPosition(index);
            CopyBlockEnd(copyPos.BlockIndex, copyPos.ElementIndex, array, ref arrayIndex, ref count);

            for (int blockIndex = copyPos.BlockIndex + 1; blockIndex <= _tail.Count; blockIndex++)
            {
                if (count == 0)
                {
                    return;
                }
                CopyBlock(blockIndex, array, ref arrayIndex, ref count);
            }
        }

        public T First()
        {
            Verify.ValidState(!IsEmpty, Strings.First_EmptyCollection);

            return Blocks[0][0];
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public int IndexOf(T item)
        {
            int processed = 0;
            foreach (T[] block in _tail)
            {
                int index = Array.IndexOf(block, item);
                if (index != -1)
                {
                    return processed + index;
                }
                processed += block.Length;
            }

            int headIndex = Array.IndexOf(_head, item, 0, _headCount);
            return headIndex != -1 ? processed + headIndex : -1;
        }

        public void Insert(int index, T item)
        {
            Verify.InRange(index >= 0 && index <= _count, nameof(index));

            if (index == _count)
            {
                Add(item);
                return;
            }

            Debug.Assert(!IsEmpty);

            // Here's how this procedure will look like when the insert position is 2 blocks
            // behind the head block.

            // Capture last item.
            // Shift at _tail.Count (head).
            // Add last item (only for head).
            // Move up.
            // Shift last item: done with _tail.Count (head).
            // Shift at _tail.Count - 1.
            // Move up.
            // Shift last item: done with _tail.Count - 1.
            // Shift end at _tail.Count - 2 (only the part after the insert position).
            // Write the item.

            T last = Last();
            var insertPos = GetPosition(index);

            // We have to special-case when the insert position is in the head block,
            // since we must also add the last item after ShiftEnd() is called.
            if (insertPos.BlockIndex == _tail.Count)
            {
                ShiftEndRight(_tail.Count, insertPos.ElementIndex);
                // This must run first in case _head changes during Add.
                _head[insertPos.ElementIndex] = item;
                Add(last);
                return;
            }

            ShiftRight(_tail.Count);

            {
                int blockIndex = _tail.Count - 1;
                // Since the insert position wasn't in the head block, it must be in a block
                // preceding the head block, and that means there are multiple blocks.
                Debug.Assert(blockIndex >= 0);

                // Add() must run after calculating blockIndex, in case it affects _tail.Count.
                Add(last);

                while (true)
                {
                    ShiftLastRight(blockIndex);
                    if (blockIndex == insertPos.BlockIndex)
                    {
                        break;
                    }
                    ShiftRight(blockIndex);
                    blockIndex--;
                }

                ShiftEndRight(blockIndex, insertPos.ElementIndex);
                _tail[blockIndex][insertPos.ElementIndex] = item;
            }
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            Verify.NotNull(items, nameof(items));
            Verify.InRange(index >= 0 && index <= Count, nameof(index));

            foreach (T item in items)
            {
                Insert(index++, item);
            }
        }

        public T Last()
        {
            Verify.ValidState(!IsEmpty, Strings.Last_EmptyCollection);

            return _head[_headCount - 1];
        }

        public Block<T> MoveToBlock()
        {
            Verify.ValidState(IsContiguous, Strings.MoveToBlock_NotContiguous);

            var result = HeadSpan;
            Reset();
            return result;
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            Verify.InRange(index >= 0 && index < _count, nameof(index));

            if (index < _count - 1)
            {
                var removePos = GetPosition(index);
                ShiftEndLeft(removePos.BlockIndex, removePos.ElementIndex);

                for (int blockIndex = removePos.BlockIndex + 1; blockIndex <= _tail.Count; blockIndex++)
                {
                    ShiftFirstLeft(blockIndex);
                    ShiftLeft(blockIndex);
                }
            }

            RemoveLast();
        }

        public void RemoveRange(int index, int count)
        {
            Verify.InRange(index >= 0, nameof(index));
            Verify.InRange(count >= 0 && Count - index >= count, nameof(count));

            for (int i = 0; i < count; i++)
            {
                RemoveAt(index);
            }
        }

        public void Reverse() => Reverse(0, _count);

        public void Reverse(int index, int count)
        {
            Verify.InRange(index >= 0, nameof(index));
            Verify.InRange(count >= 0 && _count - index >= count, nameof(count));

            int i = index, j = index + count - 1;
            while (i < j)
            {
                T temp = this[i];
                this[i] = this[j];
                this[j] = temp;
                i++;
                j--;
            }
        }

        public T[] ToArray()
        {
            if (IsEmpty)
            {
                return Array.Empty<T>();
            }

            var array = new T[_count];
            CopyTo(array);
            return array;
        }

        private void CopyBlock(int blockIndex, T[] array, ref int arrayIndex, ref int count)
        {
            CopyBlockEnd(blockIndex, 0, array, ref arrayIndex, ref count);
        }

        private void CopyBlockEnd(int blockIndex, int elementIndex, T[] array, ref int arrayIndex, ref int count)
        {
            var block = Blocks[blockIndex];
            int copyCount = Math.Min(count, block.Count);
            Array.Copy(block.Array, elementIndex, array, arrayIndex, copyCount);

            arrayIndex += copyCount;
            count -= copyCount;
        }

        private Position GetPosition(int index)
        {
            Debug.Assert(index >= 0 && index < _count);

            int blockIndex = -1, elementIndex = index;

            for (int i = 0; i < _tail.Count; i++)
            {
                T[] block = _tail[i];
                if (elementIndex < block.Length)
                {
                    blockIndex = i;
                    break;
                }
                elementIndex -= block.Length;
            }

            if (blockIndex == -1)
            {
                Debug.Assert(elementIndex < _headCount);
                blockIndex = _tail.Count;
            }

            return new Position(blockIndex, elementIndex);
        }

        private void RemoveLast()
        {
            Debug.Assert(!IsEmpty);

            _count--;
            _head[--_headCount] = default(T);

            // To maintain invariants, we need to make sure the head block isn't empty unless
            // the entire block list is empty.
            if (_headCount == 0)
            {
                if (_tail.IsEmpty)
                {
                    // The entire block list is empty, so revert to the initial state.
                    Reset();
                }
                else
                {
                    // Throw away the current head block and pretend we've just finished filling the last block.
                    _capacity -= HeadCapacity;
                    _head = _tail.RemoveLast();
                    _headCount = _head.Length;
                }
                Debug.Assert(IsFull);
            }
        }

        private void Reset()
        {
            _tail = new SmallList<T[]>();
            _head = Array.Empty<T>();
            _headCount = 0;
            _count = 0;
            _capacity = 0;
        }

        private void Resize()
        {
            Debug.Assert(IsFull);

            int initialCapacity = _options.InitialCapacity;
            if (IsEmpty)
            {
                _head = new T[initialCapacity];
                _capacity = initialCapacity;
                return;
            }

            _tail.Add(_head);
            // We want to increase the block sizes geometrically, but not on the first resize.
            // This ensures we never waste more than 50% of the memory we've allocated.
            int nextCapacity = _capacity == initialCapacity
                ? initialCapacity
                : HeadCapacity * 2;
            _head = new T[nextCapacity];
            _headCount = 0;
            _capacity += nextCapacity;
        }

        private void ShiftEndLeft(int blockIndex, int elementIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, elementIndex + 1, block.Array, elementIndex, block.Count - elementIndex - 1);
        }

        private void ShiftEndRight(int blockIndex, int elementIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, elementIndex, block.Array, elementIndex + 1, block.Count - elementIndex - 1);
        }

        private void ShiftFirstLeft(int blockIndex)
        {
            Debug.Assert(blockIndex > 0);

            var block = Blocks[blockIndex];
            var predecessor = _tail[blockIndex - 1];
            Debug.Assert(!block.IsEmpty && predecessor.Length > 0);

            predecessor[predecessor.Length - 1] = block.First();
        }

        private void ShiftLastRight(int blockIndex)
        {
            Debug.Assert(blockIndex < _tail.Count);

            T[] block = _tail[blockIndex];
            var successor = Blocks[blockIndex + 1];
            Debug.Assert(block.Length > 0 && !successor.IsEmpty);

            successor[0] = block.Last();
        }

        private void ShiftLeft(int blockIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, 1, block.Array, 0, block.Count - 1);
        }

        private void ShiftRight(int blockIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, 0, block.Array, 1, block.Count - 1);
        }

        [ExcludeFromCodeCoverage]
        T IList<T>.this[int index]
        {
            get => this[index];
            set => this[index] = value;
        }

        [ExcludeFromCodeCoverage]
        T IReadOnlyList<T>.this[int index] => this[index];

        [ExcludeFromCodeCoverage]
        bool ICollection<T>.IsReadOnly => false;

        [ExcludeFromCodeCoverage]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        [ExcludeFromCodeCoverage]
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
