using System;

namespace Clever.Collections.Internal
{
    internal static class Verify
    {
        public static void InRange(bool condition, string argumentName)
        {
            if (!condition)
            {
                throw new ArgumentOutOfRangeException(argumentName);
            }
        }

        public static void NotNull<T>(T argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void ValidState(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}