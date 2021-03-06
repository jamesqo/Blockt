﻿namespace Clever.Collections.Internal
{
    internal static class Strings
    {
        public static string First_EmptyCollection { get; } =
            "Cannot get first item of an empty collection.";

        public static string Last_EmptyCollection { get; } =
            "Cannot get last item of an empty collection.";

        public static string MoveToBlock_NotContiguous { get; } =
            "Cannot move a non-contiguous block list.";
    }
}