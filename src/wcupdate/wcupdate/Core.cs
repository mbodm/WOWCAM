namespace wcupdate
{
    internal static class Core
    {
        public static string TargetAppName => "WOWCAM";
        public static string TargetFileName => "WOWCAM.exe";

        public static void ShowError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Help);
        }

        public static string EvalFilePathArg(string filePath)
        {
            try
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(filePath);
                var absolutePath = Path.GetFullPath(expandedPath);

                return absolutePath.EndsWith(TargetFileName) ? absolutePath : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int EvalProcessIdArg(string processId)
        {
            return int.TryParse(processId, out int result) ? result : -1;
        }
    }
}
