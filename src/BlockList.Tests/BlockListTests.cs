using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Clever.Collections.Tests.TestInternal;
using Xunit;
using static Clever.Collections.BlockList;

namespace Clever.Collections.Tests
{
    public class BlockListTests
    {
        private static IEnumerable<Options> TestOptions
            => new[]
            {
                DefaultOptions,
                CreateOptions(initialCapacity: 1),
                CreateOptions(initialCapacity: 10),
                CreateOptions(initialCapacity: 13),
                CreateOptions(initialCapacity: 17)
            };

        public static IEnumerable<object[]> TestOptions_Data()
            => TestOptions.ToTheoryData();

        private static IEnumerable<(IEnumerable<int> items, Options options)> TestEnumerablesAndOptions
            => TestOptions.SelectMany(
                opts => GetTestEnumerables(opts).Select(
                    items => (items, opts)));

        public static IEnumerable<object[]> TestEnumerablesAndOptions_Data()
            => TestEnumerablesAndOptions.ToTheoryData();

        [Fact]
        public void Ctor_NoParams()
        {
            var list = new BlockList<int>();
            CheckEmptyList(new BlockList<int>());
            CheckOptions(list, DefaultOptions);
        }

        [Theory]
        [MemberData(nameof(TestOptions_Data))]
        public void Ctor_Options(Options options)
        {
            var list = new BlockList<int>(options);
            CheckEmptyList(list);
            CheckOptions(list, options);
        }

        [Theory]
        [MemberData(nameof(Ctor_Enumerable_Data))]
        public void Ctor_Enumerable(IEnumerable<int> items)
        {
            var list = new BlockList<int>(items);
            CheckContents(list, items);
            CheckOptions(list, DefaultOptions);
        }

        public static IEnumerable<object[]> Ctor_Enumerable_Data()
            => GetTestEnumerables(DefaultOptions).ToTheoryData();

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Ctor_Enumerable_Options(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            CheckContents(list, items);
            CheckOptions(list, options);
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void IsContiguous(IEnumerable<int> items, Options options)
        {
            bool expected = items.Count() <= options.InitialCapacity;
            var list = new BlockList<int>(items, options);
            Assert.Equal(expected, list.IsContiguous);
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void IsFull(IEnumerable<int> items, Options options)
        {
            // The list is full when there are no elements (the head block is a cached empty array) or
            // the count is exactly n * 2^i, where n is the initial capacity and i is an integer.
            bool expected =
                !items.Any() ||
                Math.Log((double)items.Count() / options.InitialCapacity, 2).IsInteger();

            var list = new BlockList<int>(items, options);
            Assert.Equal(expected, list.IsFull);
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Indexer(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            var array = items.ToArray();
            int excluded = checked(list.MaxOrDefault() + 1);

            for (int i = 0; i < list.Count; i++)
            {
                Assert.Equal(array[i], list[i]);
                list[i] = excluded;
                Assert.Equal(excluded, list[i]);
                list[i] = array[i];
                Assert.Equal(array[i], list[i]);
            }
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Add_AddRange(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(options);

            foreach (int item in items)
            {
                list.Add(item);
            }
            CheckContents(list, items);

            list.Clear();
            list.AddRange(items);
            CheckContents(list, items);
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Clear(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            var blocks = list.Blocks.ToArray();

            list.Clear();

            // Not only should clearing the BlockList free the references to each
            // of its blocks, it should clear each block to free references the
            // blocks themselves may hold.
            Assert.All(blocks, block =>
            {
                var defaultValues = Enumerable.Repeat(0, block.Count);
                Assert.Equal(defaultValues, block);
            });

            CheckEmptyList(list);
            // Clearing the BlockList should preserve the options it was created with.
            CheckOptions(list, options);
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Contains_IndexOf_Found(IEnumerable<int> items, Options options)
        {
            Debug.Assert(!items.ContainsDuplicates());

            var list = new BlockList<int>(items, options);

            int index = 0;
            foreach (int item in items)
            {
                Assert.True(list.Contains(item));
                Assert.Equal(index++, list.IndexOf(item));
            }
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Contains_IndexOf_NotFound(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            var excluded = new[] { checked(list.MaxOrDefault() + 1), checked(list.MinOrDefault() - 1) };
            Assert.All(excluded, item =>
            {
                Assert.False(list.Contains(item));
                Assert.Equal(-1, list.IndexOf(item));
            });
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Contains_IndexOf_DefaultValue_NoFalsePositive(IEnumerable<int> items, Options options)
        {
            // This is a regression test. If the BlockList isn't full, then there will be some trailing
            // default-initialized slots in the head block. Contains() and IndexOf() must take care not
            // to search those slots for the item, since they're not part of the list's contents.
            items = items.Where(x => x != 0);
            var list = new BlockList<int>(items, options);

            Assert.False(list.Contains(0));
            Assert.Equal(-1, list.IndexOf(0));
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void First(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            if (list.IsEmpty)
            {
                return;
            }

            Assert.Equal(Enumerable.First(list), list.First());
        }

        [Fact]
        public void First_Empty_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => new BlockList<int>().First());
        }

        [Fact]
        public void GetEnumerator_Reset_ThrowsNotSupported()
        {
            IEnumerable enumerable = new BlockList<int>();
            Assert.Throws<NotSupportedException>(() => enumerable.GetEnumerator().Reset());
        }

        [Theory]
        [MemberData(nameof(Insert_Data))]
        public void Insert(IEnumerable<int> items, Options options, int index, int item)
        {
            var expected = items.Take(index).Append(item).Concat(items.Skip(index));
            var list = new BlockList<int>(items, options);
            list.Insert(index, item);
            Assert.Equal(expected, list);
        }

        public static IEnumerable<object[]> Insert_Data()
            => TestEnumerablesAndOptions.SelectMany(x =>
            {
                var (items, options) = x;
                int excluded = checked(items.MaxOrDefault() + 1);
                return GetTestIndices(items.Count(), exclusive: false).Select(
                    index => (items, options, index, excluded));
            })
            .ToTheoryData();

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Last(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            if (list.IsEmpty)
            {
                return;
            }

            Assert.Equal(Enumerable.Last(list), list.Last());
        }

        [Fact]
        public void Last_Empty_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => new BlockList<int>().Last());
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void MoveToBlock(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            if (!list.IsContiguous)
            {
                return;
            }

            var expected = Assert.Single(list.Blocks);
            var actual = list.MoveToBlock();
            Assert.Equal(expected, actual);

            CheckEmptyList(list);
        }

        [Theory]
        [MemberData(nameof(RemoveAt_Data))]
        public void RemoveAt(IEnumerable<int> items, Options options, int index)
        {
            var expected = items.Take(index).Concat(items.Skip(index + 1));
            var list = new BlockList<int>(items, options);
            list.RemoveAt(index);
            Assert.Equal(expected, list);
        }

        public static IEnumerable<object[]> RemoveAt_Data()
            => TestEnumerablesAndOptions.SelectMany(
                x => GetTestIndices(x.items.Count(), exclusive: true).Select(
                    index => (x.items, x.options, index)))
            .ToTheoryData();

        [Fact]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<int> collection = new BlockList<int>();
            Assert.False(collection.IsReadOnly);
        }

        [Theory]
        [MemberData(nameof(Options_Equals_Data))]
        public void Options_Equals(Options opts, object obj, bool expected)
        {
            Debug.Assert(opts != null && obj != null);

            if (obj is Options other)
            {
                Assert.Equal(expected, opts.Equals(other));
                Assert.Equal(expected, other.Equals(opts));
            }

            Assert.Equal(expected, opts.Equals(obj));
            Assert.Equal(expected, obj.Equals(opts));
        }

        public static IEnumerable<object[]> Options_Equals_Data()
            => TestOptions.SelectMany((outerOpts, outerIndex) =>
            {
                // Case 1: The comparand is also an Options, and Equals() might be true.
                var testCases = TestOptions.Select(
                    (innerOpts, innerIndex) => (outerOpts, (object)innerOpts, expected: outerIndex == innerIndex));
                // Case 2: The comparand is some other type, and Equals() is never true.
                return testCases.Concat(new[]
                {
                    (outerOpts, "text", expected: false),
                    (outerOpts, new object(), expected: false),
                    (outerOpts, 42, expected: false)
                });
            })
            .ToTheoryData();

        [Theory]
        [MemberData(nameof(TestOptions_Data))]
        public void Options_Equals_Null_ReturnsFalse(Options opts)
        {
            Assert.False(opts.Equals(null));
            Assert.False(opts.Equals((object)null));
        }

        [Fact]
        public void Options_ImplementsIEquatable()
        {
            Assert.IsAssignableFrom<IEquatable<Options>>(DefaultOptions);
        }

        private static void CheckContents<T>(BlockList<T> list, IEnumerable<T> contents)
        {
            void CheckBlocks()
            {
                int elementIndex = 0;

                for (int i = 0; i < list.BlockCount; i++)
                {
                    var block = list.Blocks[i];
                    Assert.Equal(contents.Skip(elementIndex).Take(block.Count), block);
                    elementIndex += block.Count;
                }
            }

            void CheckCopyTo()
            {
                var buffer = new T[list.Count];
                list.CopyTo(buffer, 0);
                Assert.Equal(contents, buffer);
            }

            void CheckExplicitGetEnumerator()
            {
                Assert.Equal(contents, list);
                Assert.Equal(contents, (IEnumerable)list);
            }

            void CheckGetEnumerator()
            {
                var buffer = new List<T>();
                foreach (T item in list)
                {
                    buffer.Add(item);
                }
                Assert.Equal(contents, buffer);
            }
            
            void CheckToArray() => Assert.Equal(contents, list.ToArray());

            CheckCount(list, contents.Count());

            CheckBlocks();
            CheckCopyTo();
            CheckExplicitGetEnumerator();
            CheckGetEnumerator();
            CheckToArray();
        }

        private static void CheckCount<T>(BlockList<T> list, int count)
        {
            var opts = list.Options;
            int initialCapacity = opts.InitialCapacity;

            int ExpectedBlockCount()
            {
                int blockCount = 1;
                for (int i = initialCapacity; i < count; i *= 2)
                {
                    blockCount++;
                }
                return blockCount;
            }

            int ExpectedCapacity()
            {
                if (count == 0)
                {
                    return 0;
                }

                int i = initialCapacity;
                while (i < count)
                {
                    i *= 2;
                }
                return i;
            }

            Assert.Equal(count, list.Count);
            Assert.Equal(ExpectedBlockCount(), list.BlockCount);
            Assert.Equal(ExpectedCapacity(), list.Capacity);
        }

        private static void CheckEmptyList<T>(BlockList<T> list)
        {
            CheckContents(list, Array.Empty<T>());

            var emptyBlock = list.Blocks[0];
            Assert.Empty(emptyBlock);
            Assert.Single(list.Blocks, emptyBlock);
        }

        private static void CheckOptions<T>(BlockList<T> list, Options options)
        {
            Assert.Same(options, list.Options);
        }

        private static IEnumerable<IEnumerable<int>> GetTestEnumerables(Options options)
        {
            IEnumerable<int> CreateEnumerable(int count)
            {
                return Enumerable.Range(0, Math.Max(0, count));
            }

            yield return new int[0];

            for (int i = 0; i <= 5; i++)
            {
                // This is a size at which the block list will be completely filled up: count == capacity.
                // If one more element is added, a resize will be necessary.
                int filledCount = options.InitialCapacity * 2.Pow(i);

                yield return CreateEnumerable(filledCount - 10);
                yield return CreateEnumerable(filledCount - 1);
                yield return CreateEnumerable(filledCount);
                yield return CreateEnumerable(filledCount + 1);
                yield return CreateEnumerable(filledCount + 10);
            }
        }

        private static IEnumerable<int> GetTestIndices(int listCount, bool exclusive)
        {
            Debug.Assert(listCount >= 0);

            var indices = new[]
            {
                0,
                1,
                listCount / 4,
                listCount / 4 + 1,
                listCount / 2,
                listCount / 2 + 1,
                3 * listCount / 4,
                3 * listCount / 4 + 1,
                listCount - 1,
                listCount
            };

            return indices.Where(
                index => index >= 0 && (exclusive ? index < listCount : index <= listCount));
        }
    }
}