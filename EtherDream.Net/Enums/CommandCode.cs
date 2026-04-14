namespace LaserCore.EtherDream.Net.Enums
{
    public enum CommandCodeType : byte
    {
        Begin = 0x62,
        Data = 0x64,
        Ping = 0x3F,
        Prepare = 0x70,
        Unknown
    }

    public static class CommandCode
    {
        public static CommandCodeType ParseCommandCode(byte cmd) => cmd switch
        {
            0x62 => CommandCodeType.Begin,
            0x64 => CommandCodeType.Data,
            0x3F => CommandCodeType.Ping,
            0x70 => CommandCodeType.Prepare,
            _ => CommandCodeType.Unknown,
        };
    }
}
