using System.Runtime.CompilerServices;

namespace WOWCAM.Core
{
    public interface ILogger
    {
        string Storage { get; } // Named it like that, since the "Log" could be a file, or a database, or whatever.

        void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(IEnumerable<string> multiLineMessage, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
        void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    }
}
