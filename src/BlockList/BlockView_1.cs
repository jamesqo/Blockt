using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Clever.Collections.Internal;
using Clever.Collections.Internal.Diagnostics;

namespace Clever.Collections
{
    [DebuggerDisplay(DebuggerStrings.DisplayFormat)]
    [DebuggerTypeProxy(typeof(BlockView<>.DebuggerProxy))]
    public partial struct BlockView<T> : IList<Block<T>>, IReadOnlyList<Block<T>>
    {
        private readonly BlockList<T> _list;

        internal BlockView(BlockList<T> list)
        {
            Debug.Assert(list != null);

            _list = list;
        }

        public int Count => _list.BlockCount;

        private string DebuggerDisplay => $"{nameof(Count)} = {Count}";

        public Block<T> this[int index]
        {
            get
            {
                Verify.InRange(index >= 0 && index < Count, nameof(index));

                var tail = _list.Tail;
                if (index < tail.Count)
                {
                    return new Block<T>(tail[index]);
                }

                Debug.Assert(index == tail.Count);
                return _list.HeadSpan;
            }
        }

        public bool Contains(Block<T> item) => IndexOf(item) != -1;

        public void CopyTo(Block<T>[] array, int arrayIndex)
        {
            Verify.NotNull(array, nameof(array));
            Verify.InRange(arrayIndex >= 0 && array.Length - arrayIndex >= Count, nameof(arrayIndex));

            for (int i = 0; i < Count; i++)
            {
                array[arrayIndex++] = this[i];
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public int IndexOf(Block<T> item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i] == item)
                {
                    return i;
                }
            }

            return -1;
        }

        public Block<T>[] ToArray()
        {
            var blocks = new Block<T>[Count];
            CopyTo(blocks, 0);
            return blocks;
        }

        Block<T> IList<Block<T>>.this[int index]
        {
            get => this[index];
            set => throw new NotSupportedException();
        }

        void IList<Block<T>>.Insert(int index, Block<T> item) => throw new NotSupportedException();

        void IList<Block<T>>.RemoveAt(int index) => throw new NotSupportedException();

        bool ICollection<Block<T>>.IsReadOnly => true;

        void ICollection<Block<T>>.Add(Block<T> item) => throw new NotSupportedException();

        void ICollection<Block<T>>.Clear() => throw new NotSupportedException();

        bool ICollection<Block<T>>.Remove(Block<T> item) => throw new NotSupportedException();

        IEnumerator<Block<T>> IEnumerable<Block<T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}