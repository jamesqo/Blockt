using System;
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

        private static IEnumerable<(IEnumerable<int>, Options)> TestEnumerablesAndOptions
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
        public void Add_AddRange(IEnumerable<int> items, Options options)
        {
            var list = new BlockList<int>(options);

            int count = 0;
            foreach (int item in items)
            {
                count++;
                list.Add(item);
                CheckContents(list, items.Take(count));
            }

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

        // TODO: Contains & others tests

        [Fact]
        public void GetEnumerator_Reset_ThrowsNotSupported()
        {
            IEnumerable enumerable = new BlockList<int>();
            Assert.Throws<NotSupportedException>(() => enumerable.GetEnumerator().Reset());
        }

        [Fact]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<int> collection = new BlockList<int>();
            Assert.False(collection.IsReadOnly);
        }

        [Fact]
        public void ICollection_Remove_ThrowsNotSupported()
        {
            ICollection<int> collection = new BlockList<int>();
            Assert.Throws<NotSupportedException>(() => collection.Remove(item: 0));
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