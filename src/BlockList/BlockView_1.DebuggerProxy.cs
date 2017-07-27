using System.Collections.Generic;
using Clever.Collections.Internal.Diagnostics;

namespace Clever.Collections
{
    public partial struct BlockView<T>
    {
        private class DebuggerProxy : EnumerableDebuggerProxy<Block<T>>
        {
            public DebuggerProxy(IEnumerable<Block<T>> enumerable)
                : base(enumerable)
            {
            }
        }
    }
}