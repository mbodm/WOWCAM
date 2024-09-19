using System.IO;
using System.Windows;

namespace WOWCAMUPD
{
    public partial class MainWindow : Window
    {
        public const string TargetFileName = "WOWCAM.exe";

        public MainWindow()
        {
            InitializeComponent();

            MinWidth = Width;
            MinHeight = Height;
            Title = $"{AppHelper.GetApplicationName()} {AppHelper.GetApplicationVersion()}";
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();

            if (args.Count > 1)
            {
                LogError("Too many arguments");
                return;
            }
            
            args.ForEach(arg => Log($"Found argument: {arg}"));
            
            try
            {
                // Start
                Log("Starting update");
                // Step 1
                var (success, value) = await ConfigHelper.GetUpdateFilePathFromConfigAsync(TargetFileName);
                if (!Step(() => success, StatusMessages.Step1, value)) return;
                var updateFilePath = value;
                Log("Update file: " + updateFilePath);
                //Step 2
                if (!Step(() => File.Exists(updateFilePath), StatusMessages.Step2, ErrorMessages.Step2)) return;
                // Step 3
                var targetFilePath = Path.Combine(AppHelper.GetApplicationExecutableFolder(), TargetFileName);
                Log(StatusMessages.Step3);
                Log("Target file: " + targetFilePath);
                //Step 4
                if (!Step(() => File.Exists(targetFilePath), StatusMessages.Step4, ErrorMessages.Step4)) return;
                //Step 5
                if (!Step(() => ProcessHelper.IsRunningProcess(targetFilePath), StatusMessages.Step5, ErrorMessages.Step5)) return;
                //Step 6
                var processKilled = await ProcessHelper.KillProcessAsync(targetFilePath);
                if (!Step(() => processKilled, StatusMessages.Step6, ErrorMessages.Step6)) return;
                //Step 7
                if (!Step(() => FileSystemHelper.CopyFile(updateFilePath, targetFilePath), StatusMessages.Step7, ErrorMessages.Step7)) return;
                //Step 8
                var appStarted = await ProcessHelper.StartIndependentProcessAsync(targetFilePath);
                if (!Step(() => appStarted, StatusMessages.Step8, ErrorMessages.Step8)) return;
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

        private void Log(string msg)
        {
            textBoxLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
        }

        private void LogError(string msg)
        {
            Log($"Error: {msg}");
            Log("Cancelled");
        }

        private void LogException(Exception e)
        {
            Log("Error: An exception occurred.");
            Log($"-> Exception-Type: {e.GetType()}");
            Log($"-> Exception-Message: {e.Message}");
            Log("Cancelled");
        }

        private bool Step(Func<bool> predicate, string successMessage, string errorMessage)
        {
            var result = predicate();
            if (result) Log(successMessage); else LogError(errorMessage);

            return result;
        }
    }
}
