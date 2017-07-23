namespace Clever.Collections.Internal
{
    internal partial struct SmallList<T>
    {
        public struct Enumerator
        {
            private readonly T[] _array;
            private readonly int _count;
            private int _index;

            internal Enumerator(T[] array, int count)
            {
                _array = array;
                _count = count;
                _index = -1;
            }

            public T Current => _array[_index];

            public bool MoveNext() => ++_index < _count;
        }
    }
}
