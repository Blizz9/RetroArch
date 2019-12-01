using System;

namespace ParasiteLib
{
    [Serializable]
    public class CommandMessage : Message
    {
        public CommandType CommandType;
        public string Arg0;
        public string Arg1;

        public CommandMessage()
        {
            Type = MessageType.Command;
        }
    }
}
