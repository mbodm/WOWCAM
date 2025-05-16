using System.Diagnostics;
using WOWCAM.Core.Parts.Logic.System;
using WOWCAM.Helper.Parts.Application;
using WOWCAM.Helper.Parts.Download;
using WOWCAM.Helper.Parts.GitHub;
using WOWCAM.Helper.Parts.System;

namespace WOWCAM.Core.Parts.Logic.Update
{
    public sealed class DefaultUpdateManager(IReliableFileOperations reliableFileOperations, HttpClient httpClient) : IUpdateManager
    {
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));
        private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        private string updateFolder = string.Empty;

        public async Task InitAsync(string pathToApplicationTempFolder, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pathToApplicationTempFolder))
            {
                throw new ArgumentException($"'{nameof(pathToApplicationTempFolder)}' cannot be null or whitespace.", nameof(pathToApplicationTempFolder));
            }

            if (!Directory.Exists(pathToApplicationTempFolder))
            {
                throw new InvalidOperationException("Given application temp folder not exists.");
            }

            updateFolder = Path.Combine(Path.GetFullPath(pathToApplicationTempFolder), "App-Update");

            if (!Directory.Exists(updateFolder))
            {
                Directory.CreateDirectory(updateFolder);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            var installedVersion = GetInstalledVersion();
            var latestReleaseData = await GitHubHelper.GetLatestReleaseDataAsync("mbodm", "wowcam", httpClient, cancellationToken).ConfigureAwait(false);
            var updateAvailable = installedVersion < latestReleaseData.Version;

            return new UpdateData(installedVersion, latestReleaseData.Version, updateAvailable, latestReleaseData.DownloadUrl, latestReleaseData.FileName);
        }

        public async Task DownloadUpdateAsync(UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(updateData);

            CheckInitialization();

            if (!Directory.Exists(updateFolder))
            {
                Directory.CreateDirectory(updateFolder);
            }
            else
            {
                await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
            }

            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            var zipFilePath = Path.Combine(updateFolder, updateData.UpdateFileName);
            await DownloadHelper.DownloadFileAsync(httpClient, updateData.UpdateDownloadUrl, zipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);
            if (!File.Exists(zipFilePath))
            {
                throw new InvalidOperationException("Downloaded latest release, but update folder not contains zip file.");
            }

            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            await UnzipHelper.ExtractZipFileAsync(zipFilePath, updateFolder, cancellationToken).ConfigureAwait(false);

            var appFileName = AppHelper.GetApplicationExecutableFileName();
            var newExeFilePath = Path.Combine(updateFolder, appFileName);
            if (!File.Exists(newExeFilePath))
            {
                throw new InvalidOperationException($"Extracted zip file, but update folder not contains {appFileName} file.");
            }
        }

        public async Task ApplyUpdateAsync(CancellationToken cancellationToken = default)
        {
            CheckInitialization();

            if (!Directory.Exists(updateFolder))
            {
                throw new InvalidOperationException("Update folder not exists.");
            }

            var appFileName = AppHelper.GetApplicationExecutableFileName();
            var newExeFilePath = Path.Combine(updateFolder, appFileName);
            if (!File.Exists(newExeFilePath))
            {
                throw new InvalidOperationException($"Update folder not contains {appFileName} file.");
            }

            var newExeVersion = FileSystemHelper.GetExeFileVersion(newExeFilePath);
            var installedVersion = GetInstalledVersion();
            if (newExeVersion < installedVersion)
            {
                throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
            }

            var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
            var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

            File.Move(exeFilePath, bakFilePath, true);
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            File.Copy(newExeFilePath, exeFilePath, true);
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void RestartApplication(uint delayInSeconds)
        {
            if (delayInSeconds > 10)
            {
                delayInSeconds = 10;
            }

            // To decouple our .exe call from the cmd.exe process, we also need to use "start" here.
            // Since we could have spaces in our .exe path, the path has to be surrounded by quotes.
            // Doing this properly, together with "start", its fist argument has to be empty quotes.
            // See here -> https://stackoverflow.com/questions/2937569/how-to-start-an-application-without-waiting-in-a-batch-file

            var psi = new ProcessStartInfo
            {
                Arguments = $"/C ping 127.0.0.1 -n {delayInSeconds} && start \"\" \"{AppHelper.GetApplicationExecutableFilePath()}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };

            var process = Process.Start(psi) ?? throw new InvalidOperationException("The 'Process.Start()' call returned null.");
        }

        public async Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default)
        {
            var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
            var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

            if (File.Exists(bakFilePath))
            {
                File.Delete(bakFilePath);
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private void CheckInitialization()
        {
            if (updateFolder == string.Empty)
            {
                throw new InvalidOperationException("UpdateManager is not initialized (please initialize first, by calling the appropriate method.");
            }
        }

        private static Version GetInstalledVersion()
        {
            var installedExeFile = AppHelper.GetApplicationExecutableFilePath();
            var installedVersion = FileSystemHelper.GetExeFileVersion(installedExeFile);

            return installedVersion;
        }
    }
}
