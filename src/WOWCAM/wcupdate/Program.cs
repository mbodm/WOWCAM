using wcupdate;
using WOWCAM.Helper;

Console.WriteLine();
Console.WriteLine(App.Title);

if (args.Length < 1)
{
    Console.WriteLine();
    Console.WriteLine(App.Description);
    Console.WriteLine();
    Console.WriteLine(App.Usage);
    Console.WriteLine();
    Console.WriteLine(App.Help);
    Environment.Exit(1);
}

if (args.Length > 1)
{
    Core.PrintError("Too many arguments");
    Environment.Exit(2);
}

var tempFolder = Core.DecodeBase64(args[0]);
if (tempFolder == string.Empty)
{
    Core.PrintError("Given argument is not a valid base64 string.");
    Environment.Exit(3);
}

try
{
    // Start
    Core.Print("Starting update");
    // Step1
    var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
    if (!Core.Step(() => Directory.Exists(updateFolder), Output.Status1, Output.Error1)) return;
    // Step2
    var updateFilePath = Path.Combine(updateFolder, Core.TargetFileName);
    Core.Print("Update file: " + updateFilePath);
    if (!Core.Step(() => File.Exists(updateFilePath), Output.Status2, Output.Error2)) return;
    // Step3
    var targetFilePath = Path.Combine(AppHelper.GetApplicationExecutableFolder(), Core.TargetFileName);
    Core.Print("Target file: " + targetFilePath);
    if (!Core.Step(() => File.Exists(targetFilePath), Output.Status3, Output.Error3)) return;
    // Step4
    var updateVersion = FileSystemHelper.GetExeFileVersion(updateFilePath);
    var targetVersion = FileSystemHelper.GetExeFileVersion(targetFilePath);
    Core.Print($"Update file version: {updateVersion}");
    Core.Print($"Target file version: {targetVersion}");
    if (!Core.Step(() => updateVersion > targetVersion, Output.Status4, Output.Error4)) return;
    // Step5
    if (!Core.Step(() => ProcessHelper.IsRunningProcess(targetFilePath), Output.Status5, Output.Error5)) return;
    // Step6
    var processKilled = await ProcessHelper.KillProcessAsync(targetFilePath);
    if (!Core.Step(() => processKilled, Output.Status6, Output.Error6)) return;
    // Step7
    if (!Core.Step(() => FileSystemHelper.CopyFile(updateFilePath, targetFilePath), Output.Status7, Output.Error7)) return;
    // Step8
    var appStarted = await ProcessHelper.StartIndependentProcessAsync(targetFilePath);
    if (!Core.Step(() => appStarted, Output.Status8, Output.Error8)) return;
    // Finish
    Core.Print("Finished update successfully");
}
catch (Exception e)
{
    Core.PrintException(e);
    Environment.Exit(1);
}

Core.Print("Have a nice day.");
Environment.Exit(0);
