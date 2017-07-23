using System.Linq;

namespace Clever.Collections.Internal
{
    internal static class ArrayExtensions
    {
        public static T Last<T>(this T[] array)
        {
            Verify.NotNull(array, nameof(array));
            Verify.ValidState(array.Length > 0, Strings.Last_EmptyCollection);

            return array[array.Length - 1];
        }
    }
}