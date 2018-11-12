using System;
using System.IO;
using System.IO.Pipes;

namespace Parasite
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] readBuffer = new byte[1024];

            while (true)
            {
                Console.WriteLine("Waiting for client pipe connection...");

                NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite");
                namedPipeServerStream.WaitForConnection();

                using (FileStream fileStream = new FileStream("../../../states/Super Mario Bros..state9", FileMode.Append))
                {
                    int readByteCount;
                    do
                    {
                        readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);
                        Console.WriteLine("Read bytes: " + readByteCount);
                        fileStream.Write(readBuffer, 0, readByteCount);
                    } while (readByteCount != 0);
                }

                Console.WriteLine("Closing named pipe...");
                namedPipeServerStream.Close();

                Console.WriteLine("Reinitializing named pipe...");
            }
        }
    }
}
