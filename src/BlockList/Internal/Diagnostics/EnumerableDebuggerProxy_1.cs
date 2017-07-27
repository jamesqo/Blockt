using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Clever.Collections.Internal.Diagnostics
{
    internal class EnumerableDebuggerProxy<T>
    {
        private readonly IEnumerable<T> _enumerable;

        public EnumerableDebuggerProxy(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        // TODO: May have to change this to 'Z' or 'Zz'. Add explanation.
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Contents => _enumerable.ToArray();
    }
}
