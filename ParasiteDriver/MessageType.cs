namespace ParasiteDriver
{
    public enum MessageType
    {
        Ping = 0x01,
        Pong = 0x02,
        Pause = 0x03,
        RequestState = 0x04,
        State = 0x05,
        RequestScreen = 0x06,
        Screen = 0x07
    }
}
