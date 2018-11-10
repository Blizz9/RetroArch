using System;
using System.IO;
using System.IO.Pipes;

namespace Parasite
{
    class Program
    {
        static void Main(string[] args)
        {
            char[] readBuffer = new char[1024];

            while (true)
            {
                Console.WriteLine("Waiting for client pipe connection...");

                NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite");
                namedPipeServerStream.WaitForConnection();
                StreamReader streamReader = new StreamReader(namedPipeServerStream);

                Console.WriteLine("Waiting for data...");
                while (streamReader.Peek() >= 0)
                {
                    Console.WriteLine("Reading block of data...");
                    streamReader.ReadBlock(readBuffer, 0, readBuffer.Length);
                    Console.WriteLine(readBuffer);
                }
                Console.WriteLine("All data read.");

                Console.WriteLine("Closing named pipe...");
                streamReader.Close();
                namedPipeServerStream.Close();

                Console.WriteLine("Reinitializing named pipe...");
            }
        }
    }
}
