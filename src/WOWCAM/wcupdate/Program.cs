using wcupdate;
using WOWCAM.Helper;

const string TargetFileName = "WOWCAM.exe";

if (args.Length > 1)
{
    Core.PrintError("Too many arguments");
    return;
}

args.ToList().ForEach(arg => Core.Print($"Found argument: {arg}"));

try
{
    // Start
    Core.Print("Starting update");
    // Step1
    var (success, value, error) = await Core.GetUpdateFolderFromConfigAsync().ConfigureAwait(false);
    if (!Core.Step(() => success, Messages.Status1, error)) return;
    var updateFolder = value;
    // Step2
    var updateFilePath = Path.Combine(updateFolder, TargetFileName);
    Core.Print("Update file: " + updateFilePath);
    if (!Core.Step(() => File.Exists(updateFilePath), Messages.Status2, Messages.Error2)) return;
    // Step3
    var targetFilePath = Path.Combine(AppHelper.GetApplicationExecutableFolder(), TargetFileName);
    Core.Print("Target file: " + targetFilePath);
    if (!Core.Step(() => File.Exists(targetFilePath), Messages.Status3, Messages.Error3)) return;
    // Step4
    var updateVersion = FileSystemHelper.GetExeFileVersion(updateFilePath);
    var targetVersion = FileSystemHelper.GetExeFileVersion(targetFilePath);
    Core.Print($"Update file version: {updateVersion}");
    Core.Print($"Target file version: {targetVersion}");
    if (!Core.Step(() => updateVersion > targetVersion, Messages.Status4, Messages.Error4)) return;
    // Step5
    if (!Core.Step(() => ProcessHelper.IsRunningProcess(targetFilePath), Messages.Status5, Messages.Error5)) return;
    // Step6
    var processKilled = await ProcessHelper.KillProcessAsync(targetFilePath);
    if (!Core.Step(() => processKilled, Messages.Status6, Messages.Error6)) return;
    // Step7
    if (!Core.Step(() => FileSystemHelper.CopyFile(updateFilePath, targetFilePath), Messages.Status7, Messages.Error7)) return;
    // Step8
    var appStarted = await ProcessHelper.StartIndependentProcessAsync(targetFilePath);
    if (!Core.Step(() => appStarted, Messages.Status8, Messages.Error8)) return;
    // Finish
    Core.Print("Finished update successfully");
}
catch (Exception ex)
{
    Core.PrintException(ex);
    Environment.Exit(1);
}

if (args.Contains("/autoclose"))
{
    for (int i = 5; i > 1; i--)
    {
        Core.Print($"This application will close in {i} seconds");
        await Task.Delay(1000);
    }

    Core.Print("Have a nice day.");

    Environment.Exit(0);
}
