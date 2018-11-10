using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace Parasite
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting up...");

            char[] readBuffer = new char[3];

            while (true)
            {
                NamedPipeServerStream namedPipeServerStream = new NamedPipeServerStream("RetroArchParasite");
                namedPipeServerStream.WaitForConnection();
                StreamReader streamReader = new StreamReader(namedPipeServerStream);

                Console.WriteLine("Waiting for data...");

                while (streamReader.Peek() >= 0)
                {
                    Console.WriteLine("Reading data...");
                    streamReader.ReadBlock(readBuffer, 0, readBuffer.Length);
                    Console.WriteLine(readBuffer);
                }

                Console.WriteLine("Closing named pipe...");
                streamReader.Close();
                namedPipeServerStream.Close();

                Console.WriteLine("Reinitializing named pipe...");
            }

            /*
            Console.WriteLine("HERE!");

            StartServer();
            Task.Delay(1000).Wait();


            ////Client
            //var client = new NamedPipeClientStream("LogPipe");
            //client.Connect();
            ////StreamReader reader = new StreamReader(client);
            //StreamWriter writer = new StreamWriter(client);

            while (true)
            {
                //string input = Console.ReadLine();
                //if (String.IsNullOrEmpty(input)) break;
                //writer.WriteLine(input);
                //writer.Flush();
                ////Console.WriteLine(reader.ReadLine());
            }
            */
        }

        static void StartServer()
        {
            Task.Factory.StartNew(() =>
            {
                var server = new NamedPipeServerStream("LogPipe");
                server.WaitForConnection();
                StreamReader reader = new StreamReader(server);
                //StreamWriter writer = new StreamWriter(server);
                while (true)
                {
                    char[] c = null;
                    c = new char[1];

                    reader.ReadBlock(c, 0, c.Length);

                    //if (((int)c[0]) != 0)
                        Console.WriteLine((int)c[0]);

                    //while (reader.Peek() >= 0)
                    //{
                    //    Console.WriteLine("HERE");
                    //    c = new char[5];
                    //    reader.ReadBlock(c, 0, c.Length);
                    //    Console.WriteLine(c);
                    //    //server.Flush();
                    //}
                    //var line = reader.ReadLine();
                    //Console.WriteLine(line);
                    //server.Flush();
                    //writer.WriteLine(String.Join("", line.Reverse()));
                    //writer.Flush();
                }
                //string input = Console.ReadLine();
            });
        }
    }
}
