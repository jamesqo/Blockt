namespace Clever.Collections
{
    public static partial class BlockList
    {
        public static Options DefaultOptions { get; } = CreateOptions(initialCapacity: 8);

        public static Options CreateOptions(int initialCapacity) => new Options(initialCapacity);
    }
}