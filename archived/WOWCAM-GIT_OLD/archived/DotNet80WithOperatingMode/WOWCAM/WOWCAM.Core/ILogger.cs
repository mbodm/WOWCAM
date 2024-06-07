using System.Runtime.CompilerServices;

namespace WOWCAM.Core
{
    public interface ILogger
    {
        string Storage { get; } // Using such a generic term here, since this could be a file, or database, or whatever.

        void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(IEnumerable<string> multiLineMessage, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
