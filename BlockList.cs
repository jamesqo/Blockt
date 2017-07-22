using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clever.Collections
{
    public class BlockList<T> : ICollection<T>
    {
        private const int InitialCapacity = 4;

        // This is a mutable struct field; do not make it readonly.
        private ValueList<T[]> _blocks;
        private T[] _current;
        private int _index;
        private int _count;
        
        public BlockList()
        {
            _current = Array.Empty<T>();
        }

        public BlockList(ICollection<T> collection)
        {
            Initialize(collection);
        }

        public BlockList(IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection)
            {
                Initialize(collection);
                return;
            }
            
            AddRange(enumerable);
        }

        public int BlockCount => _blocks.Count + 1;

        public int Count => _count;

        private bool AtEndOfCurrentBlock => _index == _current.Length;

        private int CurrentBlockCapacity => _current.Length;

        public void Add(T item)
        {
            if (AtEndOfCurrentBlock)
            {
                MakeRoom();
            }

            _current[_index++] = item;
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            {
                Add(item);
            }
        }

        public void Clear()
        {
            _blocks.Clear();
            _current = Array.Empty<T>();
            _index = 0;
            _count = 0;
        }

        public bool Contains(T item)
        {
            foreach (T[] block in _blocks)
            {
                if (Array.IndexOf(block, item) >= 0)
                {
                    return true;
                }
            }

            return Array.IndexOf(_current, item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (T[] block in _blocks)
            {
                Array.Copy(block, 0, array, arrayIndex, block.Length);
                arrayIndex += block.Length;
            }

            Array.Copy(_current, 0, array, arrayIndex, _index);
        }

        public T[] GetBlock(int index)
        {
            if (index < _blocks.Count)
            {
                return _blocks[index];
            }

            Debug.Assert(index == _blocks.Count);
            return _current;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public T[] ToArray()
        {
            if (BlockCount == 1 && AtEndOfCurrentBlock)
            {
                return _current;
            }

            Debug.Assert(_count > 0);
            var array = new T[_count];
            CopyTo(array, 0);
            return array;
        }

        private void Initialize(ICollection<T> collection)
        {
            _count = collection.Count;
            _current = new T[_count];
            collection.CopyTo(_current, 0);
        }

        private void MakeRoom()
        {
            Debug.Assert(AtEndOfCurrentBlock);

            _blocks.Add(_current);
            int nextCapacity = Math.Max(CurrentBlockCapacity * 2, InitialCapacity);
            _current = new T[nextCapacity];
            _index = 0;
        }

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly BlockList<T> _list;
            private T[] _currentBlock;
            private int _blockIndex;
            private int _elementIndex;

            internal Enumerator(BlockList<T> list) : this()
            {
                _list = list;
                _currentBlock = list.GetBlock(0);
                _elementIndex = -1;
            }

            public T Current => _currentBlock[_elementIndex];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_elementIndex + 1 == _currentBlock.Length)
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
