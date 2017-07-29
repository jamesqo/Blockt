using System;
using System.Collections.Generic;
using System.Diagnostics;
using Clever.Collections.Internal;

namespace Clever.Collections
{
    public partial class BlockList<T>
    {
        // TODO: Debugger attributes
        public struct Cursor
        {
            // end state if logical ind == count, or blockind == blockcnt?
            // We don't want to keep arnd. the logical ind, perf overhead for Inc()
            // ++_elementIndex >= cnt.
            // end state: _blockIndex == _tail.Count, _block is empty, _elementIndex = 0
            // Why not _elementIndex == -1? Since we consider new BlockList<>().Start
            // Advantages of 0: less special-casing
            // Advantages of -1: no >=, easy to check if end.
            // Decision: 0

            private readonly BlockList<T> _list;

            private Block<T> _block; // TODO: Expose property?
            private int _blockIndex;
            private int _elementIndex;

            internal Cursor(BlockList<T> list, int index)
                : this()
            {
                Debug.Assert(list != null);
                Debug.Assert(index >= 0);

                _list = list;
                Seek(index);
            }

            public int BlockIndex => _blockIndex;

            public int ElementIndex => _elementIndex;

            // TODO: Remove note?
            // One advantage of returning a ref instead of get/set prop is since the compiler won't error when
            // one tries to do GetCursor(i).Value = foo.
            public ref T Value => ref _block[_elementIndex];

            private bool IsAtEnd
            {
                get
                {
                    Debug.Assert(_blockIndex != _list.BlockCount || (_block.IsEmpty && _elementIndex == 0));
                    return _blockIndex == _list.BlockCount;
                }
            }

            public void Add(int count)
            {
                Verify.InRange(count >= 0, nameof(count));

                for (int i = 0; i < count; i++)
                {
                    Inc();
                }
            }

            public void CopyTo(T[] array, int arrayIndex, int count)
            {
                // TODO: Validation

                _list.CopyBlockEnd(_blockIndex, _elementIndex, array, ref arrayIndex, ref count);

                var tail = _list.Tail;
                for (int blockIndex = _blockIndex + 1; blockIndex <= tail.Count; blockIndex++)
                {
                    if (count == 0)
                    {
                        return;
                    }
                    _list.CopyBlock(blockIndex, array, ref arrayIndex, ref count);
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
                if (IsAtEnd)
                {
                    _list.Add(item);
                    return;
                }

                Debug.Assert(!_list.IsEmpty);
                T last = _list.Last();

                // We have to special-case when the insert position is in the head block,
                // since we must also add the last item after ShiftEnd() is called.
                var tail = _list.Tail;
                if (_blockIndex == tail.Count)
                {
                    _list.ShiftEndRight(tail.Count, _elementIndex);
                    // This must run first in case Head changes during Add.
                    _list.Head[_elementIndex] = item;
                    _list.Add(last);
                    return;
                }

                _list.ShiftRight(tail.Count);

                {
                    int blockIndex = tail.Count - 1;
                    // Since the insert position wasn't in the head block, it must be in a block
                    // preceding the head block, and that means there are multiple blocks.
                    Debug.Assert(blockIndex >= 0);

                    // Add() must run after calculating blockIndex, in case it affects _tail.Count.
                    _list.Add(last);

                    while (true)
                    {
                        _list.ShiftLastRight(blockIndex);
                        if (blockIndex == _blockIndex)
                        {
                            break;
                        }
                        _list.ShiftRight(blockIndex);
                        blockIndex--;
                    }

                    _list.ShiftEndRight(_blockIndex, _elementIndex);
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
                _list.ShiftEndLeft(_blockIndex, _elementIndex);

                var tail = _list.Tail;
                for (int blockIndex = _blockIndex + 1; blockIndex <= tail.Count; blockIndex++)
                {
                    _list.ShiftFirstLeft(blockIndex);
                    _list.ShiftLeft(blockIndex);
                }

                _list.RemoveLast();
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
                Verify.InRange(count >= 0, nameof(count));

                for (int i = 0; i < count; i++)
                {
                    Dec();
                }
            }
        }

        private void CopyBlock(int blockIndex, T[] array, ref int arrayIndex, ref int count)
        {
            CopyBlockEnd(blockIndex, 0, array, ref arrayIndex, ref count);
        }

        private void CopyBlockEnd(int blockIndex, int elementIndex, T[] array, ref int arrayIndex, ref int count)
        {
            var block = Blocks[blockIndex];
            int copyCount = Math.Min(count, block.Count);
            Array.Copy(block.Array, elementIndex, array, arrayIndex, copyCount);

            arrayIndex += copyCount;
            count -= copyCount;
        }

        private void RemoveLast()
        {
            Debug.Assert(!IsEmpty);

            _count--;
            _head[--_headCount] = default(T);

            // To maintain invariants, we need to make sure the head block isn't empty unless
            // the entire block list is empty.
            if (_headCount == 0)
            {
                if (_tail.IsEmpty)
                {
                    // The entire block list is empty, so revert to the initial state.
                    Reset();
                }
                else
                {
                    // Throw away the current head block and pretend we've just finished filling the last block.
                    _capacity -= HeadCapacity;
                    _head = _tail.RemoveLast();
                    _headCount = _head.Length;
                }
                Debug.Assert(IsFull);
            }
        }

        private void ShiftEndLeft(int blockIndex, int elementIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, elementIndex + 1, block.Array, elementIndex, block.Count - elementIndex - 1);
        }

        private void ShiftEndRight(int blockIndex, int elementIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, elementIndex, block.Array, elementIndex + 1, block.Count - elementIndex - 1);
        }

        private void ShiftFirstLeft(int blockIndex)
        {
            Debug.Assert(blockIndex > 0);

            var block = Blocks[blockIndex];
            var predecessor = _tail[blockIndex - 1];
            Debug.Assert(!block.IsEmpty && predecessor.Length > 0);

            predecessor[predecessor.Length - 1] = block.First();
        }

        private void ShiftLastRight(int blockIndex)
        {
            Debug.Assert(blockIndex < _tail.Count);

            T[] block = _tail[blockIndex];
            var successor = Blocks[blockIndex + 1];
            Debug.Assert(block.Length > 0 && !successor.IsEmpty);

            successor[0] = block.Last();
        }

        private void ShiftLeft(int blockIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, 1, block.Array, 0, block.Count - 1);
        }

        private void ShiftRight(int blockIndex)
        {
            var block = Blocks[blockIndex];
            Array.Copy(block.Array, 0, block.Array, 1, block.Count - 1);
        }
    }
}
