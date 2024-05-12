using System.Reflection;

namespace WOWCAM.Helpers
{
    public sealed class DefaultAppHelper : IAppHelper
    {
        public string GetApplicationVersion()
        {
            // Taken from Edi Wang´s page:
            // https://edi.wang/post/2018/9/27/get-app-version-net-core

            return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        }

        public string GetApplicationExecutableFolder()
        {
            // Taken from Microsoft:
            // https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli#api-incompatibility

            return Path.GetFullPath(AppContext.BaseDirectory);
        }
    }
}
