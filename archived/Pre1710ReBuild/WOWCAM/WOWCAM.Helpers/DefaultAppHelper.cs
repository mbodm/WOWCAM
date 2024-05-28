using System.Reflection;

namespace WOWCAM.Helpers
{
    public sealed class DefaultAppHelper : IAppHelper
    {
        public string GetApplicationName()
        {
            return Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";
        }

        public string GetApplicationVersion()
        {
            // It's the counterpart of the "Version" entry, declared in the .csproj file.

            // See Edi Wang's page:
            // https://edi.wang/post/2018/9/27/get-app-version-net-core

            return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        }

        public string GetApplicationExecutableFolder()
        {
            // See Microsoft's advice:
            // https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli#api-incompatibility

            return Path.GetFullPath(AppContext.BaseDirectory);
        }

        public string GetApplicationExecutableFileName()
        {
            return Path.ChangeExtension(GetApplicationName(), ".exe");
        }

        public string GetApplicationExecutableFilePath()
        {
            return Path.Combine(GetApplicationExecutableFolder(), GetApplicationExecutableFileName());
        }
    }
}
