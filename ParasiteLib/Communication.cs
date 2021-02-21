using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;

namespace ParasiteLib
{
    public class Communication
    {
        public static void SendMessage(PipeStream pipeStream, Message message)
        {
            byte[] messageBytes;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, message);
                messageBytes = memoryStream.ToArray();
            }

            byte[] pipeBuffer = new byte[sizeof(int) + messageBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(messageBytes.Length), 0, pipeBuffer, 0, sizeof(int));
            Buffer.BlockCopy(messageBytes, 0, pipeBuffer, sizeof(int), messageBytes.Length);

            pipeStream.Write(pipeBuffer, 0, pipeBuffer.Length);
        }

        public static Message ReceiveMessage(PipeStream pipeStream)
        {
            byte[] readBuffer = new byte[sizeof(int)];
            int readByteCount = pipeStream.Read(readBuffer, 0, readBuffer.Length);

            int messageSize = BitConverter.ToInt32(readBuffer, 0);

            readBuffer = new byte[messageSize];
            readByteCount = pipeStream.Read(readBuffer, 0, messageSize);

            Message message;
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(readBuffer))
            {
                message = (Message)binaryFormatter.Deserialize(memoryStream);
            }

            return message;
        }
    }
}
