namespace Clever.Collections
{
    public static partial class BlockList
    {
        public static Options DefaultOptions { get; } =
            CreateOptions(initialCapacity: 32);

        public static Options CreateOptions(int initialCapacity)
        {
            return new Options(initialCapacity);
        }
    }
}