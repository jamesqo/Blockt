using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clever.Collections
{
    // TODO: Add XML docs. Explain why it's called SmallList.
    internal struct SmallList<T>
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

        private void MakeRoom()
        {
            Debug.Assert(_count == Capacity);

            T[] newArray;
            if (_count == 0)
            {
                newArray = new T[InitialCapacity];
            }
            else
            {
                newArray = new T[_count * 2];
                Array.Copy(_array, 0, newArray, 0, _count);
            }
            
            _array = newArray;
        }
    }
}
