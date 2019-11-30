using RGiesecke.DllExport;
using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace ParasiteLib
{
    public class Lib
    {
        private static NamedPipeClientStream _namedPipeClientStream;
        private static bool TEMPROMLOADED;

        [DllExport("Init", CallingConvention = CallingConvention.Cdecl)]
        public static void Init()
        {
            _namedPipeClientStream = new NamedPipeClientStream(".", "RetroArchParasite", PipeDirection.InOut);
            Console.Write("[PARASITE-LIB]: Connecting to server...");
            _namedPipeClientStream.Connect();
            Console.WriteLine("[PARASITE-LIB]: Connected.");
        }

        [DllExport("Clock", CallingConvention = CallingConvention.Cdecl)]
        public static void Clock(ulong clockCount, ulong frameCount, IntPtr commandAddress, IntPtr arg0Address, IntPtr arg1Address)
        {
            Console.WriteLine(string.Format("[PARASITE-LIB]: Clock | {0} | {1}", clockCount, frameCount));

            ClockMessage clockMessage = new ClockMessage() { ClockCount = (long)clockCount, FrameCount = (long)frameCount };
            Communication.SendMessage(_namedPipeClientStream, clockMessage);
            Message replyMessage = Communication.ReceiveMessage(_namedPipeClientStream);
            // Console.WriteLine("[PARASITE-LIB]: Received Reply | " + replyMessage.FrameCount);

            if (!TEMPROMLOADED)
            {
                string arg0 = @"D:\Development\C++\RetroArch\cores\nestopia_libretro.dll";
                // string arg0 = @"D:\Development\C++\RetroArch\cores\snes9x_libretro.dll",
                string arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario Bros..zip";
                // string arg1 = @"D:\Development\C++\RetroArch\roms\Double Dribble.zip"
                // string arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario World.zip"
                Marshal.WriteInt32(commandAddress, (int)CommandType.LoadROM);
                Marshal.WriteIntPtr(arg0Address, Marshal.StringToHGlobalAnsi(arg0));
                Marshal.WriteIntPtr(arg1Address, Marshal.StringToHGlobalAnsi(arg1));
                TEMPROMLOADED = true;
            }
        }
        
        [DllExport("GameClock", CallingConvention = CallingConvention.Cdecl)]
        public static void GameClock(ulong clockCount, ulong frameCount, ulong stateSize, IntPtr stateAddress, uint pixelFormat, uint width, uint height, ulong pitch, IntPtr screenAddress)
        {
            GameClockMessage gameClockMessage = new GameClockMessage()
            {
                ClockCount = (long)clockCount,
                FrameCount = (long)frameCount,
                State = new byte[(int)stateSize],
                PixelFormat = (PixelFormat)(int)pixelFormat,
                Width = (int)width,
                Height = (int)height,
                Pitch = (int)pitch,
            };
            Marshal.Copy(stateAddress, gameClockMessage.State, 0, (int)stateSize);
            int screenSize = gameClockMessage.Pitch * gameClockMessage.Height;
            gameClockMessage.Screen = new byte[screenSize];
            Marshal.Copy(screenAddress, gameClockMessage.Screen, 0, screenSize);

            Console.WriteLine(string.Format("[PARASITE-LIB]: GameClock | {0} | {1} | {2} | {3}", clockCount, frameCount, gameClockMessage.Width, gameClockMessage.Height));

            Communication.SendMessage(_namedPipeClientStream, gameClockMessage);
            Message replyMessage = Communication.ReceiveMessage(_namedPipeClientStream);
            // Console.WriteLine("[PARASITE-LIB]: Received Reply | " + replyMessage.FrameCount);

            //if (frameCount == 240)
            //{
            //    _commandQueue.Add(new Command()
            //    {
            //        Type = CommandType.PauseToggle,
            //    });
            //}
        }

        [DllExport("ContentLoaded", CallingConvention = CallingConvention.Cdecl)]
        public static void ContentLoaded(ulong clockCount, IntPtr contentPathAddress, IntPtr coreNameAddress, IntPtr coreVersionAddress)
        {
            ContentLoadedMessage contentLoadedMessage = new ContentLoadedMessage()
            {
                ClockCount = (long)clockCount,
                FrameCount = long.MinValue,
                ContentPath = Marshal.PtrToStringAnsi(contentPathAddress),
                CoreName = Marshal.PtrToStringAnsi(coreNameAddress),
                CoreVersion = Marshal.PtrToStringAnsi(coreVersionAddress),
            };

            Console.WriteLine(string.Format("[PARASITE-LIB]: ContentLoaded | {0} | {1} | {2} | {3}", clockCount, contentLoadedMessage.ContentPath, contentLoadedMessage.CoreName, contentLoadedMessage.CoreVersion));

            Communication.SendMessage(_namedPipeClientStream, contentLoadedMessage);
        }
    }
}
