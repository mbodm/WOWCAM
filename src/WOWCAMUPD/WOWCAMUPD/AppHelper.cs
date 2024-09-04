using System.IO;
using System.Reflection;

namespace WOWCAMUPD
{
    internal static class AppHelper
    {
        public static string GetApplicationName() =>
            Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";

        // It's the counterpart of the "Version" entry, declared in the .csproj file.
        public static string GetApplicationVersion() =>
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        // See Microsoft's advice -> https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli#api-incompatibility
        public static string GetApplicationExecutableFolder() =>
            Path.GetFullPath(AppContext.BaseDirectory);
    }
}
