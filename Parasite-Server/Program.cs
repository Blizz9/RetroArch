using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parasite
{
    class Program
    {
        static void Main(string[] args)
        {
            NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

            Task.Factory.StartNew(() =>
            {
                namedPipeServerStream.WaitForConnection();

                byte[] readBuffer = new byte[9];
                while (true)
                {
                    int readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);

                    if (readByteCount > 0)
                    {
                        Console.WriteLine("Server bytes read");

                        byte command = readBuffer[0];
                        ulong size = BitConverter.ToUInt64(readBuffer, 1);

                        Console.WriteLine("Writing from server");
                        string writeString = "Sending" + size.ToString();
                        byte[] writeBuffer = ASCIIEncoding.ASCII.GetBytes(writeString);
                        namedPipeServerStream.Write(writeBuffer, 0, writeBuffer.Length);
                    }

                    Thread.Sleep(1);
                }
            });

            while (true) {}

            //NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "RetroArchParasite", PipeDirection.InOut);
            //namedPipeClientStream.Connect();

            //while (true)
            //{
            //    Thread.Sleep(1000);

            //    Console.WriteLine("Writing from client");
            //    byte[] writeBuffer = Enumerable.Repeat((byte)0x41, 1024).ToArray();
            //    namedPipeClientStream.Write(writeBuffer, 0, writeBuffer.Length);

            //    while (true)
            //    {
            //        byte[] readBuffer = new byte[1024];
            //        int readByteCount = namedPipeClientStream.Read(readBuffer, 0, readBuffer.Length);

            //        if (readByteCount > 0)
            //        {
            //            Console.WriteLine("Client bytes read");
            //            break;
            //        }

            //        Thread.Sleep(1);
            //    }
            //}

            //byte[] readBuffer = new byte[1024];

            //while (true)
            //{
            //    Console.WriteLine("Waiting for client pipe connection...");

            //    NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite");
            //    namedPipeServerStream.WaitForConnection();

            //    using (FileStream fileStream = new FileStream("../../../states/Super Mario Bros..state9", FileMode.Append))
            //    {
            //        int readByteCount;
            //        do
            //        {
            //            readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);
            //            Console.WriteLine("Read bytes: " + readByteCount);
            //            fileStream.Write(readBuffer, 0, readByteCount);
            //        } while (readByteCount != 0);
            //    }

            //    Console.WriteLine("Closing named pipe...");
            //    namedPipeServerStream.Close();

            //    Console.WriteLine("Reinitializing named pipe...");
            //}

            /*
            //byte[] writeBuffer = new byte[1024];
            byte[] writeBuffer = Enumerable.Repeat((byte)0x65, 1024).ToArray();

            NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream("RetroArchSend");
            namedPipeClientStream.Connect();
            namedPipeClientStream.Write(writeBuffer, 0, writeBuffer.Length);

            namedPipeClientStream.Close();
            */

            //using (NamedPipeClientStream namedPipeClient = new NamedPipeClientStream(".", "test-pipe", PipeDirection.InOut))
            //{
            //    namedPipeClient.Connect();
            //    Console.WriteLine("Client connected to the named pipe server. Waiting for server to send the first message...");
            //    namedPipeClient.ReadMode = PipeTransmissionMode.Message;
            //    string messageFromServer = ProcessSingleReceivedMessage(namedPipeClient);
            //    Console.WriteLine("The server is saying {0}", messageFromServer);
            //    Console.Write("Write a response: ");
            //    string response = Console.ReadLine();
            //    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            //    namedPipeClient.Write(responseBytes, 0, responseBytes.Length);
            //    while (response != "x")
            //    {
            //        messageFromServer = ProcessSingleReceivedMessage(namedPipeClient);
            //        Console.WriteLine("The server is saying {0}", messageFromServer);
            //        Console.Write("Write a response: ");
            //        response = Console.ReadLine();
            //        responseBytes = Encoding.UTF8.GetBytes(response);
            //        namedPipeClient.Write(responseBytes, 0, responseBytes.Length);
            //    }
            //}
        }
    }
}
