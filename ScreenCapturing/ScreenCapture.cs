using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace MVPDiscordBot.ScreenCapturing
{
    [SupportedOSPlatform("windows")]
    internal partial class ScreenCapture
    {
        #region Properties and Fields

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static List<WindowStruct> _WinStructList = [];

        #endregion Properties and Fields

        #region Methods

        private static bool Callback(int hWnd, int lparam)
        {
            StringBuilder sb = new(256);
            _ = GetWindowText(hWnd, sb, 256);
            string title = sb.ToString();

            if (title.Equals("maplestory", StringComparison.CurrentCultureIgnoreCase))
            {
                _WinStructList.Add(new WindowStruct { WinHwnd = hWnd, WinTitle = sb.ToString() });
            }

            return true;
        }

        public static List<WindowStruct> GetMaplestoryWindows()
        {
            _WinStructList = [];
            EnumWindows(callBackPtr, IntPtr.Zero);

            return _WinStructList;
        }

        public static Bitmap CaptureWindow(IntPtr windowHandle)
        {
            Rect rectangle = new();
            GetWindowRect(windowHandle, ref rectangle);

            Rectangle screenBounds = new(rectangle.Left,
                rectangle.Top,
                rectangle.Right - rectangle.Left,
                rectangle.Bottom - rectangle.Top);
            Bitmap bitmap = new(screenBounds.Width, screenBounds.Height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(new Point(screenBounds.Left, screenBounds.Top), Point.Empty, screenBounds.Size);
            }

            return bitmap;
        }

        public static Bitmap CaptureMaplestoryChat()
        {
            nint maplestoryChatHandle = IntPtr.Zero;
            int minWidth = Int32.MaxValue;
            List<WindowStruct> maplestoryWindows = GetMaplestoryWindows();

            foreach (WindowStruct winStruct in maplestoryWindows)
            {
                Rect rectangle = new();
                GetWindowRect(winStruct.WinHwnd, ref rectangle);

                Rectangle screenBounds = new(rectangle.Left,
                    rectangle.Top,
                    rectangle.Right - rectangle.Left,
                    rectangle.Bottom - rectangle.Top);

                if (screenBounds.Width < minWidth)
                {
                    minWidth = screenBounds.Width;
                    maplestoryChatHandle = winStruct.WinHwnd;
                }
            }

            return CaptureWindow(maplestoryChatHandle);
        }

        #endregion Methods

        #region Dll Imports

        [LibraryImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumWindows(CallBackPtr lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetDesktopWindow();

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        #endregion Dll Imports

        #region Delegates and Callbacks

        private delegate bool CallBackPtr(int hwnd, int lParam);
        private static readonly CallBackPtr callBackPtr = Callback;

        #endregion Delegates and Callbacks
    }
}
