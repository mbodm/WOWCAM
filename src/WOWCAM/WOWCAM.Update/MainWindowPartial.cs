using System.IO;
using System.Windows;
using System.Xml.Linq;

namespace WOWCAM.Update
{
    public partial class MainWindow : Window
    {
        private void Log(string msg)
        {
            textBoxLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }

        private void LogError(string msg)
        {
            Log($"Error: {msg}");
            Log("Stopped");
        }

        private void LogException(Exception e)
        {
            Log("Error: An exception occurred.");
            Log($"-> Exception-Type: {e.GetType()}");
            Log($"-> Exception-Message: {e.Message}");
            Log("Stopped");
        }

        private bool Step(Func<bool> predicate, string successMessage, string errorMessage)
        {
            var result = predicate();

            if (result) Log(successMessage);
            else LogError(errorMessage);

            return result;
        }

        private static async Task<(bool Success, string Value, string Error)> GetUpdateFolderFromConfigAsync()
        {
            var xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");
            if (!File.Exists(xmlFile))
            {
                return (false, string.Empty, "Could not found WOWCAM config file.");
            }

            using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None).ConfigureAwait(false);
            var temp = doc.Root?.Element("general")?.Element("temp")?.Value?.Trim() ?? "%TEMP%";
            if (string.IsNullOrWhiteSpace(temp))
            {
                return (false, string.Empty, "Could not determine WOWCAM temp folder.");
            }

            var tempFolder = Environment.ExpandEnvironmentVariables(temp);
            var updateFolder = Path.Combine(tempFolder, "MBODM-WOWCAM-Update");
            if (!Directory.Exists(updateFolder))
            {
                return (false, string.Empty, "Update folder not exists.");
            }

            fileStream.Close();

            return (true, updateFolder, string.Empty);
        }
    }
}
