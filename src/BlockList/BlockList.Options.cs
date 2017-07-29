using Clever.Collections.Internal;

namespace Clever.Collections
{
    public static partial class BlockList
    {
        public class Options
        {
            internal Options(int initialCapacity)
            {
                Verify.InRange(initialCapacity > 0, nameof(initialCapacity));

                InitialCapacity = initialCapacity;
            }

            public int InitialCapacity { get; }
        }
    }
}