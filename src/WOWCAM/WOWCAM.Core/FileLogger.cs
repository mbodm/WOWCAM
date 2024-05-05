using System.Runtime.CompilerServices;
using System.Text;

namespace WOWCAM.Core
{
    public sealed class FileLogger : ILogger
    {
        private readonly object syncRoot = new();
        private readonly string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.log");
        private readonly string newLine = Environment.NewLine;

        public string Storage => logFile;

        public void ClearLog()
        {
            lock (syncRoot)
            {
                File.WriteAllText(Storage, string.Empty);
            }
        }

        public void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
            }

            lock (syncRoot)
            {
                AppendLogEntry("Message", file, line, message);
            }
        }

        public void Log(IEnumerable<string> lines, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            ArgumentNullException.ThrowIfNull(lines);

            if (!lines.Any())
            {
                throw new ArgumentNullException(nameof(lines), "Enumerable is empty.");
            }

            var message = string.Join(newLine, lines);

            lock (syncRoot)
            {
                AppendLogEntry("Message", file, line, message);
            }
        }

        public void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var message = $"Exception-Type: {exception.GetType().Name}{newLine}" + $"Exception-Message: {exception.Message}";

            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                var formattedStackTrace = exception.StackTrace.Replace(newLine, string.Empty).Replace("   at ", $"{newLine}at ");

                message += $"{newLine}Exception-StackTrace:{formattedStackTrace}";
            }

            lock (syncRoot)
            {
                AppendLogEntry("Exception", file, line, message);
            }
        }

        private void AppendLogEntry(string header, string file, int line, string message)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            file = Path.GetFileName(file);

            var text = $"[{now}] {header}{newLine}File: {file}{newLine}Line: {line}{newLine}{message}{newLine}{newLine}";

            File.AppendAllText(logFile, text, Encoding.UTF8);
        }
    }
}
