using System;
using System.Collections;
using System.Collections.Generic;

namespace Clever.Collections
{
    public partial struct BlockView<T>
    {
        public struct Enumerator : IEnumerator<Block<T>>
        {
            private readonly BlockView<T> _view;

            private int _index;

            internal Enumerator(BlockView<T> view)
            {
                _view = view;
                _index = -1;
            }

            public Block<T> Current => _view[_index];

            public void Dispose()
            {
            }

            public bool MoveNext() => ++_index < _view.Count;

            object IEnumerator.Current => throw new NotImplementedException();

            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}