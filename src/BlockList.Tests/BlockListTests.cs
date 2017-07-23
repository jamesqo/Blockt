﻿using System;
using System.Collections;
using System.Collections.Generic;
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
                CreateOptions(initialCapacity: 32)
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
            var blocks = list.GetBlocks();

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
        public void Contains_True(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            Assert.All(list, item =>
            {
                Assert.True(list.Contains(item));
            });
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Contains_False(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(items, options);
            var excluded = new[] { checked(list.MaxOrDefault() + 1), checked(list.MinOrDefault() - 1) };
            Assert.All(excluded, item =>
            {
                Assert.False(list.Contains(item));
            });
        }

        [Theory]
        [MemberData(nameof(TestEnumerablesAndOptions_Data))]
        public void Contains_DefaultValue_NoFalsePositive(IEnumerable<int> items, Options options)
        {
            // This is a regression test. If the BlockList isn't full, then there will be some
            // trailing default-initialized slots in the head block. Contains() must take care
            // not to search those slots for the item, since they're not part of the list's contents.
            items = items.Where(x => x != 0);
            var list = new BlockList<int>(items, options);
            Assert.False(list.Contains(0));
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
                var testCases = new[]
                {
                    (items, options, index: 0, item: excluded)
                };

                if (items.Any())
                {
                    testCases = testCases.Concat(new[]
                    {
                        (items, options, index: 1, item: excluded),
                        (items, options, index: items.Count() / 4, item: excluded),
                        (items, options, index: items.Count() / 4 + 1, item: excluded),
                        (items, options, index: items.Count() / 2, item: excluded),
                        (items, options, index: items.Count() / 2 + 1, item: excluded),
                        (items, options, index: 3 * items.Count() / 4, item: excluded),
                        (items, options, index: 3 * items.Count() / 4 + 1, item: excluded),
                        (items, options, index: items.Count() - 1, item: excluded)
                    })
                    .ToArray();
                }

                return testCases;
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

            var expected = Assert.Single(list.GetBlocks());
            var actual = list.MoveToBlock();
            Assert.Equal(expected, actual);

            CheckEmptyList(list);
        }

        [Fact]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<int> collection = new BlockList<int>();
            Assert.False(collection.IsReadOnly);
        }

        private static void CheckContents<T>(BlockList<T> list, IEnumerable<T> contents)
        {
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

            void CheckGetBlockAndGetBlocks()
            {
                var blocks = list.GetBlocks();
                int elementIndex = 0;

                for (int i = 0; i < list.BlockCount; i++)
                {
                    var block = blocks[i];
                    Assert.Equal(block, list.GetBlock(i));

                    Assert.Equal(contents.Skip(elementIndex).Take(block.Count), block);
                    elementIndex += block.Count;
                }
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

            CheckCopyTo();
            CheckExplicitGetEnumerator();
            CheckGetBlockAndGetBlocks();
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

            var emptyBlock = list.GetBlock(0);
            Assert.Empty(emptyBlock);
            Assert.Single(list.GetBlocks(), emptyBlock);
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
    }
}