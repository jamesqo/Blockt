using System.Diagnostics;

namespace Clever.Collections
{
    public partial class BlockList<T>
    {
        public struct Cursor
        {
            private readonly BlockList<T> _list;

            private Block<T> _block;
            private int _blockIndex;
            private int _elementIndex;

            public ref T Value => throw null;

            internal Cursor(BlockList<T> list, int index)
                : this(list)
            {
                Debug.Assert(list != null);
                Debug.Assert(index >= 0);

                _list = list;
                (_blockIndex, _elementIndex) = list.GetPosition(index);
            }

            public void Add(int count)
            {

            }

            public void Dec()
            {
                if (--_elementIndex < 0)
                {
                    DecRare();
                }
            }

            private void DecRare()
            {
                Debug.Assert(_elementIndex == -1);

                _block = _list.Blocks[--_blockIndex];
                _elementIndex = _block.Count - 1;
            }

            public void Inc()
            {
                if (++_elementIndex == _block.Count)
                {
                    IncRare();
                }
            }

            private void IncRare()
            {
                Debug.Assert(_elementIndex == _block.Count);

                _block = _list.Blocks[++_blockIndex];
                _elementIndex = 0;
            }

            public void Seek(int index)
            {

            }

            public void Subtract(int count)
            {

            }
        }
    }
}
