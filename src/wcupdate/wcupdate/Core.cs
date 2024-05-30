namespace wcupdate
{
    internal static class Core
    {
        public static string TargetFolder => Helper.GetApplicationFolder();
        public static string TargetFileName => "WOWCAM.exe";
        public static string TargetFilePath => Path.Combine(TargetFolder, TargetFileName);

        public static void ShowError(string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
            Console.WriteLine();
            Console.WriteLine(App.Link);
        }

        public static string EvalUpdateFolderArg(string updateFolderArg)
        {
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(updateFolderArg);

                return Path.GetFullPath(expanded);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static int EvalProcessIdArg(string processIdArg)
        {
            return int.TryParse(processIdArg, out int result) ? result : 0;
        }
    }
}
