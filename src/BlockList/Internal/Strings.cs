namespace Clever.Collections.Internal
{
    internal static class Strings
    {
        public static string First_EmptyList { get; } =
            "Cannot get first item of an empty list.";

        public static string Last_EmptyList { get; } =
            "Cannot get last item of an empty list.";

        public static string MoveToBlock_NotContiguous { get; } =
            "Cannot move a non-contiguous block list.";
    }
}