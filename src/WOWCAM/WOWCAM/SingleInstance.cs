using System.Windows;
using System.Windows.Interop;

namespace WOWCAM
{
    // Implementation follows this guideline and Stack Overflow posts:
    // http://sanity-free.org/143/csharp_dotnet_single_instance_application.html
    // https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-wpf-application/2932076#2932076

    public static class SingleInstance
    {
        // Mutex name was created by manually using the Guid.NewGuid() method once
        private static readonly Mutex mutex = new(true, "{0b2db15f-f1b9-47d4-b265-20b19ddf79cd}");

        // Register a specific custom window message
        private static readonly uint WM_MBODM_WOWCAM_SHOW = WinApi.RegisterWindowMessageW("WM_MBODM_WOWCAM_SHOW");

        public static bool AnotherInstanceIsAlreadyRunning
        {
            get
            {
                if (mutex.WaitOne(TimeSpan.Zero, true))
                {
                    return false; // No instance running
                }
                else
                {
                    return true; // Instance already running
                }
            }
        }

        public static void RegisterHook(Window mainWindow)
        {
            // This method registers a hook which reacts to a specific custom window message
            // This application brings its main window to front after receiving that message

            var hwnd = new WindowInteropHelper(mainWindow).Handle;
            var hwndSource = HwndSource.FromHwnd(hwnd); // Do not dispose this (or the app will close immediately)

            hwndSource.AddHook(new HwndSourceHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == WM_MBODM_WOWCAM_SHOW)
                {
                    handled = true;

                    // In Windows only the active foreground thread is allowed to set the foreground window
                    // But existing instance is no longer foreground thread after starting another instance
                    // This 3 steps are a well-known trick to prevent the application from taskbar blinking
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Activate();
                    mainWindow.WindowState = WindowState.Normal;
                }

                return IntPtr.Zero;
            }));
        }

        public static void BroadcastMessage()
        {
            // This method sends a specific custom window message to any already running application instance
            // Such a running application instance will bring its window to front when receiving that message
            // That mutex above is used to find out if such another application instance is currently running

            const int HWND_BROADCAST = 0xFFFF;

            WinApi.PostMessageW(HWND_BROADCAST, WM_MBODM_WOWCAM_SHOW, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
