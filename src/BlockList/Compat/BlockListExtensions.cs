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

        public static BlockList<TOutput> ConvertAll<T, TOutput>(this BlockList<T> list, Func<T, TOutput> converter)
        {
            // TODO: What should be done with the capacity here? Implement once you've decided.
        }

        // TODO: What about CopyTo(int, T[], int, int)?

        public static bool Exists<T>(this BlockList<T> list, Predicate<T> match) => list.FindIndex(match) != -1;

        public static T Find<T>(this BlockList<T> list, Predicate<T> match)
        {
            int index = list.FindIndex(match);
            return index == -1 ? default(T) : list[index];
        }

        public static BlockList<T> FindAll<T>(this BlockList<T> list, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(match, nameof(match));

            var result = new BlockList<T>();
            foreach (T item in list)
            {
                if (match(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static int FindIndex<T>(this BlockList<T> list, Predicate<T> match) => list.FindIndex(0, match);

        public static int FindIndex<T>(this BlockList<T> list, int startIndex, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));

            return list.FindIndex(startIndex, list.Count - startIndex, match);
        }

        public static int FindIndex<T>(this BlockList<T> list, int startIndex, int count, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(match, nameof(match));
            Verify.InRange(startIndex >= 0 && startIndex <= list.Count, nameof(startIndex));
            Verify.InRange(count >= 0 && list.Count - startIndex >= count, nameof(count));

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static T FindLast<T>(this BlockList<T> list, Predicate<T> match)
        {
            int index = list.FindLastIndex(match);
            return index == -1 ? default(T) : list[index];
        }

        public static int FindLastIndex<T>(this BlockList<T> list, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));

            return list.FindLastIndex(list.Count - 1, match);
        }

        public static int FindLastIndex<T>(this BlockList<T> list, int startIndex, Predicate<T> match) => list.FindLastIndex(startIndex, startIndex + 1, match);

        public static int FindLastIndex<T>(this BlockList<T> list, int startIndex, int count, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(match, nameof(match));
            Verify.InRange(
                list.IsEmpty ? startIndex == -1 : (startIndex >= 0 && startIndex < list.Count),
                nameof(startIndex));
            Verify.InRange(count >= 0 && startIndex - count + 1 >= 0, nameof(count));

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(list[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static void ForEach<T>(this BlockList<T> list, Action<T> action)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(action, nameof(action));

            foreach (T item in list)
            {
                action(item);
            }
        }

        public static BlockList<T> GetRange<T>(this BlockList<T> list, int index, int count)
        {
            Verify.NotNull(list, nameof(list));
            Verify.InRange(index >= 0, nameof(index));
            Verify.InRange(count >= 0 && list.Count - index >= count, nameof(count));
        }

        // TODO: Verify.NotNull list at the top everywhere.

        // TODO: IndexOf(T, int) and IndexOf(T, int, int).

        // TODO: LastIndexOf, all overloads.

        public static int RemoveAll<T>(this BlockList<T> list, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(match, nameof(match));

            // TODO
        }

        // TODO: RemoveRange

        // TODO: Reverse, all overloads.

        // TODO: Sort will be tricky. (All overloads, again.)

        // Would TrimExcess be applicable?

        public static bool TrueForAll<T>(this BlockList<T> list, Predicate<T> match)
        {
            Verify.NotNull(list, nameof(list));
            Verify.NotNull(match, nameof(match));

            foreach (T item in list)
            {
                if (!match(item))
                {
                    return false;
                }
            }

            return true;
        }
    }
}