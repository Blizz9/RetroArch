using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ParasiteDriver
{
    public class Driver
    {
        public volatile bool SendPauseToggle;
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
                NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

                Debug.WriteLine("Waiting for other end of pipe to connect...");
                namedPipeServerStream.WaitForConnection();
                Debug.WriteLine("connected.");

                while (true)
                {
                    try
                    {
                        waitForMessageType(namedPipeServerStream, MessageType.Ping);

                        Message message = new Message();

                        if (SendPauseToggle)
                        {
                            SendPauseToggle = false;
                            message.Type = MessageType.PauseToggle;
                            sendMessage(namedPipeServerStream, message);
                        }
                        else if (RequestState)
                        {
                            RequestState = false;
                            message.Type = MessageType.RequestState;
                            sendMessage(namedPipeServerStream, message);
                            Message stateMessage = receiveMessage(namedPipeServerStream);
                            File.WriteAllBytes("test.state", stateMessage.Payload);
                        }
                        else if (RequestScreen)
                        {
                            RequestScreen = false;
                            message.Type = MessageType.RequestScreen;
                            sendMessage(namedPipeServerStream, message);
                            Message screenMessage = receiveMessage(namedPipeServerStream);

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
                        else
                        {
                            //message.Type = MessageType.Test;
                            //message.Payload = BitConverter.GetBytes((uint)112233);
                            message.Type = MessageType.Pong;
                            sendMessage(namedPipeServerStream, message);
                        }

                        Thread.Sleep(1);
                    }
                    catch (NamedPipeClosedException)
                    {
                        break;
                    }
                }
            }
        }

        #region Communication Routines

        private Message receiveMessage(NamedPipeServerStream namedPipeServerStream)
        {
            Message message = new Message();

            byte[] readBuffer = new byte[sizeof(byte) + sizeof(ulong)];
            int readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);

            if (readByteCount == 0)
            {
                namedPipeServerStream.Close();
                throw new NamedPipeClosedException("Incorrect message type was received, likely the named pipe was closed from the other end.");
            }

            int i = 0;
            message.Type = (MessageType)readBuffer[i];
            i += sizeof(byte);
            ulong payloadSize = BitConverter.ToUInt64(readBuffer, i);

            if (payloadSize > 0)
            {
                message.Payload = new byte[payloadSize];
                readByteCount = namedPipeServerStream.Read(message.Payload, 0, (int)payloadSize);

                if (readByteCount == 0)
                    throw new Exception("Unable to read message payload properly.");
            }

            return (message);
        }

        private void sendMessage(NamedPipeServerStream namedPipeServerStream, Message message)
        {
            byte[] writeBuffer = new byte[9 + message.Payload.Length];

            writeBuffer[0] = (byte)message.Type;
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)message.Payload.Length), 0, writeBuffer, 1, sizeof(ulong));

            if (message.Payload.Length > 0)
                Buffer.BlockCopy(message.Payload, 0, writeBuffer, 9, message.Payload.Length);

            namedPipeServerStream.Write(writeBuffer, 0, writeBuffer.Length);
        }

        private void waitForMessageType(NamedPipeServerStream namedPipeServerStream, MessageType messageType)
        {
            Message message = receiveMessage(namedPipeServerStream);

            if (message.Type != messageType)
                throw new Exception("Incorrect message type was received.");
        }

        #endregion
    }
}
