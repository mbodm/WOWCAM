using System.Runtime.CompilerServices;

namespace WebViewTests.Logger
{
    public interface ILogger
    {
        string Storage { get; } // Using such a generic term here since this could be a file/database/whatever

        void ClearLog();
        void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(IEnumerable<string> lines, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
