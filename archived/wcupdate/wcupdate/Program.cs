using wcupdate;

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
    Console.WriteLine("Too many arguments");
    Environment.Exit(2);
}

if (string.IsNullOrWhiteSpace(args[0]) || !File.Exists(args[0]))
{
    Console.WriteLine("Given argument is not a valid path to an existing file.");
    Environment.Exit(3);
}

Console.WriteLine();

// Start
Flow.Status("Started update");
// Step1
if (args[0] != "dryrun") Core.Step1CheckRights(Messages.Status1, Messages.Error1, 501);
// Step2
var updateFilePath = await Core.Step2DetermineUpdateFileAsync(args[0], Messages.Status2, Messages.Error2, 502).ConfigureAwait(false);
// Step3
Core.Step3CheckUpdateFile(updateFilePath, Messages.Status3, Messages.Error3, 503);
// Step4
var targetFilePath = Core.Step4DetermineTargetFile(Messages.Status4, Messages.Error4, 504);
// Step5
Core.Step5CheckTargetFile(targetFilePath, Messages.Status5, Messages.Error5, 505);
// Step6
Core.Step6CheckProcess(Messages.Status6, Messages.Error6, 506);
// Step7
await Core.Step7KillProcessAsync(Messages.Status7, Messages.Error7, 507).ConfigureAwait(false);
// Wait
await Task.Delay(1000).ConfigureAwait(false);
// Step8
Core.Step8CopyFile(updateFilePath, targetFilePath, Messages.Status8, Messages.Error8, 508);
// Step9
Core.Step9StartApp(targetFilePath, Messages.Status9, Messages.Error9, 509);
// Finish
Flow.Status("Finished update");

Console.WriteLine();
Console.WriteLine("Application successfully updated!");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
