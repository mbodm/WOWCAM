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
    Core.Process(true, 502, "Too many arguments.", string.Empty);
}

var command = args[0];
Core.Process(
    () => command != "update" && command != "dryrun",
    503,
    "Given argument is not a valid command.",
    $"Given command: {command}");

if (command == "update")
{
    Core.Process(
        Helper.ApplicationHasAdminRights(),
        504,
        "This application was started with insufficient (non-administrative) rights.", "Has sufficient rights");
}

var updateFilePath = await Core.GetUpdateFilePathAsync(command == "dryrun").ConfigureAwait(false);
Core.Process(
    () => !string.IsNullOrWhiteSpace(updateFilePath),
    505,
    "Could not determine update file location.",
    $"Determined update file path: {updateFilePath}");

Core.Process(
    () => File.Exists(updateFilePath),
    506,
    "Update file not exists.",
    "Update file exists");

var targetFilePath = Core.GetTargetFilePath();
Core.Process(
    () => !string.IsNullOrWhiteSpace(targetFilePath),
    507,
    "Target file not exists.",
    $"Determined target file path: {targetFilePath}");

Core.Process(() => Helper.ProcessIsRunning(Core.TargetFileName), 508, "Could not found running target process.", "Target process is running");

Core.Process(() => Helper.KillProcess(Core.TargetFileName), 509, "Could not kill running target process.", "Target process killed");

await Task.Delay(1000);

if (command == "update")
{
    Core.Process(!Helper.OverwriteFile(updateFilePath, targetFilePath), 510, "Could not replace target file.", "Target file replaced");
}

Core.Process(!Helper.StartProcess(targetFilePath), 511, "Could not start target app.", "Target app started");

Console.WriteLine("Successfully updated target app.");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
