using System.Reflection;

namespace WOWCAM.Helper
{
    public sealed class DefaultAppHelper : IAppHelper
    {
        public string GetApplicationName() =>
            Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";

        // It's the counterpart of the "Version" entry, declared in the .csproj file.
        // See Edi Wang's page -> https://edi.wang/post/2018/9/27/get-app-version-net-core
        public string GetApplicationVersion() =>
            Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        // See Microsoft's advice -> https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli#api-incompatibility
        public string GetApplicationExecutableFolder() =>
            Path.GetFullPath(AppContext.BaseDirectory);

        public string GetApplicationExecutableFileName() =>
            Path.ChangeExtension(GetApplicationName(), ".exe");

        public string GetApplicationExecutableFilePath() =>
            Path.Combine(GetApplicationExecutableFolder(), GetApplicationExecutableFileName());
    }
}
