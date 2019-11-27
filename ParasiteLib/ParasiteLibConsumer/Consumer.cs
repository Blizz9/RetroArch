using ParasiteLib;
using System;

namespace ParasiteLibConsumer
{
    public class Consumer
    {
        public static void Main(string[] args)
        {
            Lib.Init();
            string logMessage = Lib.ConsumeLogMessage();
            Console.WriteLine(logMessage);
        }
    }
}
