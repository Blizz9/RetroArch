using ParasiteLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParasiteDriver
{
    public class Driver
    {
        private NamedPipeServerStream _namedPipeServerStream;

        private volatile object _sync = new object();
        private Dictionary<int, byte> _injectBytes = new Dictionary<int, byte>();
        private string _loadState;
        private bool _saveScreen;

        public event Action ContentLoaded;
        public event Action<long, byte[]> GameClock;

        public Driver()
        {
            Task.Factory.StartNew(() => messageLoop());
        }

        public Dictionary<int, byte> InjectBytes
        {
            get
            {
                lock (_sync)
                    return _injectBytes;
            }
            set
            {
                lock (_sync)
                    _injectBytes = value;
            }
        }

        public string LoadState
        {
            get
            {
                lock (_sync)
                    return _loadState;
            }
            set
            {
                lock (_sync)
                    _loadState = value;
            }
        }

        public bool SaveScreen
        {
            get
            {
                lock (_sync)
                    return _saveScreen;
            }
            set
            {
                lock (_sync)
                    _saveScreen = value;
            }
        }

        private void messageLoop()
        {
            Debug.WriteLine("Parasite started.");

            while (true)
            {
                _namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                Debug.WriteLine("Waiting for other end of pipe to connect...");
                _namedPipeServerStream.WaitForConnection();
                Debug.WriteLine("connected.");

                while (true)
                {
                    try
                    {
                        Message message = Communication.ReceiveMessage(_namedPipeServerStream);

                        switch (message.Type)
                        {
                            case MessageType.Clock:
                                ClockMessage clockMessage = (ClockMessage)message;
                                // Debug.WriteLine("Clock: " + clockMessage.FrameCount);
                                if (message.ClockCount == 1)
                                {
                                    string arg0 = @"D:\Development\C++\RetroArch\cores\nestopia_libretro.dll";
                                    // string arg0 = @"D:\Development\C++\RetroArch\cores\snes9x_libretro.dll",
                                    string arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario Bros..zip";
                                    // string arg1 = @"D:\Development\C++\RetroArch\roms\Double Dribble.zip"
                                    // string arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario World.zip"

                                    Communication.SendMessage(_namedPipeServerStream, new CommandMessage() { ClockCount = message.ClockCount, FrameCount = message.FrameCount, CommandType = CommandType.LoadROM, Arg0 = arg0, Arg1 = arg1 });
                                }
                                else
                                {
                                    Communication.SendMessage(_namedPipeServerStream, new ClockMessage() { ClockCount = message.ClockCount, FrameCount = message.FrameCount });
                                }
                                break;

                            case MessageType.GameClock:
                                GameClockMessage gameClockMessage = (GameClockMessage)message;
                                Task.Factory.StartNew(() => GameClock?.Invoke(gameClockMessage.FrameCount, gameClockMessage.State));

                                // 0x03C4
                                Debug.WriteLine(gameClockMessage.State[0x03C4 + 0x38]);

                                if (SaveScreen)
                                {
                                    saveScreen(gameClockMessage.Width, gameClockMessage.Height, gameClockMessage.Pitch, gameClockMessage.PixelFormat, gameClockMessage.Screen);
                                    SaveScreen = false;
                                }

                                if (!string.IsNullOrWhiteSpace(LoadState))
                                {
                                    // Task.Factory.StartNew(() => saveScreen(gameClockMessage.Width, gameClockMessage.Height, gameClockMessage.Pitch, gameClockMessage.PixelFormat, gameClockMessage.Screen));
                                    LoadStateMessage loadStateMessage = new LoadStateMessage() { ClockCount = message.ClockCount, FrameCount = message.FrameCount };
                                    // loadStateMessage.State = gameClockMessage.State;
                                    // loadStateMessage.State[1938] = 5;
                                    loadStateMessage.State = File.ReadAllBytes(LoadState);
                                    LoadState = string.Empty;
                                    Communication.SendMessage(_namedPipeServerStream, loadStateMessage);
                                }
                                else if (InjectBytes.Any())
                                {
                                    LoadStateMessage loadStateMessage = new LoadStateMessage() { ClockCount = message.ClockCount, FrameCount = message.FrameCount };
                                    loadStateMessage.State = gameClockMessage.State;
                                    foreach (int location in InjectBytes.Keys)
                                    {
                                        loadStateMessage.State[location] = InjectBytes[location];
                                    }
                                    InjectBytes.Clear();
                                    Communication.SendMessage(_namedPipeServerStream, loadStateMessage);
                                }
                                else
                                {
                                    Communication.SendMessage(_namedPipeServerStream, new GameClockMessage() { ClockCount = message.ClockCount, FrameCount = message.FrameCount });
                                }

                                break;

                            case MessageType.ContentLoaded:
                                ContentLoaded?.Invoke();
                                break;
                        }
                    }
                    catch (NamedPipeClosedException)
                    {
                        break;
                    }
                }
            }
        }

        private void saveScreen(int width, int height, int pitch, ParasiteLib.PixelFormat raPixelFormat, byte[] screenData)
        {
            System.Windows.Media.PixelFormat pixelFormat;
            switch (raPixelFormat)
            {
                case ParasiteLib.PixelFormat.RGB1555:
                    pixelFormat = PixelFormats.Bgr555;
                    break;

                case ParasiteLib.PixelFormat.RGB565:
                    pixelFormat = PixelFormats.Bgr565;
                    break;

                case ParasiteLib.PixelFormat.XRGB8888:
                    pixelFormat = PixelFormats.Bgra32;
                    break;

                default:
                    pixelFormat = PixelFormats.Bgra32;
                    break;
            }

            if (raPixelFormat == ParasiteLib.PixelFormat.XRGB8888)
                // make all alpha bytes 0xFF since they aren't set properly
                for (int i = 3; i < screenData.Length; i += 4)
                    screenData[i] = 0xFF;

            BitmapSource screen = BitmapSource.Create((int)width, (int)height, 300, 300, pixelFormat, BitmapPalettes.Gray256, screenData, (int)pitch);

            using (FileStream fileStream = new FileStream("out.png", FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(screen));
                encoder.Save(fileStream);
            }
        }
    }
}
