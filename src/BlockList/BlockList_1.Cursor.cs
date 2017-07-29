using System.Collections.Generic;
using System.Diagnostics;

namespace Clever.Collections
{
    public partial class BlockList<T>
    {
        // TODO: Debugger attributes
        public struct Cursor
        {
            private readonly BlockList<T> _list;

            private Block<T> _block;
            private int _blockIndex;
            private int _elementIndex;

            // TODO: Remove note?
            // One advantage of returning a ref instead of get/set prop is since the compiler won't error when
            // one tries to do GetCursor(i).Value = foo.
            public ref T Value => throw null;

            internal Cursor(BlockList<T> list, int index)
                : this()
            {
                Debug.Assert(list != null);
                Debug.Assert(index >= 0);

                _list = list;
                Seek(index);
            }

            public void Add(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Inc();
                }
            }

            public void CopyTo(T[] array, int arrayIndex, int count)
            {
                CopyBlockEnd(_blockIndex, _elementIndex, array, ref arrayIndex, ref count);

                var tail = _list.Tail;
                for (int blockIndex = _blockIndex + 1; blockIndex <= tail.Count; blockIndex++)
                {
                    if (count == 0)
                    {
                        return;
                    }
                    CopyBlock(blockIndex, array, ref arrayIndex, ref count);
                }
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

            public void Insert(T item)
            {
                if (index == _list.Count)
                {
                    Add(item);
                    return;
                }

                Debug.Assert(!_list.IsEmpty);
                T last = _list.Last();

                // We have to special-case when the insert position is in the head block,
                // since we must also add the last item after ShiftEnd() is called.
                var tail = _list.Tail;
                if (_blockIndex == tail.Count)
                {
                    ShiftEndRight(tail.Count, _elementIndex);
                    // This must run first in case Head changes during Add.
                    _list.Head[_elementIndex] = item;
                    _list.Add(last);
                    return;
                }

                ShiftRight(tail.Count);

                {
                    int blockIndex = tail.Count - 1;
                    // Since the insert position wasn't in the head block, it must be in a block
                    // preceding the head block, and that means there are multiple blocks.
                    Debug.Assert(blockIndex >= 0);

                    // Add() must run after calculating blockIndex, in case it affects _tail.Count.
                    _list.Add(last);

                    while (true)
                    {
                        ShiftLastRight(blockIndex);
                        if (blockIndex == _blockIndex)
                        {
                            break;
                        }
                        ShiftRight(blockIndex);
                        blockIndex--;
                    }

                    ShiftEndRight(_blockIndex, _elementIndex);
                    tail[_blockIndex][_elementIndex] = item;
                }
            }

            public void InsertRange(IEnumerable<T> items)
            {
                foreach (T item in items)
                {
                    Insert(item);
                    Inc();
                }
            }

            public void Remove()
            {
                if (index < _count - 1)
                {
                    var removePos = GetPosition(index);
                    ShiftEndLeft(removePos.BlockIndex, removePos.ElementIndex);

                    for (int blockIndex = removePos.BlockIndex + 1; blockIndex <= _tail.Count; blockIndex++)
                    {
                        ShiftFirstLeft(blockIndex);
                        ShiftLeft(blockIndex);
                    }
                }

                RemoveLast();
            }

            public void RemoveRange(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Remove();
                }
            }

            public void Seek(int index)
            {
                // TODO: Change to Verify. Also, what if index == Count?
                Debug.Assert(index >= 0 && index < _list.Count);

                _elementIndex = index;

                var tail = _list.Tail;
                for (int i = 0; i < tail.Count; i++)
                {
                    T[] block = tail[i];
                    if (_elementIndex < block.Length)
                    {
                        _blockIndex = i;
                        return;
                    }
                    _elementIndex -= block.Length;
                }

                // TODO: <=
                Debug.Assert(_elementIndex < _list.HeadCount);
                _blockIndex = tail.Count;
            }

            public void Subtract(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    Dec();
                }
            }
        }
    }
}
