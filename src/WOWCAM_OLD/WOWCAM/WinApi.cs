using System.Runtime.InteropServices;

namespace WOWCAM
{
    public static class WinApi
    {
        // As of 08/2024 (in .NET 8) the usage of [LibraryImport] came up with a LOT of quirks.
        // It was nearly impossible to made it work in a straight and "easy to understand" way.
        // Therefore i decided it's best to just still stick to the [DllImport] implementation.

        // https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-postmessagew
        [DllImport("user32")]
        public static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-registerwindowmessagew
        [DllImport("user32", CharSet = CharSet.Unicode)]
        public static extern uint RegisterWindowMessageW(string lpString);
    }
}
