using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SolutionIconSwitcher
{
    internal static class Logger
    {
        private const string LogFile = "SolutionIconSwitcher.log";
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "SolutionIconSwitcher", LogFile);
        private static readonly object Lock = new object();

        static Logger()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? string.Empty);
            LogDebug($"=== Сессия началась v{Assembly.GetExecutingAssembly().GetName().Version} ===");
        }

        public static void LogInfo(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("INFO", message, caller, line);
        }

        public static void LogWarning(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("WARN", message, caller, line);
        }

        public static void LogError(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
            WriteEntry("ERROR", message, caller, line);
        }

        public static void LogDebug(string message, [CallerMemberName] string caller = "", [CallerLineNumber] int line = 0)
        {
#if DEBUG
            WriteEntry("DEBUG", message, caller, line);
#endif
        }

        private static void WriteEntry(string level, string message, string caller, int line)
        {
            lock (Lock)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-5}] {caller}:{line} - {message}\n");
            }
        }
    }
}
