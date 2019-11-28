using RGiesecke.DllExport;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ParasiteLib
{
    public class Lib
    {
        private static NamedPipeClientStream _namedPipeClientStream;
        private static List<Command> _commandQueue;

        private static readonly object _sync = new object();

        [DllExport("Init", CallingConvention = CallingConvention.Cdecl)]
        public static void Init()
        {
            lock (_sync)
            {
                _commandQueue = new List<Command>();
                _commandQueue.Add(new Command()
                {
                    Type = CommandType.LoadROM,
                    Arg0 = @"D:\Development\C++\RetroArch\cores\nestopia_libretro.dll",
                    // Arg0 = @"D:\Development\C++\RetroArch\cores\snes9x_libretro.dll",
                    Arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario Bros..zip"
                    // Arg1 = @"D:\Development\C++\RetroArch\roms\Double Dribble.zip"
                    // Arg1 = @"D:\Development\C++\RetroArch\roms\Super Mario World.zip"
                });
            }

            _namedPipeClientStream = new NamedPipeClientStream(".", "RetroArchParasite", PipeDirection.InOut);
            Console.WriteLine("[PARASITE-LIB]: Connecting to server...");
            _namedPipeClientStream.Connect();

            Console.WriteLine("[PARASITE-LIB]: Initialized");
        }

        [DllExport("Clock", CallingConvention = CallingConvention.Cdecl)]
        public static void Clock(ulong frameCount, IntPtr commandAddress, IntPtr arg0Address, IntPtr arg1Address)
        {
            Console.WriteLine("[PARASITE-LIB]: Clock | " + frameCount);

            lock (_sync)
            {
                if (_commandQueue.Any())
                {
                    Command command = _commandQueue.First();
                    _commandQueue.RemoveAt(0);

                    Marshal.WriteInt32(commandAddress, (int)command.Type);
                    Marshal.WriteIntPtr(arg0Address, Marshal.StringToHGlobalAnsi(command.Arg0));
                    Marshal.WriteIntPtr(arg1Address, Marshal.StringToHGlobalAnsi(command.Arg1));
                }
            }
        }

        [DllExport("GameClock", CallingConvention = CallingConvention.Cdecl)]
        public static void GameClock(ulong frameCount, ulong stateSize, IntPtr stateAddress, uint pixelFormat, uint width, uint height, ulong pitch, IntPtr screenAddress)
        {
            Console.WriteLine("[PARASITE-LIB]: GameClock | " + frameCount);

            StateAndScreenMessage message = new StateAndScreenMessage()
            {
                Type = MessageType.StateAndScreen,
                FrameCount = (long)frameCount,
                State = new byte[(int)stateSize],
                PixelFormat = (PixelFormat)(int)pixelFormat,
                Width = (int)width,
                Height = (int)height,
                Pitch = (int)pitch,
            };
            Marshal.Copy(stateAddress, message.State, 0, (int)stateSize);
            int screenSize = message.Pitch * message.Height;
            message.Screen = new byte[screenSize];
            Marshal.Copy(screenAddress, message.Screen, 0, screenSize);

            byte[] messageBytes;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, message);
                messageBytes = memoryStream.ToArray();
            }

            byte[] pipeBuffer = new byte[sizeof(int) + messageBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(messageBytes.Length), 0, pipeBuffer, 0, sizeof(int));
            Buffer.BlockCopy(messageBytes, 0, pipeBuffer, sizeof(int), messageBytes.Length);

            _namedPipeClientStream.Write(pipeBuffer, 0, pipeBuffer.Length);

            //if (frameCount == 240)
            //{
            //    _commandQueue.Add(new Command()
            //    {
            //        Type = CommandType.PauseToggle,
            //    });
            //}
        }
    }
}
