using System.Diagnostics;

namespace Clever.Collections
{
    public static partial class BlockList
    {
        // TODO: Position.FromBlock{Index}?
        internal struct Position
        {
            internal Position(int blockIndex, int elementIndex)
            {
                Debug.Assert(blockIndex >= 0);
                Debug.Assert(elementIndex >= 0);

                BlockIndex = blockIndex;
                ElementIndex = elementIndex;
            }

            public int BlockIndex { get; }

            public int ElementIndex { get; }

            public override string ToString() => $"[{BlockIndex}, {ElementIndex}]";
        }
    }
}