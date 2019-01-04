namespace ParasiteDriver
{
    public class Message
    {
        public MessageType Type;
        public byte[] Payload;

        public Message()
        {
            Payload = new byte[0];
        }
    }
}
