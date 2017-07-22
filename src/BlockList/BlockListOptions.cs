using System;
using System.Diagnostics;

namespace Clever.Collections
{
    public class BlockListOptions : IEquatable<BlockListOptions>
    {
        internal BlockListOptions(int initialCapacity)
        {
            Debug.Assert(initialCapacity > 0);

            InitialCapacity = initialCapacity;
        }

        public int InitialCapacity { get; }

        public bool Equals(BlockListOptions other)
        {
            return other != null
                && InitialCapacity == other.InitialCapacity;
        }

        public override bool Equals(object obj)
            => obj is BlockListOptions other && Equals(other);

        public override int GetHashCode() => throw new NotSupportedException();
    }
}