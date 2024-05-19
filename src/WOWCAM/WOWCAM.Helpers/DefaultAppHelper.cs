using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

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

        public bool ApplicationHasAdminRights()
        {
            // Taken from StackOverflow:
            // https://stackoverflow.com/questions/5953240/check-for-administrator-privileges-in-c-sharp

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var identity = WindowsIdentity.GetCurrent();

                if (identity != null)
                {
                    var principal = new WindowsPrincipal(identity);

                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }

            return false;
        }
    }
}
