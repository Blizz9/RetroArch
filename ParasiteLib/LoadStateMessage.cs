using System;

namespace ParasiteLib
{
    [Serializable]
    public class LoadStateMessage : Message
    {
        public byte[] State;

        public LoadStateMessage()
        {
            Type = MessageType.LoadState;
        }
    }
}
