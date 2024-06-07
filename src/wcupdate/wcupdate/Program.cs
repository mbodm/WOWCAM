using wcupdate;

Console.WriteLine();
Console.WriteLine(App.Title);
Console.WriteLine();

if (args.Length == 0)
{
    Console.WriteLine(App.Description);
    Console.WriteLine();
    Console.WriteLine(App.Usage);
    Console.WriteLine();
    Console.WriteLine(App.Help);
    Environment.Exit(1);
}

if (args.Length > 0 && args.Length < 3)
{
    Core.ShowError("Too few arguments.");
    Environment.Exit(301);
}

if (args.Length > 3)
{
    Core.ShowError("Too many arguments.");
    Environment.Exit(302);
}

var sourceFile = Core.EvalFilePathArg(args[0]);
if (string.IsNullOrEmpty(sourceFile))
{
    Core.ShowError("Given argument is not a valid source file path.");
    Environment.Exit(303);
}

var destFile = Core.EvalFilePathArg(args[1]);
if (string.IsNullOrEmpty(sourceFile))
{
    Core.ShowError("Given argument is not a valid destination file path.");
    Environment.Exit(304);
}

var processId = Core.EvalProcessIdArg(args[2]);
if (processId == -1)
{
    Core.ShowError("Given argument is not a valid process ID.");
    Environment.Exit(305);
}

if (!Helper.ApplicationHasAdminRights())
{
    Core.ShowError("This application was started with insufficient (non-administrative) rights.");
    Environment.Exit(306);
}



await File.AppendAllTextAsync("fuzz.txt", sourceFile).ConfigureAwait(false);
await File.AppendAllTextAsync("fuzz.txt", destFile).ConfigureAwait(false);
await File.AppendAllTextAsync("fuzz.txt", processId.ToString()).ConfigureAwait(false);



if (!File.Exists(sourceFile))
{
    Core.ShowError("Given source file not exists.");
    Environment.Exit(307);
}

if (!File.Exists(destFile))
{
    Core.ShowError("Given destination file not exists.");
    Environment.Exit(308);
}

if (!Helper.ProcessIsRunning(processId))
{
    Core.ShowError("Could not found running process with given process ID.");
    Environment.Exit(309);
}

if (!Helper.CloseProcess(processId))
{
    Core.ShowError($"Could not close running process with given process ID.");
    Environment.Exit(310);
}

await Task.Delay(1000);

if (!Helper.OverwriteFile(sourceFile, destFile))
{
    Core.ShowError($"Could not replace destination file.");
    Environment.Exit(311);
}

if (!Helper.StartProcess(destFile))
{
    Core.ShowError("Could not start destination file.");
    Environment.Exit(312);
}

Console.WriteLine("Successfully replaced destination file.");
Console.WriteLine();
Console.WriteLine("Have a nice day.");
Environment.Exit(0);
