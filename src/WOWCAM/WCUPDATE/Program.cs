using System;
using System.IO;

namespace WCUPDATE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(Logic.AppTitle);
            Console.WriteLine();

            // Args

            if (args.Length == 0)
            {
                Console.WriteLine(Logic.AppDescription);
                Console.WriteLine();
                Console.WriteLine(Logic.AppUsage);
                Console.WriteLine();
                Console.WriteLine(Logic.AppUrl);
                Environment.Exit(0);
            }

            if (args.Length > 0 && args.Length < 2)
            {
                Logic.ShowError("Too few arguments.");
                Environment.Exit(666);
            }

            if (args.Length > 2)
            {
                Logic.ShowError("Too many arguments.");
                Environment.Exit(666);
            }

            var updateFolder = Logic.EvalUpdateFolderArg(args[0]);
            if (string.IsNullOrEmpty(updateFolder))
            {
                Logic.ShowError("Given argument is not a valid folder path.");
                Environment.Exit(666);
            }

            var processId = Logic.EvalProcessIdArg(args[1]);
            if (processId == 0)
            {
                Logic.ShowError("Given argument is not a valid process ID.");
                Environment.Exit(666);

            }

            // Validate

            if (!Directory.Exists(updateFolder))
            {
                Logic.ShowError("Given update folder not exists.");
                Environment.Exit(666);
            }

            var updateFile = Path.Combine(updateFolder, Logic.TargetFileName);
            if (!File.Exists(updateFile))
            {
                Logic.ShowError("Given update folder not contains an update file.");
                Environment.Exit(666);
            }

            var updateVersion = Helper.GetExeFileVersion(updateFile);
            if (updateVersion == null)
            {
                Logic.ShowError("Could not determine update file version.");
                Environment.Exit(666);
            }

            var targetVersion = Helper.GetExeFileVersion(Logic.TargetFileName);
            if (targetVersion == null)
            {
                Logic.ShowError("Could not determine target file version.");
                Environment.Exit(666);
            }

            if (updateVersion < targetVersion)
            {
                Logic.ShowError("Found update file version is older than target file version.");
                Environment.Exit(666);
            }

            if (!Logic.TargetApplicationRunning(processId))
            {
                Logic.ShowError("Target application not running.");
                Environment.Exit(666);
            }

            if (!Logic.CloseTargetApplication(processId))
            {
                Logic.ShowError("Could not close target application.");
                Environment.Exit(666);
            }

            // Replace

            Console.WriteLine($"Update version: {updateVersion.Major}.{updateVersion.Minor}.{updateVersion.Build}");
            Console.WriteLine($"Target version: {targetVersion.Major}.{targetVersion.Minor}.{targetVersion.Build}");
            Console.WriteLine();

            if (!Logic.ReplaceTargetFile(updateFile))
            {
                Logic.ShowError("Could not replace target file with update file.");
                Environment.Exit(666);
            }

            // Restart

            if (!Logic.StartTargetApplication())
            {
                Logic.ShowError("Could not start target application.");
                Environment.Exit(666);
            }

            // Finish

            Console.WriteLine("Update successful.");
            Console.WriteLine();
            Console.WriteLine("Have a nice day.");
            Environment.Exit(0);
        }
    }
}
