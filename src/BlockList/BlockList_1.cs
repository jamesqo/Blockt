using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Clever.Collections.Internal;
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

        public int Capacity => _capacity;

        public int Count => _count;

        public bool IsContiguous => BlockCount == 1;

        public bool IsEmpty => _count == 0;

        public bool IsFull => _count == _capacity;

        public Options Options => _options;

        private string DebuggerDisplay => $"{nameof(Count)} = {Count}, {nameof(HeadCount)} = {HeadCount}, {nameof(HeadCapacity)} = {HeadCapacity}";

        private int HeadCapacity => _head.Length;

        private int HeadCount => _headCount;

        private Block<T> HeadSpan => new Block<T>(_head, _headCount);

        public T this[int index]
        {
            get => GetRef(index);
            set => GetRef(index) = value;
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

        public void Clear()
        {
            for (int i = 0; i < _tail.Count; i++)
            {
                T[] block = _tail[i];
                _tail[i] = null;
                Array.Clear(block, 0, block.Length);
            }
            _tail = new SmallList<T[]>();

            Array.Clear(_head, 0, _headCount);
            _head = Array.Empty<T>();

            _headCount = 0;
            _count = 0;
            _capacity = 0;
        }

        public bool Contains(T item) => IndexOf(item) != -1;

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

        public T First()
        {
            Verify.ValidState(!IsEmpty, Strings.First_EmptyCollection);

            return GetBlock(0).Array[0];
        }

        public Block<T> GetBlock(int index)
        {
            Verify.InRange(index >= 0 && index < BlockCount, nameof(index));

            if (index < _tail.Count)
            {
                return new Block<T>(_tail[index]);
            }

            Debug.Assert(index == _tail.Count);
            return HeadSpan;
        }

        public Block<T>[] GetBlocks()
        {
            var blocks = new Block<T>[BlockCount];
            for (int i = 0; i < _tail.Count; i++)
            {
                blocks[i] = new Block<T>(_tail[i]);
            }
            blocks[_tail.Count] = HeadSpan;
            return blocks;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public ref T GetRef(int index)
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
                ShiftEnd(_tail.Count, insertPos.ElementIndex);
                // This must run first in case _head changes during Add.
                _head[insertPos.ElementIndex] = item;
                Add(last);
                return;
            }

            Shift(_tail.Count);

            {
                int blockIndex = _tail.Count - 1;
                // Since the insert position wasn't in the head block, it must be in a block
                // preceding the head block, and that means there are multiple blocks.
                Debug.Assert(blockIndex >= 0);

                // Add() must run after calculating blockIndex, in case it affects _tail.Count.
                Add(last);

                while (true)
                {
                    ShiftLast(blockIndex);
                    if (blockIndex == insertPos.BlockIndex)
                    {
                        break;
                    }
                    Shift(blockIndex);
                    blockIndex--;
                }

                ShiftEnd(blockIndex, insertPos.ElementIndex);
                _tail[blockIndex][insertPos.ElementIndex] = item;
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
            _head = Array.Empty<T>();

            _headCount = 0;
            _count = 0;
            _capacity = 0;

            return result;
        }

        public void RemoveAt(int index) => throw new NotImplementedException();

        public T[] ToArray()
        {
            if (IsEmpty)
            {
                return Array.Empty<T>();
            }

            var array = new T[_count];
            CopyTo(array, 0);
            return array;
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
        
        private void Shift(int blockIndex)
        {
            var block = GetBlock(blockIndex);
            Array.Copy(block.Array, 0, block.Array, 1, block.Count - 1);
        }

        private void ShiftEnd(int blockIndex, int elementIndex)
        {
            var block = GetBlock(blockIndex);
            Array.Copy(block.Array, elementIndex, block.Array, elementIndex + 1, block.Count - elementIndex - 1);
        }

        private void ShiftLast(int blockIndex)
        {
            Debug.Assert(blockIndex < _tail.Count);

            T[] block = _tail[blockIndex];
            var successor = GetBlock(blockIndex + 1);
            Debug.Assert(block.Length > 0 && !successor.IsEmpty);

            successor[0] = block.Last();
        }

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
