using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blockt
{
    public struct ValueList<T> : IList<T>
    {
        private const int InitialCapacity = 4;

        private T[] _array;
        private int _count;

        public int Capacity => _array?.Length ?? 0;

        public int Count => _count;

        public T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _count);
                return _array[index];
            }
            set
            {
                Debug.Assert(index >= 0 && index < _count);
                _array[index] = value;
            }
        }

        public void Add(T item)
        {
            if (_count == Capacity)
            {
                MakeRoom();
            }

            _array[_count++] = item;
        }

        public void Clear()
        {
            if (_array != null)
            {
                Array.Clear(_array, 0, _count);
                _count = 0;
            }
        }

        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (_array != null)
            {
                Array.Copy(_array, 0, array, arrayIndex, _count);
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public int IndexOf(T item)
            => _array == null ? -1 : Array.IndexOf(_array, item, 0, _count);

        public void Insert(int index, T item)
        {
            if (_count == Capacity)
            {
                MakeRoom();
            }

            if (index < _count)
            {
                Array.Copy(_array, index, _array, index + 1, _count - index);
            }

            _array[index] = item;
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        private void MakeRoom()
        {
            Debug.Assert(_count == Capacity);

            int newCapacity = _count == 0 ? InitialCapacity : _count * 2;
            var newArray = new T[newCapacity];
            Array.Copy(_array, 0, newArray, 0, _count);
            _array = newArray;
        }

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection<T>.Remove(T item) => throw new NotImplementedException();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private int _index;

            public Enumerator(ValueList<T> list)
            {
                _array = list._array;
                _count = list._count;
                _index = -1;
            }

            public T Current => _array[_index];

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                int nextIndex = _index + 1;
                if (nextIndex < _count)
                {
                    _index = nextIndex;
                    return true;
                }

                return false;
            }

            object IEnumerator.Current => Current;

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
