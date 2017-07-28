using System;
using System.Collections.ObjectModel;
using Clever.Collections.Internal;

namespace Clever.Collections.Compat
{
    public static class BlockListExtensions
    {
        public static ReadOnlyCollection<T> AsReadOnly<T>(this BlockList<T> list)
        {
            Verify.NotNull(list, nameof(list));

            return new ReadOnlyCollection<T>(list);
        }

        // TODO: Implement BinarySearch() like List has.

        // TODO: What about CopyTo(int, T[], int, int)?

        // TODO: Verify.NotNull list at the top everywhere.

        // TODO: IndexOf(T, int) and IndexOf(T, int, int).

        // TODO: LastIndexOf, all overloads.

        // TODO: RemoveRange

        // TODO: Reverse, all overloads.

        // TODO: Sort will be tricky. (All overloads, again.)

        // Would TrimExcess be applicable?
    }
}