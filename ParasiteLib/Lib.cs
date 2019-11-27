using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using RGiesecke.DllExport;

namespace ParasiteLib
{
    [Serializable]
    public class Test
    {
        public string Name;
        public ulong Value;
        public byte[] State;
        public byte[] Other;
    }

    public class Lib
    {
        private static NamedPipeClientStream _namedPipeClientStream;
        private static string _logMessage;
        private static Thread _thread;
        private static volatile int _counter;

        private static readonly object _sync = new object();

        [DllExport("Init", CallingConvention = CallingConvention.Cdecl)]
        public static void Init()
        {
            //_namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
            _namedPipeClientStream = new NamedPipeClientStream(".", "RetroArchParasite", PipeDirection.InOut);
            //TokenImpersonationLevel.Impersonation);
            Console.WriteLine("[PARASITE-LIB]: Connecting to server...");
            _namedPipeClientStream.Connect();

            // _namedPipeClientStream.WriteByte(0x01);

            // log("Initialized from lib");
            Console.WriteLine("[PARASITE-LIB]: Initialized from lib");
        }

        [DllExport("Clock", CallingConvention = CallingConvention.Cdecl)]
        public static void Clock()
        {
            Console.WriteLine("[PARASITE-LIB]: Clock");
        }

        [DllExport("GameClock", CallingConvention = CallingConvention.Cdecl)]
        public static void GameClock(ulong frameCount, ulong stateSize, IntPtr stateAddress, uint pixelFormat, uint width, uint height, ulong pitch, IntPtr screenAddress)
        {
            // Console.WriteLine("[PARASITE-LIB]: " + frameCount);

            //byte[] stateData = new byte[stateSize];
            //Marshal.Copy(stateDataAddress, stateData, 0, (int)stateSize);

            //Console.WriteLine("[PARASITE-LIB]: GameClock: " + stateSize);
            // Console.WriteLine("[PARASITE-LIB]: " + pixelFormat + "|" + width + "|" + height + "|" + pitch);
            // Console.WriteLine("[PARASITE-LIB]: " + stateData[4]);

            //byte[] buffer = new byte[sizeof(byte) + sizeof(ulong) + sizeof(ulong) + stateSize];
            //ulong payloadSize = sizeof(ulong) + stateSize;

            //int index = 0;
            //buffer[index] = 0x08;
            //index += sizeof(byte);
            //Buffer.BlockCopy(BitConverter.GetBytes(payloadSize), 0, buffer, index, sizeof(ulong));
            //index += sizeof(ulong);
            //Buffer.BlockCopy(BitConverter.GetBytes(stateSize), 0, buffer, index, sizeof(ulong));
            //index += sizeof(ulong);
            //// Buffer.BlockCopy(message.Payload, 0, writeBuffer, 9, message.Payload.Length);
            //// Marshal.Copy(pnt, managedArray, 0, size);
            //Marshal.Copy(stateDataAddress, buffer, index, (int)stateSize);

            // _namedPipeClientStream.Write(buffer, 0, buffer.Length);

            //Console.WriteLine("[PARASITE-LIB]: WORKED?: " + buffer.Length);

            StateAndScreenMessage message = new StateAndScreenMessage
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
        }

        [DllExport("Add", CallingConvention = CallingConvention.Cdecl)]
        public static int Add(int a, int b)
        {
            int result = a + b;
            log("Result is: " + result);
            return result;
        }

        [DllExport("BeginThread", CallingConvention = CallingConvention.Cdecl)]
        public static void BeginThread()
        {
            ThreadStart threadStart = new ThreadStart(thread);
            _thread = new Thread(threadStart);
            _thread.Start();
        }

        [DllExport("GetCounter", CallingConvention = CallingConvention.Cdecl)]
        public static int GetCounter()
        {
            return _counter;
        }

        [DllExport("ConsumeLogMessage", CallingConvention = CallingConvention.Cdecl)]
        public static string ConsumeLogMessage()
        {
            string logMessage;

            lock (_sync)
            {
                if (string.IsNullOrEmpty(_logMessage))
                {
                    return null;
                }

                logMessage = _logMessage;
                _logMessage = string.Empty;
            }

            return logMessage;
        }

        private static void log(string message)
        {
            lock (_sync)
            {
                _logMessage = message;
            }
        }

        private static void thread()
        {
            while (true)
            {
                Thread.Sleep(100);
                _counter++;
            }
        }
    }
}
