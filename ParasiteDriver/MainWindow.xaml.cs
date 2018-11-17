using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ParasiteDriver
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x00;
        private const uint MOD_ALT = 0x01;
        private const uint MOD_CONTROL = 0x02;
        private const uint MOD_SHIFT = 0x04;
        private const uint MOD_WIN = 0x08;
        private const uint VK_CAPITAL = 0x14;

        private IntPtr _windowHandle;
        private HwndSource _windowHandleSource;
        private volatile bool _sendPause;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _windowHandleSource = HwndSource.FromHwnd(_windowHandle);
            _windowHandleSource.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_NONE, VK_CAPITAL);
        }

        protected override void OnClosed(EventArgs e)
        {
            _windowHandleSource.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_CAPITAL)
                            {
                                _sendPause = true;
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Debug.WriteLine("Parasite started.");

                NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                Debug.WriteLine("Waiting for other end of pipe to connect...");
                namedPipeServerStream.WaitForConnection();
                Debug.WriteLine("connected.");

                while (true)
                {
                    //Debug.Write("Waiting to receive a command...");

                    byte[] readCommandBuffer = new byte[9];
                    int readByteCount = namedPipeServerStream.Read(readCommandBuffer, 0, readCommandBuffer.Length);

                    if (readByteCount > 0)
                    {
                        //Debug.WriteLine("command received.");
                        Debug.Write(".");

                        byte command = readCommandBuffer[0];
                        ulong size = BitConverter.ToUInt64(readCommandBuffer, 1);

                        //Debug.WriteLine(string.Format("Command: [0x{0}][{1}].", command.ToString("X2"), size.ToString()));

                        if (command == 0x01)
                        {
                            //Debug.WriteLine("Replying to RetroArch with new command.");
                            byte[] writeCommandBuffer = new byte[9];

                            if (_sendPause)
                            {
                                writeCommandBuffer[0] = 0x03;
                                _sendPause = false;
                            }
                            else
                            {
                                writeCommandBuffer[0] = 0x02;
                            }
                            Buffer.BlockCopy(BitConverter.GetBytes((ulong)0), 0, writeCommandBuffer, 1, sizeof(ulong));
                            //Buffer.BlockCopy(BitConverter.GetBytes((ulong)12345), 0, writeCommandBuffer, 1, sizeof(ulong));
                            namedPipeServerStream.Write(writeCommandBuffer, 0, writeCommandBuffer.Length);
                        }
                    }

                    Thread.Sleep(1);
                }
            });
        }
    }
}
