using wcupdate;

Console.WriteLine();
Console.WriteLine(App.Title);
Console.WriteLine();

// Check args

if (args.Length == 0)
{
    Console.WriteLine(App.Description);
    Console.WriteLine();
    Console.WriteLine(App.Usage);
    Console.WriteLine();
    Console.WriteLine(App.Link);
    Environment.Exit(1);
}

if (args.Length > 0 && args.Length < 2)
{
    Core.ShowError("Too few arguments.");
    Environment.Exit(501);
}

if (args.Length > 2)
{
    Core.ShowError("Too many arguments.");
    Environment.Exit(502);
}

var updateFolder = Core.EvalUpdateFolderArg(args[0]);
if (string.IsNullOrEmpty(updateFolder))
{
    Core.ShowError("Given argument is not a valid folder path.");
    Environment.Exit(503);
}

var processId = Core.EvalProcessIdArg(args[1]);
if (processId == 0)
{
    Core.ShowError("Given argument is not a valid process ID.");
    Environment.Exit(504);
}

// Validate

if (!Directory.Exists(updateFolder))
{
    Core.ShowError("Given update folder not exists.");
    Environment.Exit(505);
}

var updateFilePath = Path.Combine(updateFolder, Core.TargetFileName);
if (!File.Exists(updateFilePath))
{
    Core.ShowError("Given update folder not contains an update file.");
    Environment.Exit(506);
}

var updateVersion = Helper.GetExeFileVersion(updateFilePath);
if (updateVersion == null)
{
    Core.ShowError("Could not determine update file version.");
    Environment.Exit(507);
}

var targetVersion = Helper.GetExeFileVersion(Core.TargetFileName);
if (targetVersion == null)
{
    Core.ShowError("Could not determine target file version.");
    Environment.Exit(508);
}

if (updateVersion < targetVersion)
{
    Core.ShowError("Found update file version is older than existing target file version.");
    Environment.Exit(509);
}

if (!Helper.ProcessIsRunning(processId))
{
    Core.ShowError("Target application not running.");
    Environment.Exit(510);
}

if (!Helper.ApplicationHasAdminRights())
{
    Core.ShowError("This application was started with insufficient (non-administrative) rights.");
    Environment.Exit(511);
}

// Replace app

Console.WriteLine($"Update version: {updateVersion.Major}.{updateVersion.Minor}.{updateVersion.Build}");
Console.WriteLine($"Target version: {targetVersion.Major}.{targetVersion.Minor}.{targetVersion.Build}");
Console.WriteLine();

if (!Helper.CloseProcess(processId))
{
    Core.ShowError("Could not close target application.");
    Environment.Exit(512);
}

await Task.Delay(1000);

if (!Helper.OverwriteFile(updateFilePath, Core.TargetFilePath))
{
    Core.ShowError("Could not replace target file with update file.");
    Environment.Exit(513);
}

// Clean up

if (Directory.Exists(updateFolder))
{
    Directory.Delete(updateFolder, true);
}

// Restart app

if (!Helper.StartProcess(Core.TargetFilePath))
{
    Core.ShowError("Could not start target application.");
    Environment.Exit(514);
}

// Finish

Console.WriteLine("Update successful");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
