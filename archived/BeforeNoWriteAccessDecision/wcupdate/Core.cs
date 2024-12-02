using System.Text;

namespace wcupdate
{
    internal static class Core
    {
        public const string TargetAppName = "WOWCAM";
        public const string TargetFileName = $"{TargetAppName}.exe";

        public static void Print(string msg)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");
        }

        public static void PrintError(string msg)
        {
            Print($"Error: {msg}");
            Print("Stopped");
        }

        public static void PrintException(Exception e)
        {
            Print("Error: An exception occurred.");
            Print($"-> Exception-Type: {e.GetType()}");
            Print($"-> Exception-Message: {e.Message}");
            Print("Stopped");
        }

        public static bool Step(Func<bool> predicate, string successMessage, string errorMessage)
        {
            var result = predicate();

            if (result) Print(successMessage);
            else PrintError(errorMessage);

            return result;
        }

        public static string DecodeBase64(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
