using System;

namespace ParasiteLib
{
    [Serializable]
    public class Message
    {
        public MessageType Type;
        public long FrameCount;
    }
}
