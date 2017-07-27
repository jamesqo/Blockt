using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Clever.Collections
{
    public partial class BlockList<T> : IList<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly BlockList<T> _list;
            private Block<T> _currentBlock;
            private int _blockIndex;
            private int _elementIndex;

            internal Enumerator(BlockList<T> list)
                : this()
            {
                Debug.Assert(list != null);

                _list = list;
                _currentBlock = list.Blocks[0];
                _elementIndex = -1;
            }

            public T Current => _currentBlock[_elementIndex];

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

                    _currentBlock = _list.Blocks[++_blockIndex];
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
