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

            if (args.Count == 0)
            {
                LogError("No command line arguments given");
                return;
            }

            args.ForEach(arg => Log($"Found argument: {arg}"));

            if (!FileSystemHelper.IsValidAbsolutePath(args[0]))
            {
                LogError("First given command line argument is not a valid absolute path to a file or folder.");
                return;
            }

            var updateFilePath = Path.Combine(args[0], TargetFileName);
            Log("Update file: " + updateFilePath);

            var targetFilePath = Path.Combine(AppHelper.GetApplicationExecutableFolder(), TargetFileName);
            Log("Target file: " + targetFilePath);

            try
            {
                // Start
                Log("Starting update");
                //Step 1
                if (!Step(() => File.Exists(updateFilePath), StatusMessages.Step1, ErrorMessages.Step1)) return;
                //Step 2
                if (!Step(() => File.Exists(targetFilePath), StatusMessages.Step2, ErrorMessages.Step2)) return;
                //Step 3
                if (!Step(() => ProcessHelper.IsRunningProcess(targetFilePath), StatusMessages.Step3, ErrorMessages.Step3)) return;
                //Step 4
                var processKilled = await ProcessHelper.KillProcessAsync(targetFilePath);
                if (!Step(() => processKilled, StatusMessages.Step4, ErrorMessages.Step4)) return;
                //Step 5
                if (!Step(() => FileSystemHelper.CopyFile(updateFilePath, targetFilePath), StatusMessages.Step5, ErrorMessages.Step5)) return;
                //Step 6
                var appStarted = await ProcessHelper.StartIndependentProcessAsync(targetFilePath);
                if (!Step(() => appStarted, StatusMessages.Step6, ErrorMessages.Step6)) return;
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
