using System;

namespace ParasiteLib
{
    [Serializable]
    public class Message
    {
        public MessageType Type;
        public long ClockCount;
        public long FrameCount;
    }
}
