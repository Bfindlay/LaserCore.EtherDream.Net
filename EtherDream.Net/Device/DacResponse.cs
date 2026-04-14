using System;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;
using LaserCore.EtherDream.Net.Enums;

namespace LaserCore.EtherDream.Net.Device
{
    
    public static class DacResponse
    {
        public static DacResponseDto ParseDacResponse(byte[] bytes)
        {
            if (bytes.Length != 22)
            {
                throw new Exception($"Response expected to be 22 bytes but was {bytes.Length}");
            }

            var status = DacStatus.ParseDacStatus(bytes[2..]);
            var responseDto = new DacResponseDto()
            {
                Response = bytes[0],
                Command = bytes[1],
                DacStatus = status
            };

            return responseDto;
        }

        public static DacResponseDto DeserializeResponse(byte[] param)
        {
            Span<byte> bytes = param;
            DacResponseDto response = MemoryMarshal.Cast<byte, DacResponseDto>(bytes)[0];
            return response;
        }

        public static AckCode ParseAckCode(byte ack)
        {
            switch (ack)
            {
                case 0x61:
                    return AckCode.Ack;
                case 0x46:
                    return AckCode.NackBufferFull;
                case 0x49:
                    return AckCode.NackInvalid;
                case 0x21:
                    return AckCode.NackStop;
                default:
                    return AckCode.NackUnknown;

            }
        }
    }

}