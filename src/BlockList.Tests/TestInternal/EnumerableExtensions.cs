using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Clever.Collections.Tests.TestInternal
{
    internal static class EnumerableExtensions
    {
        public static T MaxOrDefault<T>(this IEnumerable<T> source, T defaultValue = default(T))
        {
            return source.Any() ? source.Max() : defaultValue;
        }

        public static T MinOrDefault<T>(this IEnumerable<T> source, T defaultValue = default(T))
        {
            return source.Any() ? source.Min() : defaultValue;
        }

        public static IEnumerable<object[]> ToTheoryData<T>(this IEnumerable<T> source)
        {
            return source.Select(x => new object[] { x });
        }

        public static IEnumerable<object[]> ToTheoryData<T1, T2>(this IEnumerable<(T1, T2)> source)
        {
            return source.Select(x => new object[] { x.Item1, x.Item2 });
        }
    }
}