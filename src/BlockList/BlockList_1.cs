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

        internal T[] Head => _head;

        internal int HeadCount => _headCount;

        internal Block<T> HeadSpan => new Block<T>(_head, _headCount);

        internal SmallList<T[]> Tail => _tail;

        [ExcludeFromCodeCoverage]
        private string DebuggerDisplay => $"{nameof(Count)} = {Count}, {nameof(HeadCount)} = {HeadCount}, {nameof(HeadCapacity)} = {HeadCapacity}";

        private int HeadCapacity => _head.Length;

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
            GetCursor(index).CopyTo(array, arrayIndex, count);
        }

        public T First()
        {
            Verify.ValidState(!IsEmpty, Strings.First_EmptyCollection);

            return Blocks[0][0];
        }

        public Cursor GetCursor(int index) => new Cursor(this, index);

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

            GetCursor(index).Insert(item);
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            Verify.NotNull(items, nameof(items));
            Verify.InRange(index >= 0 && index <= Count, nameof(index));

            GetCursor(index).InsertRange(items);
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

            GetCursor(index).Remove();
        }

        public void RemoveRange(int index, int count)
        {
            Verify.InRange(index >= 0, nameof(index));
            Verify.InRange(count >= 0 && Count - index >= count, nameof(count));

            GetCursor(index).RemoveRange(count);
        }

        public void Reverse() => Reverse(0, _count);

        public void Reverse(int index, int count)
        {
            Verify.InRange(index >= 0, nameof(index));
            Verify.InRange(count >= 0 && _count - index >= count, nameof(count));

            var i = GetCursor(index);
            var j = GetCursor(index + count - 1);
            while (i < j)
            {
                T temp = i.Value;
                i.Value = j.Value;
                j.Value = temp;
                i.Inc();
                j.Dec();
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
