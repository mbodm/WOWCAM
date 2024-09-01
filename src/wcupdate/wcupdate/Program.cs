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
    Core.ShowErrorAndExit("Too many arguments", 500);
}

// Step 01 (check command)
var command = args[0];
Core.Eval(() => command == "update" || command == "dryrun", Status.Step01 + command, Error.Step01, 501);

// Step 02 (check rights)
if (command == "update") Core.Eval(Helper.ApplicationHasAdminRights, Status.Step02, Error.Step02, 502);

// Step 03 (determine update file)
var verbose = command == "dryrun";
var updateFilePath = await Core.GetUpdateFilePathAsync(verbose).ConfigureAwait(false);
Core.Eval(() => !string.IsNullOrWhiteSpace(updateFilePath), Status.Step03 + updateFilePath, Error.Step03, 503);

// Step 04 (check update file)
Core.Eval(() => File.Exists(updateFilePath), Status.Step04, Error.Step04, 504);

// Step 05 (determine target file)
var targetFilePath = Core.GetTargetFilePath();
Core.Eval(() => !string.IsNullOrWhiteSpace(updateFilePath), Status.Step05 + targetFilePath, Error.Step05, 505);

// Step 06 (check target file)
Core.Eval(() => File.Exists(targetFilePath), Status.Step06, Error.Step06, 506);

// Step 07 (check process)
Core.Eval(() => Helper.ProcessIsRunning(Core.TargetFileName), Status.Step07, Error.Step07, 507);

// Step 08 (kill process)
Core.Eval(() => Helper.KillProcess(Core.TargetFileName), Status.Step08, Error.Step08, 508);

// Wait
await Task.Delay(1000);

// Step 09 (copy file)
if (command == "update") Core.Eval(() => Helper.OverwriteFile(updateFilePath, targetFilePath), Status.Step09, Error.Step09, 509);

// Step 10 (start app)
Core.Eval(() => Helper.StartProcess(targetFilePath), Status.Step10, Error.Step10, 510);

// Success
Console.WriteLine("Successfully updated target app.");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
