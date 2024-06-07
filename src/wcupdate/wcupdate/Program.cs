using wcupdate;

Console.WriteLine();
Console.WriteLine(App.Title);
Console.WriteLine();

if (args.Length < 1)
{
    Console.WriteLine(App.Description);
    Console.WriteLine();
    Console.WriteLine(App.Usage);
    Console.WriteLine();
    Console.WriteLine(App.Help);
    Environment.Exit(1);
}

if (args.Length > 1)
{
    Core.ShowError("Too many arguments.");
    Environment.Exit(502);
}

if (!int.TryParse(args[0], out int processId))
{
    Core.ShowError("Given argument is not a valid process ID.");
    Environment.Exit(503);
}

if (!Helper.ApplicationHasAdminRights())
{
    Core.ShowError("This application was started with insufficient (non-administrative) rights.");
    Environment.Exit(504);
}

var updateFolder = await Core.GetUpdateFolderAsync().ConfigureAwait(false);
if (string.IsNullOrEmpty(updateFolder))
{
    Core.ShowError("Could not determine update folder.");
    Environment.Exit(505);
}

var sourceFile = Path.Combine(updateFolder, Core.TargetFileName);
if (!File.Exists(sourceFile))
{
    Core.ShowError("Source file not exists.");
    Environment.Exit(506);
}

var destFile = Path.Combine(Helper.GetApplicationExecutableFolder(), Core.TargetFileName);
if (!File.Exists(destFile))
{
    Core.ShowError("Destination file not exists.");
    Environment.Exit(507);
}

if (!Helper.ProcessIsRunning(processId))
{
    Core.ShowError("Could not found running process with given process ID.");
    Environment.Exit(508);
}

if (!Helper.CloseProcess(processId))
{
    Core.ShowError($"Could not close running process with given process ID.");
    Environment.Exit(509);
}

await Task.Delay(1000);

if (!Helper.OverwriteFile(sourceFile, destFile))
{
    Core.ShowError($"Could not replace destination file.");
    Environment.Exit(510);
}

if (!Helper.StartProcess(destFile))
{
    Core.ShowError("Could not start destination file.");
    Environment.Exit(511);
}

Console.WriteLine("Successfully replaced destination file.");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
