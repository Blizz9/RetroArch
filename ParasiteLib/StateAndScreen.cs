using System;

namespace ParasiteLib
{
    [Serializable]
    public class StateAndScreenMessage : Message
    {
        public byte[] State;
        public PixelFormat PixelFormat;
        public int Width;
        public int Height;
        public int Pitch;
        public byte[] Screen;
    }
}
