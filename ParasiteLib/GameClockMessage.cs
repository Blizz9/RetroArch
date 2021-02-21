using System;

namespace ParasiteLib
{
    [Serializable]
    public class GameClockMessage : Message
    {
        public byte[] State;
        public PixelFormat PixelFormat;
        public int Width;
        public int Height;
        public int Pitch;
        public byte[] Screen;

        public GameClockMessage()
        {
            Type = MessageType.GameClock;
        }
    }
}
