using System;
using System.Diagnostics;
using Clever.Collections.Internal;

namespace Clever.Collections
{
    public static partial class BlockList
    {
        public class Options : IEquatable<Options>
        {
            internal Options(int initialCapacity)
            {
                Verify.InRange(initialCapacity > 0, nameof(initialCapacity));

                InitialCapacity = initialCapacity;
            }

            public int InitialCapacity { get; }

            public bool Equals(Options other)
            {
                return other != null
                    && InitialCapacity == other.InitialCapacity;
            }

            public override bool Equals(object obj)
                => obj is Options other && Equals(other);

            public override int GetHashCode() => throw new NotSupportedException();
        }
    }
}