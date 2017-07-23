namespace Clever.Collections.Tests.TestInternal
{
    internal static class NumberExtensions
    {
        public static bool IsInteger(this double d) => d % 1 == 0;

        public static int Pow(this int @base, int exponent)
        {
            int result = 1;
            for (int i = 0; i < exponent; i++)
            {
                result *= @base;
            }
            return result;
        }
    }
}