using System;
using System.IO;
using System.Threading.Tasks;

namespace WCUPDATE
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine(App.Title);
            Console.WriteLine();

            // Args

            if (args.Length == 0)
            {
                Console.WriteLine(App.Description);
                Console.WriteLine();
                Console.WriteLine(App.Usage);
                Console.WriteLine();
                Console.WriteLine(App.Link);
                Environment.Exit(0);
            }

            if (args.Length > 0 && args.Length < 2)
            {
                Core.ShowError("Too few arguments.");
                Environment.Exit(666);
            }

            if (args.Length > 2)
            {
                Core.ShowError("Too many arguments.");
                Environment.Exit(666);
            }

            var updateFolder = Core.EvalUpdateFolderArg(args[0]);
            if (string.IsNullOrEmpty(updateFolder))
            {
                Core.ShowError("Given argument is not a valid folder path.");
                Environment.Exit(666);
            }

            var processId = Core.EvalProcessIdArg(args[1]);
            if (processId == 0)
            {
                Core.ShowError("Given argument is not a valid process ID.");
                Environment.Exit(666);
            }

            // Validate

            if (!Directory.Exists(updateFolder))
            {
                Core.ShowError("Given update folder not exists.");
                Environment.Exit(666);
            }

            var updateFile = Path.Combine(updateFolder, Core.TargetFileName);
            if (!File.Exists(updateFile))
            {
                Core.ShowError("Given update folder not contains an update file.");
                Environment.Exit(666);
            }

            var updateVersion = Helper.GetExeFileVersion(updateFile);
            if (updateVersion.Major < 1)
            {
                Core.ShowError("Could not determine update file version.");
                Environment.Exit(666);
            }

            var targetVersion = Helper.GetExeFileVersion(Core.TargetFileName);
            if (targetVersion.Major < 1)
            {
                Core.ShowError("Could not determine target file version.");
                Environment.Exit(666);
            }

            if (updateVersion < targetVersion)
            {
                Core.ShowError("Found update file version is older than existing target file version.");
                Environment.Exit(666);
            }

            if (!Core.TargetApplicationIsRunning(processId))
            {
                Core.ShowError("Target application not running.");
                Environment.Exit(666);
            }

            if (!Helper.ApplicationHasAdminRights())
            {
                Core.ShowError("This application was started with insufficient (non-administrative) rights.");
                Environment.Exit(666);
            }

            // Replace

            Console.WriteLine($"Update version: {updateVersion.Major}.{updateVersion.Minor}.{updateVersion.Build}");
            Console.WriteLine($"Target version: {targetVersion.Major}.{targetVersion.Minor}.{targetVersion.Build}");
            Console.WriteLine();

            if (!Core.CloseTargetApplication(processId))
            {
                Core.ShowError("Could not close target application.");
                Environment.Exit(666);
            }

            await Task.Delay(1000);

            if (!Core.ReplaceTargetFile(updateFile))
            {
                Core.ShowError("Could not replace target file with update file.");
                Environment.Exit(666);
            }

            // Restart

            if (!Core.StartTargetApplication())
            {
                Core.ShowError("Could not start target application.");
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
