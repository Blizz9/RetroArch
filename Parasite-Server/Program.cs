using System;
using System.IO.Pipes;
using System.Threading;

namespace Parasite
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Parasite started.");

            NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite", PipeDirection.InOut, 1, PipeTransmissionMode.Byte);

            Console.Write("Waiting for other end of pipe to connect...");
            namedPipeServerStream.WaitForConnection();
            Console.WriteLine("connected.");

            int frame = 0;
            while (true)
            {
                //Console.Write("Waiting to receive a command...");

                byte[] readCommandBuffer = new byte[9];
                int readByteCount = namedPipeServerStream.Read(readCommandBuffer, 0, readCommandBuffer.Length);

                if (readByteCount > 0)
                {
                    //Console.WriteLine("command received.");
                    Console.Write(".");
                    frame++;

                    byte command = readCommandBuffer[0];
                    ulong size = BitConverter.ToUInt64(readCommandBuffer, 1);

                    //Console.WriteLine(string.Format("Command: [0x{0}][{1}].", command.ToString("X2"), size.ToString()));

                    if (command == 0x01)
                    {
                        //Console.WriteLine("Replying to RetroArch with new command.");
                        byte[] writeCommandBuffer = new byte[9];

                        if (frame % 300 == 299)
                        {
                            writeCommandBuffer[0] = 0x03;
                        }
                        else
                        {
                            writeCommandBuffer[0] = 0x02;
                        }
                        Buffer.BlockCopy(BitConverter.GetBytes((ulong)0), 0, writeCommandBuffer, 1, sizeof(ulong));
                        //Buffer.BlockCopy(BitConverter.GetBytes((ulong)12345), 0, writeCommandBuffer, 1, sizeof(ulong));
                        namedPipeServerStream.Write(writeCommandBuffer, 0, writeCommandBuffer.Length);
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}
