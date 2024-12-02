using WOWCAM.Helper;

namespace wcupdate
{
    internal static class App
    {
        public static string FileName => $"{AppHelper.GetApplicationName()}.exe".ToLower();
        public static string Version => AppHelper.GetApplicationVersion();
        public static string Title => $"{FileName} {Version} (by MBODM 09/2024)";
        public static string Description => $"A tiny Windows command-line tool, used by {Core.TargetAppName} to replace it's own executable.";
        public static string Usage => $"Usage: {FileName} <base64-encoded path to WOWCAM's configured temp folder>";
        public static string Help => "Have a look at \"https://github.com/mbodm/wowcam\" for more information.";
    }
}
