namespace ParasiteDriver
{
    public enum MessageType
    {
        Ping = 0x01,
        NoOp = 0x02,
        Pause = 0x03,
        RequestState = 0x04,
        State = 0x04
    }
}
