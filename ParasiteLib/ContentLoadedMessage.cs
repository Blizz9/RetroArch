using System;

namespace ParasiteLib
{
    [Serializable]
    public class ContentLoadedMessage : Message
    {
        public string ContentPath;
        public string CoreName;
        public string CoreVersion;

        public ContentLoadedMessage()
        {
            Type = MessageType.ContentLoaded;
        }
    }
}
