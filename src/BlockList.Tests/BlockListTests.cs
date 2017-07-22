using System;
using System.Collections.Generic;
using System.Linq;
using Clever.Collections.Tests.TestInternal;
using Xunit;

namespace Clever.Collections.Tests
{
    public class BlockListTests
    {
        private static IEnumerable<BlockListOptions> TestOptions
            => new[]
            {
                BlockList.DefaultOptions,
                BlockList.Options(initialCapacity: 1),
                BlockList.Options(initialCapacity: 32)
            };

        [Fact]
        public void Ctor_NoParams()
        {
            CheckEmptyList(new BlockList<int>());
        }

        [Theory]
        [MemberData(nameof(Ctor_Options_Data))]
        public void Ctor_Options(BlockListOptions options)
        {
            CheckEmptyList(new BlockList<int>(options));
        }

        public static IEnumerable<object[]> Ctor_Options_Data()
            => TestOptions.ToTheoryData();

        [Theory]
        [MemberData(nameof(Ctor_Enumerable_Data))]
        public void Ctor_Enumerable(IEnumerable<int> enumerable)
        {
            var list = new BlockList<int>(enumerable);
            CheckContents(list, enumerable);
        }

        [Theory]
        [MemberData(nameof(Ctor_Enumerable_Options_Data))]
        public void Ctor_Enumerable_Options(IEnumerable<int> enumerable, BlockListOptions options)
        {
            var list = new BlockList<int>(enumerable, options);
            CheckContents(list, enumerable);
        }

        public static IEnumerable<object[]> Ctor_Enumerable_Data()
            => GetTestEnumerables(BlockList.DefaultOptions).ToTheoryData();

        public static IEnumerable<object[]> Ctor_Enumerable_Options_Data()
            => TestOptions
            .SelectMany(opts => GetTestEnumerables(opts).Select(en => (en, opts)))
            .ToTheoryData();

        private static void CheckContents<T>(BlockList<T> list, IEnumerable<T> contents)
        {
            CheckCount(list, contents.Count());

            Assert.Equal(contents, list);
            Assert.Equal(contents, list.ToArray());
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

        private static IEnumerable<IEnumerable<int>> GetTestEnumerables(BlockListOptions options)
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