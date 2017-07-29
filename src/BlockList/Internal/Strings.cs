namespace Clever.Collections.Internal
{
    internal static class Strings
    {
        public static string First_EmptyCollection { get; } =
            "Cannot get first item of an empty collection.";

        public static string Last_EmptyCollection { get; } =
            "Cannot get last item of an empty collection.";

        public static string MoveToBlock_NotContiguous { get; } =
            "Cannot move a non-contiguous block list.";

        public static string Remove_CursorAtEnd { get; } =
            "Cannot remove an item when the cursor is at the end of the block list.";
    }
}