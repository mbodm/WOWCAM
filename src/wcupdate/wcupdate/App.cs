namespace wcupdate
{
    internal static class App
    {
        public static string FileName => $"{Helper.GetAssemblyName()}.exe";
        public static string Version => Helper.GetApplicationVersion();
        public static string Title => $"{FileName} {Version} (by MBODM 05/2024)";
        public static string Description => $"A tiny Windows command-line tool, used by {Core.TargetAppName} to replace it's own executable.";
        public static string Usage => $"Usage: {FileName} <path to new {Core.TargetFileName}> <path to existing {Core.TargetFileName}> <process ID of running {Core.TargetFileName}>";
        public static string Help => "Have a look at https://github.com/mbodm/wowcam for more information";
    }
}
