using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Clever.Collections.Internal;
using static Clever.Collections.BlockList;

namespace Clever.Collections
{
    public class BlockList<T> : IList<T>
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

        public int BlockCount => TailCount + 1;

        public int Capacity => _capacity;

        public int Count => _count;

        public bool IsContiguous => BlockCount == 1;

        public bool IsEmpty => _count == 0;

        public bool IsFull => _count == _capacity;

        public Options Options => _options;

        private int HeadCapacity => _head.Length;

        private ArraySegment<T> HeadSpan => new ArraySegment<T>(_head, 0, _headCount);

        private int TailCount => _tail.Count;

        public T this[int index]
        {
            get => GetRef(index);
            set => GetRef(index) = value;
        }

        public void Add(T item)
        {
            if (IsFull)
            {
                MakeRoom();
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
            for (int i = 0; i < TailCount; i++)
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

        public bool Contains(T item)
        {
            foreach (T[] block in _tail)
            {
                if (Array.IndexOf(block, item) >= 0)
                {
                    return true;
                }
            }

            return Array.IndexOf(_head, item, 0, _headCount) >= 0;
        }

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

        public ArraySegment<T> GetBlock(int index)
        {
            Verify.InRange(index >= 0 && index < BlockCount, nameof(index));

            if (index < TailCount)
            {
                return new ArraySegment<T>(_tail[index]);
            }

            Debug.Assert(index == TailCount);
            return HeadSpan;
        }

        public ArraySegment<T>[] GetBlocks()
        {
            var blocks = new ArraySegment<T>[BlockCount];
            for (int i = 0; i < TailCount; i++)
            {
                blocks[i] = new ArraySegment<T>(_tail[i]);
            }
            blocks[TailCount] = HeadSpan;
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
                if (index >= 0)
                {
                    return processed + index;
                }
                processed += block.Length;
            }

            int headIndex = Array.IndexOf(_head, item, 0, _headCount);
            return headIndex >= 0 ? processed + headIndex : -1;
        }

        public void Insert(int index, T item) => throw new NotImplementedException();

        public ArraySegment<T> MoveToBlock()
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

        private void MakeRoom()
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

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly BlockList<T> _list;
            private ArraySegment<T> _currentBlock;
            private int _blockIndex;
            private int _elementIndex;

            internal Enumerator(BlockList<T> list)
                : this()
            {
                _list = list;
                _currentBlock = list.GetBlock(0);
                _elementIndex = -1;
            }

            // This can be changed to index _currentBlock directly once ArraySegment.this[] is available.
            public T Current => _currentBlock.Array[_currentBlock.Offset + _elementIndex];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_elementIndex + 1 == _currentBlock.Count)
                {
                    if (_blockIndex + 1 == _list.BlockCount)
                    {
                        return false;
                    }

                    _currentBlock = _list.GetBlock(++_blockIndex);
                    _elementIndex = -1;
                }

                _elementIndex++;
                return true;
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
