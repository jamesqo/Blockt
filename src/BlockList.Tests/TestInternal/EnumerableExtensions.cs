using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clever.Collections.Tests.TestInternal
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<object[]> ToTheoryData<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.Select(x => new object[] { x });
        }

        public static IEnumerable<object[]> ToTheoryData<T1, T2>(this IEnumerable<(T1, T2)> enumerable)
        {
            return enumerable.Select(x => new object[] { x.Item1, x.Item2 });
        }
    }
}