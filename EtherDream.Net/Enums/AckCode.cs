namespace LaserCore.EtherDream.Net.Enums
{
    public enum AckCode : byte
    {
        Ack = 0x61,
        NackBufferFull = 0x46,
        NackInvalid = 0x49,
        NackStop = 0x21,
        NackUnknown
    }
}
