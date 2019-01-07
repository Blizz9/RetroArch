using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        private NamedPipeServerStream _namedPipeServerStream;

        private int _frameCount;

        private volatile bool _sendPause;
        private volatile bool _requestState;
        private volatile bool _requestScreen;

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
                                _requestScreen = true;
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
            runNamedPipeLoop();
        }

        #region Named Pipe Loop

        private void runNamedPipeLoop()
        {
            Task.Factory.StartNew(() =>
            {
                Debug.WriteLine("Parasite started.");

                _namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                Debug.WriteLine("Waiting for other end of pipe to connect...");
                _namedPipeServerStream.WaitForConnection();
                Debug.WriteLine("connected.");

                while (true)
                {
                    waitForMessageType(MessageType.Ping);
                    _frameCount++;

                    Message message = new Message();

                    if (_sendPause)
                    {
                        _sendPause = false;
                        message.Type = MessageType.Pause;
                        sendMessage(message);
                    }
                    else if (_requestState)
                    {
                        _requestState = false;
                        message.Type = MessageType.RequestState;
                        sendMessage(message);
                        Message stateMessage = receiveMessage();
                        File.WriteAllBytes("test.state", stateMessage.Payload);
                    }
                    else if (_requestScreen)
                    {
                        _requestScreen = false;
                        message.Type = MessageType.RequestScreen;
                        sendMessage(message);
                        Message screenMessage = receiveMessage();

                        uint raPixelFormat = BitConverter.ToUInt32(screenMessage.Payload, 0);
                        uint width = BitConverter.ToUInt32(screenMessage.Payload, sizeof(uint));
                        uint height = BitConverter.ToUInt32(screenMessage.Payload, (sizeof(uint) + sizeof(uint)));
                        uint pitch = BitConverter.ToUInt32(screenMessage.Payload, (sizeof(uint) + sizeof(uint) + sizeof(uint)));

                        System.Windows.Media.PixelFormat pixelFormat;
                        switch ((PixelFormat)raPixelFormat)
                        {
                            case PixelFormat.RGB1555:
                                pixelFormat = PixelFormats.Bgr555;
                                break;

                            case PixelFormat.RGB565:
                                pixelFormat = PixelFormats.Bgr565;
                                break;

                            case PixelFormat.XRGB8888:
                                pixelFormat = PixelFormats.Bgra32;
                                break;

                            default:
                                pixelFormat = PixelFormats.Bgra32;
                                break;
                        }

                        //byte[] screenPayload = new byte[width * height * pixelMemorySize];
                        byte[] screenPayload = new byte[height * pitch];
                        //Array.Copy(screenMessage.Payload, (sizeof(uint) + sizeof(uint) + sizeof(uint)), screenPayload, 0, (width * height * pixelMemorySize));
                        Array.Copy(screenMessage.Payload, (sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint)), screenPayload, 0, (height * pitch));

                        if ((PixelFormat)raPixelFormat == PixelFormat.XRGB8888)
                            // make all alpha bytes 0xFF since they aren't set properly
                            for (int i = 3; i < screenPayload.Length; i += 4)
                                screenPayload[i] = 0xFF;

                        //BitmapSource screen = BitmapSource.Create((int)width, (int)height, 300, 300, pixelFormat, BitmapPalettes.Gray256, screenPayload, ((int)width * pixelMemorySize));
                        BitmapSource screen = BitmapSource.Create((int)width, (int)height, 300, 300, pixelFormat, BitmapPalettes.Gray256, screenPayload, (int)pitch);

                        using (FileStream fileStream = new FileStream("test.png", FileMode.Create))
                        {
                            BitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(screen));
                            encoder.Save(fileStream);
                        }
                    }
                    else
                    {
                        //message.Type = MessageType.Test;
                        //message.Payload = BitConverter.GetBytes((uint)112233);
                        message.Type = MessageType.Pong;
                        sendMessage(message);
                    }

                    Thread.Sleep(1);
                }
            });
        }

        #endregion

        #region Communication Routines

        private Message receiveMessage()
        {
            Message message = new Message();

            byte[] readBuffer = new byte[9];
            int readByteCount = _namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);

            if (readByteCount == 0)
                throw new Exception("Unable to read message properly.");

            message.Type = (MessageType)readBuffer[0];
            ulong payloadSize = BitConverter.ToUInt64(readBuffer, 1);

            if (payloadSize > 0)
            {
                message.Payload = new byte[payloadSize];
                readByteCount = _namedPipeServerStream.Read(message.Payload, 0, (int)payloadSize);

                if (readByteCount == 0)
                    throw new Exception("Unable to read message payload properly.");
            }

            return (message);
        }

        private void sendMessage(Message message)
        {
            byte[] writeBuffer = new byte[9 + message.Payload.Length];

            writeBuffer[0] = (byte)message.Type;
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)message.Payload.Length), 0, writeBuffer, 1, sizeof(ulong));

            if (message.Payload.Length > 0)
                Buffer.BlockCopy(message.Payload, 0, writeBuffer, 9, message.Payload.Length);

            _namedPipeServerStream.Write(writeBuffer, 0, writeBuffer.Length);
        }

        private void waitForMessageType(MessageType messageType)
        {
            Message message = receiveMessage();

            if (message.Type != messageType)
                throw new Exception("Incorrect message type was received.");
        }

        #endregion
    }
}
