using System;

namespace ParasiteLib
{
    [Serializable]
    public class ClockMessage : Message
    {
        public ClockMessage()
        {
            Type = MessageType.Clock;
        }
    }
}
