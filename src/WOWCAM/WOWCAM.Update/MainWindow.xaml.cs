using System.IO;
using System.Windows;
using WOWCAM.Helper;

namespace WOWCAM.Update
{
    public partial class MainWindow : Window
    {
        public MainWindow
        {
            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"{appHelper.GetApplicationName()} {appHelper.GetApplicationVersion()}";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            const string TargetFileName = "WOWCAM.exe";

            var args = Environment.GetCommandLineArgs().Skip(1);

            if (args.Count() > 1)
            {
                LogError("Too many arguments");
                return;
            }

            args.ToList().ForEach(arg => Log($"Found argument: {arg}"));

            try
            {
                // Start
                Log("Starting update");
                // Step1
                var (success, value, error) = await GetUpdateFolderFromConfigAsync();
                if (!Step(() => success, Messages.Status1, error)) return;
                var updateFolder = value;
                // Step2
                var updateFilePath = Path.Combine(updateFolder, TargetFileName);
                Log("Update file: " + updateFilePath);
                if (!Step(() => File.Exists(updateFilePath), Messages.Status2, Messages.Error2)) return;
                // Step3
                var targetFilePath = Path.Combine(appHelper.GetApplicationExecutableFolder(), TargetFileName);
                Log("Target file: " + targetFilePath);
                if (!Step(() => File.Exists(targetFilePath), Messages.Status3, Messages.Error3)) return;
                // Step4
                var updateVersion = fileSystemHelper.GetExeFileVersion(updateFilePath);
                var targetVersion = fileSystemHelper.GetExeFileVersion(targetFilePath);
                Log($"Update file version: {updateVersion}");
                Log($"Target file version: {targetVersion}");
                if (!Step(() => updateVersion > targetVersion, Messages.Status4, Messages.Error4)) return;
                // Step5
                if (!Step(() => processHelper.IsRunningProcess(targetFilePath), Messages.Status5, Messages.Error5)) return;
                // Step6
                var processKilled = await processHelper.KillProcessAsync(targetFilePath);
                if (!Step(() => processKilled, Messages.Status6, Messages.Error6)) return;
                // Step7
                if (!Step(() => fileSystemHelper.CopyFile(updateFilePath, targetFilePath), Messages.Status7, Messages.Error7)) return;
                // Step8
                var appStarted = await processHelper.StartIndependentProcessAsync(targetFilePath);
                if (!Step(() => appStarted, Messages.Status8, Messages.Error8)) return;
                // Finish
                Log("Finished update successfully");
            }
            catch (Exception ex)
            {
                LogException(ex);
                return;
            }

            if (args.Contains("/autoclose"))
            {
                for (int i = 5; i > 1; i--)
                {
                    Log($"This application will close in {i} seconds");
                    await Task.Delay(1000);
                }

                Log("Have a nice day.");

                Close();
            }
        }
    }
}
