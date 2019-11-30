using ParasiteLib;
using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace ParasiteDriver
{
    public class Driver
    {
        private NamedPipeServerStream _namedPipeServerStream;

        public event Action ContentLoaded;
        public event Action<long, byte[]> GameClock;

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
                                Communication.SendMessage(_namedPipeServerStream, new Message() { Type = MessageType.Clock, FrameCount = message.FrameCount });
                                break;

                            case MessageType.GameClock:
                                GameClockMessage gameClockMessage = (GameClockMessage)message;
                                Task.Factory.StartNew(() => GameClock?.Invoke(gameClockMessage.FrameCount, gameClockMessage.State));
                                // Debug.WriteLine("Game Clock: " + gameClockMessage.FrameCount + " | " + gameClockMessage.State[1938]);
                                Communication.SendMessage(_namedPipeServerStream, new Message() { Type = MessageType.GameClock, FrameCount = message.FrameCount });
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

        //#region Message Handlers

        //private void handleRequestScreen()
        //{
        //    Message message = new Message();
        //    message.Type = MessageType.RequestScreen;
        //    sendMessage(message);

        //    Message screenMessage = receiveMessage();

        //    int i = 0;
        //    uint raPixelFormat = BitConverter.ToUInt32(screenMessage.Payload, i);
        //    i += sizeof(uint);
        //    uint width = BitConverter.ToUInt32(screenMessage.Payload, i);
        //    i += sizeof(uint);
        //    uint height = BitConverter.ToUInt32(screenMessage.Payload, i);
        //    i += sizeof(uint);
        //    uint pitch = BitConverter.ToUInt32(screenMessage.Payload, i);
        //    i += sizeof(uint);

        //    byte[] screenPayload = new byte[height * pitch];
        //    Array.Copy(screenMessage.Payload, i, screenPayload, 0, (height * pitch));

        //    System.Windows.Media.PixelFormat pixelFormat;
        //    switch ((PixelFormat)raPixelFormat)
        //    {
        //        case PixelFormat.RGB1555:
        //            pixelFormat = PixelFormats.Bgr555;
        //            break;

        //        case PixelFormat.RGB565:
        //            pixelFormat = PixelFormats.Bgr565;
        //            break;

        //        case PixelFormat.XRGB8888:
        //            pixelFormat = PixelFormats.Bgra32;
        //            break;

        //        default:
        //            pixelFormat = PixelFormats.Bgra32;
        //            break;
        //    }

        //    if ((PixelFormat)raPixelFormat == PixelFormat.XRGB8888)
        //        // make all alpha bytes 0xFF since they aren't set properly
        //        for (i = 3; i < screenPayload.Length; i += 4)
        //            screenPayload[i] = 0xFF;

        //    BitmapSource screen = BitmapSource.Create((int)width, (int)height, 300, 300, pixelFormat, BitmapPalettes.Gray256, screenPayload, (int)pitch);

        //    using (FileStream fileStream = new FileStream("test.png", FileMode.Create))
        //    {
        //        BitmapEncoder encoder = new PngBitmapEncoder();
        //        encoder.Frames.Add(BitmapFrame.Create(screen));
        //        encoder.Save(fileStream);
        //    }
        //}

        //#endregion
    }
}
