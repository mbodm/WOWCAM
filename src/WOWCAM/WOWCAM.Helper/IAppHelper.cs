namespace WOWCAM.Helper
{
    public interface IAppHelper
    {
        string GetApplicationName();
        string GetApplicationVersion();
        string GetApplicationExecutableFolder();
        string GetApplicationExecutableFileName();
        string GetApplicationExecutableFilePath();
    }
}
