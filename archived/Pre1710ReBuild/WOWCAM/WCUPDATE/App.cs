namespace WCUPDATE
{
    internal static class App
    {
        public static string FileName => $"{Helper.GetAssemblyName()}.exe";
        public static string Version => Helper.GetApplicationVersion();
        public static string Title => $"{FileName} {Version} (by MBODM 2024)";
        public static string Description => $"A tiny Windows command-line tool, used by {Core.TargetFileName} to replace it's own executable.";
        public static string Usage => $"Usage: {FileName} <path to {Core.TargetFileName} update folder> <current {Core.TargetFileName} process ID>";
        public static string Link => "Have a look at https://github.com/mbodm/wowcam for more information";
    }
}
