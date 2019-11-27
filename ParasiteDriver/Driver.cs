using ParasiteLib;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParasiteDriver
{
    public class Driver
    {
        NamedPipeServerStream _namedPipeServerStream;

        public event Action Connected;
        public event Action<int, byte[]> Clock;

        public volatile bool PauseToggle;
        public volatile bool RequestState;
        public volatile bool RequestScreen;

        public Driver()
        {
            Task.Factory.StartNew(() => messageLoop());
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

                Connected?.Invoke();

                while (true)
                {
                    try
                    {
                        byte[] readBuffer = new byte[sizeof(int)];
                        int readByteCount = _namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);

                        if (readByteCount == 0)
                        {
                            _namedPipeServerStream.Close();
                            throw new NamedPipeClosedException("Incorrect message type was received, likely the named pipe was closed from the other end.");
                        }

                        // int messageType = BitConverter.ToInt32(readBuffer, 0);
                        // int messageSize = BitConverter.ToInt32(readBuffer, sizeof(int));
                        int messageSize = BitConverter.ToInt32(readBuffer, 0);

                        readBuffer = new byte[messageSize];
                        readByteCount = _namedPipeServerStream.Read(readBuffer, 0, messageSize);

                        ParasiteLib.Message message;
                        BinaryFormatter binaryFormatter = new BinaryFormatter();
                        using (MemoryStream memoryStream = new MemoryStream(readBuffer))
                        {
                            message = (ParasiteLib.Message)binaryFormatter.Deserialize(memoryStream);
                        }

                        Console.WriteLine(message.Type);
                        Console.WriteLine(message.FrameCount);
                        StateAndScreenMessage stateAndScreenMessage = (StateAndScreenMessage)message;
                        Console.WriteLine(stateAndScreenMessage.Height);
                        Console.WriteLine(stateAndScreenMessage.PixelFormat);

                        //if (stateAndScreen.FrameCount == 120)
                        //{
                        //    // make all alpha bytes 0xFF since they aren't set properly
                        //    for (int i = 3; i < stateAndScreen.Screen.Length; i += 4)
                        //        stateAndScreen.Screen[i] = 0xFF;

                        //    BitmapSource screen = BitmapSource.Create(stateAndScreen.Width, stateAndScreen.Height, 300, 300, PixelFormats.Bgra32, BitmapPalettes.Gray256, stateAndScreen.Screen, stateAndScreen.Pitch);

                        //    using (FileStream fileStream = new FileStream("test.png", FileMode.Create))
                        //    {
                        //        BitmapEncoder encoder = new PngBitmapEncoder();
                        //        encoder.Frames.Add(BitmapFrame.Create(screen));
                        //        encoder.Save(fileStream);
                        //    }
                        //}

                        //Message pingMessage = waitForMessageType(MessageType.Ping);

                        //int i = 0;
                        //ulong frameCount = BitConverter.ToUInt64(pingMessage.Payload, i);
                        //i += sizeof(ulong);

                        //byte[] state = new byte[pingMessage.Payload.Length - i];
                        //Array.Copy(pingMessage.Payload, i, state, 0, (pingMessage.Payload.Length - i));

                        //Task.Factory.StartNew(() => Clock?.Invoke((int)frameCount, state));

                        //if (PauseToggle)
                        //{
                        //    PauseToggle = false;
                        //    handlePauseToggle();
                        //}
                        //else if (RequestState)
                        //{
                        //    RequestState = false;
                        //    handleRequestState();
                        //}
                        //else if (RequestScreen)
                        //{
                        //    RequestScreen = false;
                        //    handleRequestScreen();
                        //}
                        //else
                        //{
                        //    handlePong();
                        //}
                    }
                    catch (NamedPipeClosedException)
                    {
                        break;
                    }
                }
            }
        }

        #region Message Handlers

        private void handlePong()
        {
            Message message = new Message();
            message.Type = MessageType.Pong;
            sendMessage(message);
        }

        private void handlePauseToggle()
        {
            Message message = new Message();
            message.Type = MessageType.PauseToggle;
            sendMessage(message);
        }

        private void handleRequestState()
        {
            Message message = new Message();
            message.Type = MessageType.RequestState;
            sendMessage(message);

            Message stateMessage = receiveMessage();

            File.WriteAllBytes("test.state", stateMessage.Payload);
        }

        private void handleRequestScreen()
        {
            Message message = new Message();
            message.Type = MessageType.RequestScreen;
            sendMessage(message);

            Message screenMessage = receiveMessage();

            int i = 0;
            uint raPixelFormat = BitConverter.ToUInt32(screenMessage.Payload, i);
            i += sizeof(uint);
            uint width = BitConverter.ToUInt32(screenMessage.Payload, i);
            i += sizeof(uint);
            uint height = BitConverter.ToUInt32(screenMessage.Payload, i);
            i += sizeof(uint);
            uint pitch = BitConverter.ToUInt32(screenMessage.Payload, i);
            i += sizeof(uint);

            byte[] screenPayload = new byte[height * pitch];
            Array.Copy(screenMessage.Payload, i, screenPayload, 0, (height * pitch));

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

            if ((PixelFormat)raPixelFormat == PixelFormat.XRGB8888)
                // make all alpha bytes 0xFF since they aren't set properly
                for (i = 3; i < screenPayload.Length; i += 4)
                    screenPayload[i] = 0xFF;

            BitmapSource screen = BitmapSource.Create((int)width, (int)height, 300, 300, pixelFormat, BitmapPalettes.Gray256, screenPayload, (int)pitch);

            using (FileStream fileStream = new FileStream("test.png", FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(screen));
                encoder.Save(fileStream);
            }
        }

        #endregion

        #region Communication Routines

        private Message receiveMessage()
        {
            Message message = new Message();

            byte[] readBuffer = new byte[sizeof(byte) + sizeof(ulong)];
            int readByteCount = _namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);

            if (readByteCount == 0)
            {
                _namedPipeServerStream.Close();
                throw new NamedPipeClosedException("Incorrect message type was received, likely the named pipe was closed from the other end.");
            }

            int i = 0;
            message.Type = (MessageType)readBuffer[i];
            i += sizeof(byte);
            ulong payloadSize = BitConverter.ToUInt64(readBuffer, i);

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

        private Message waitForMessageType(MessageType messageType)
        {
            Message message = receiveMessage();

            if (message.Type != messageType)
                throw new Exception("Incorrect message type was received.");

            return (message);
        }

        #endregion
    }
}
