using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;

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

                using (FileStream fileStream = new FileStream("test.state", FileMode.Append))
                {
                    int readByteCount;
                    do
                    {
                        readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);
                        Console.WriteLine("Read bytes: " + readByteCount);
                        fileStream.Write(readBuffer, 0, readByteCount);
                    } while (readByteCount != 0);
                }

                //while (true)
                //{
                //    int readByteCount = namedPipeServerStream.Read(readBuffer, 0, readBuffer.Length);
                //    if (readByteCount == 0)
                //    {
                //        Thread.Sleep(10);
                //    }
                //    else
                //    {
                //        Console.WriteLine("Bytes read!!!: " + readByteCount);
                //    }
                //}
                //namedPipeServerStream.IsMessageComplete

                //File.WriteAllBytes("test.state", readBuffer);
                //File.
                //StreamReader streamReader = new StreamReader(namedPipeServerStream);

                //Console.WriteLine("Waiting for data...");
                //while (streamReader.Peek() >= 0)
                //{
                //    Console.WriteLine("Reading block of data...");
                //    streamReader.ReadBlock(readBuffer, 0, readBuffer.Length);
                //    //Console.WriteLine(readBuffer);
                //}
                //Console.WriteLine("All data read.");

                //byte[] readBufferBytes = readBuffer.Select(c => (byte)c).ToArray();

                //File.WriteAllBytes("test.state", readBufferBytes);

                Console.WriteLine("Closing named pipe...");
                //streamReader.Close();
                namedPipeServerStream.Close();

                Console.WriteLine("Reinitializing named pipe...");
            }
        }
    }
}
