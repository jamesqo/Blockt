using System;
using System.Collections.Generic;
using System.Diagnostics;
using Clever.Collections.Internal;
using Clever.Collections.Internal.Diagnostics;

namespace Clever.Collections
{
    public partial class BlockList<T>
    {
        [DebuggerDisplay(DebuggerStrings.DisplayFormat)]
        public struct Cursor
        {
            private readonly BlockList<T> _list;

            private Block<T> _block;
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

            public Block<T> Block => _block;

            public int BlockIndex => _blockIndex;

            public int ElementIndex => _elementIndex;

            public ref T Value => ref _block[_elementIndex];

            [ExcludeFromCodeCoverage]
            private string DebuggerDisplay => $"({BlockIndex}, {ElementIndex})";

            private bool IsAtEnd
            {
                get
                {
                    bool result = _blockIndex == _list.BlockCount;
                    Debug.Assert(!result || (_block.IsEmpty && _elementIndex == 0));
                    return result;
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
                Verify.NotNull(array, nameof(array));
                Verify.InRange(arrayIndex >= 0, nameof(arrayIndex));
                Verify.InRange(count >= 0 && array.Length - arrayIndex >= count, nameof(count));

                if (count == 0)
                {
                    return;
                }

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
                // since we must also add the last item after ShiftEndRight() is called.
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

                    // Add() must run after calculating the block index, in case it affects tail.Count.
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
                Verify.ValidState(!IsAtEnd, Strings.Remove_CursorAtEnd);

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
                Verify.InRange(count >= 0, nameof(count));

                for (int i = 0; i < count; i++)
                {
                    Remove();
                }
            }

            public void Seek(int index)
            {
                Verify.InRange(index >= 0 && index <= _list.Count, nameof(index));

                if (index == _list.Count)
                {
                    SeekToEnd();
                    return;
                }

                _elementIndex = index;

                var tail = _list.Tail;
                for (int i = 0; i < tail.Count; i++)
                {
                    T[] block = tail[i];
                    if (_elementIndex < block.Length)
                    {
                        _blockIndex = i;
                        _block = _list.Blocks[_blockIndex];
                        return;
                    }
                    _elementIndex -= block.Length;
                }

                Debug.Assert(_elementIndex < _list.HeadCount);
                _blockIndex = tail.Count;
                _block = _list.Blocks[_blockIndex];
            }

            public void SeekToEnd()
            {
                _blockIndex = _list.BlockCount;
                _block = Block<T>.Empty;
                _elementIndex = 0;
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
