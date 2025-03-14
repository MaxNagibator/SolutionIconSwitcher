using System;

namespace SolutionIconSwitcher
{
    internal static class ExceptionExtensions
    {
        public static string Dump(this Exception exception)
        {
            return $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
        }
    }
}
