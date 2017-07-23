using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clever.Collections.Internal
{
    internal class EnumerableDebuggerProxy<T>
    {
        private readonly IEnumerable<T> _enumerable;

        public EnumerableDebuggerProxy(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents => _enumerable.ToArray();
    }
}
