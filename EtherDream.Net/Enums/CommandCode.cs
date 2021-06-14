namespace LaserCore.Etherdream.Net.Enums
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
        public static CommandCodeType ParseCommandCode(byte cmd)
        {
            switch (cmd)
            {
                case 0x62:
                    return CommandCodeType.Begin;
                case 0x64:
                    return CommandCodeType.Data;
                case 0x3F:
                    return CommandCodeType.Ping;
                case 0x70:
                    return CommandCodeType.Prepare;
                default:
                    return CommandCodeType.Unknown;
            }
        }
    }
}
