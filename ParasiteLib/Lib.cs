using RGiesecke.DllExport;
using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace ParasiteLib
{
    public class Lib
    {
        private static NamedPipeClientStream _namedPipeClientStream;

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
            // Console.WriteLine(string.Format("[PARASITE-LIB]: Clock | {0} | {1}", clockCount, frameCount));

            ClockMessage clockMessage = new ClockMessage() { ClockCount = (long)clockCount, FrameCount = (long)frameCount };
            Communication.SendMessage(_namedPipeClientStream, clockMessage);
            Message replyMessage = Communication.ReceiveMessage(_namedPipeClientStream);

            if (replyMessage.Type == MessageType.Command)
            {
                CommandMessage commandMessage = (CommandMessage)replyMessage;
                Console.WriteLine("[PARASITE-LIB]: Received Command to Load ROM | " + commandMessage.Arg0 + " | " + commandMessage.Arg1);
                Marshal.WriteInt32(commandAddress, (int)commandMessage.CommandType);
                Marshal.WriteIntPtr(arg0Address, Marshal.StringToHGlobalAnsi(commandMessage.Arg0));
                Marshal.WriteIntPtr(arg1Address, Marshal.StringToHGlobalAnsi(commandMessage.Arg1));
            }
        }
        
        [DllExport("GameClock", CallingConvention = CallingConvention.Cdecl)]
        public static void GameClock(ulong clockCount, ulong frameCount, ulong stateSize, IntPtr stateAddress, uint pixelFormat, uint width, uint height, ulong pitch, IntPtr screenAddress, IntPtr loadStateAddress)
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

            // Console.WriteLine(string.Format("[PARASITE-LIB]: GameClock | {0} | {1} | {2} | {3}", clockCount, frameCount, gameClockMessage.Width, gameClockMessage.Height));

            Communication.SendMessage(_namedPipeClientStream, gameClockMessage);
            Message replyMessage = Communication.ReceiveMessage(_namedPipeClientStream);
            // Console.WriteLine("[PARASITE-LIB]: Received Reply | " + replyMessage.FrameCount);

            if (replyMessage.Type == MessageType.LoadState)
            {
                LoadStateMessage loadStateMessage = (LoadStateMessage)replyMessage;
                Marshal.Copy(loadStateMessage.State, 0, stateAddress, loadStateMessage.State.Length);
                Marshal.WriteByte(loadStateAddress, 1);
            }
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
