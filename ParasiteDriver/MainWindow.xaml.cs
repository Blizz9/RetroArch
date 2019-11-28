using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace ParasiteDriver
{
    [Serializable]
    public class Test
    {
        public byte[] State;
    }

    public partial class MainWindow : Window
    {
        #region Win32 Api

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WM_CLOSE = 0x10;
        private const int WS_CHILD = 0x40000000;

        #endregion

        private Process _raProcess;
        private IntPtr _raLogWindow = IntPtr.Zero;
        private IntPtr _raWindow = IntPtr.Zero;

        private Driver _driver;
        private HotKey _hotKey;

        private SMB _smb;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _hotKey = new HotKey(this);
            _hotKey.Pressed += hotKeyPressed;
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKey.Cleanup();

            base.OnClosed(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _driver = new Driver();
            _driver.Connected += driverConnected;

            // _smb = new SMB(_driver);

            _raProcess = Process.Start(@"..\..\..\..\retroarch.exe");
            _raProcess.WaitForInputIdle();
            _raLogWindow = _raProcess.MainWindowHandle;

            foreach (ProcessThread thread in _raProcess.Threads)
            {
                EnumThreadWindows((uint)thread.Id, (windowHandle, lParam) =>
                {
                    StringBuilder windowTitle = new StringBuilder(GetWindowTextLength(windowHandle) * 2);
                    GetWindowText(windowHandle, windowTitle, windowTitle.Capacity);

                    if (windowTitle.ToString() == "RetroArch")
                        _raWindow = windowHandle;

                    return true;
                }, IntPtr.Zero);
            }

            SetParent(_raLogWindow, new WindowInteropHelper(this).Handle);
            SetParent(_raWindow, new WindowInteropHelper(this).Handle);
            SetWindowLong(_raLogWindow, GWL_STYLE, WS_CHILD);
            SetWindowLong(_raLogWindow, GWL_STYLE, WS_VISIBLE);
            SetWindowLong(_raWindow, GWL_STYLE, WS_CHILD);
            SetWindowLong(_raWindow, GWL_STYLE, WS_VISIBLE);
            Point logPanelLocation = logPanel.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
            MoveWindow(_raLogWindow, (int)logPanelLocation.X, (int)logPanelLocation.Y, (int)logPanel.ActualWidth, (int)logPanel.ActualHeight, true);
            Point mainPanelLocation = mainPanel.TransformToAncestor(Application.Current.MainWindow).Transform(new Point(0, 0));
            MoveWindow(_raWindow, (int)mainPanelLocation.X, (int)mainPanelLocation.Y, (int)mainPanel.ActualWidth, (int)mainPanel.ActualHeight, true);
        }

        private void driverConnected()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (ProcessThread thread in _raProcess.Threads)
                {
                    EnumThreadWindows((uint)thread.Id, (windowHandle, lParam) =>
                    {
                        StringBuilder windowTitle = new StringBuilder(GetWindowTextLength(windowHandle) * 2);
                        GetWindowText(windowHandle, windowTitle, windowTitle.Capacity);

                        if (windowTitle.ToString() == "RetroArch")
                            _raWindow = windowHandle;

                        return true;
                    }, IntPtr.Zero);
                }

                SetParent(_raWindow, new WindowInteropHelper(this).Handle);
                SetWindowLong(_raWindow, GWL_STYLE, WS_CHILD);
                SetWindowLong(_raWindow, GWL_STYLE, WS_VISIBLE);
                MoveWindow(_raWindow, 0, 0, 879, 672, true);
            }));
        }

        private void hotKeyPressed()
        {
            _driver.RequestScreen = true;
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SetForegroundWindow(_raWindow);
        }
    }
}
