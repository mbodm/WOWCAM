using System.Runtime.InteropServices;

namespace WOWCAM.Core
{
    public static partial class SingleInstanceManager
    {
        [LibraryImport("user32", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int RegisterWindowMessage(string message);

        [LibraryImport("user32", EntryPoint = "PostMessageA")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        // The mutex name was created by manually using the Guid.NewGuid() method once
        private static readonly Mutex mutex = new(true, "{0b2db15f-f1b9-47d4-b265-20b19ddf79cd}");

        // Register a custom window message
        public static readonly int WM_MBODM_WOWCAM_SHOW = RegisterWindowMessage("WM_MBODM_WOWCAM_SHOW");

        public static bool InstanceAlreadyRunning
        {
            get
            {
                if (mutex.WaitOne(TimeSpan.Zero, true))
                {
                    // No instance running
                    return false;
                }
                else
                {
                    // Instance already running
                    return true;
                }
            }
        }

        public static void PostMessageToBringRunningInstanceToFront()
        {
            const int HWND_BROADCAST = 0xFFFF;

            PostMessage(HWND_BROADCAST, WM_MBODM_WOWCAM_SHOW, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
