using System;
using System.Collections.Generic;
using System.Text;

namespace EtherDream.Net.Enums
{
    
    public enum LightEngineStateType
    {
        Ready = 0,
        Warmup = 1,
        Cooldown = 2,
        EmergencyStop = 3
    }

    //[0]: Emergency stop occurred due to E-Stop packet or invalid command.
    //[1]: Emergency stop occurred due to E-Stop input to projector.
    //[2]: Emergency stop input to projector is currently active.
    //[3]: Emergency stop occurred due to overtemperature condition.
    //[4]: Overtemperature condition is currently active.
    //[5]: Emergency stop occurred due to loss of Ethernet link.
    //[15:5]: Future use.

    public enum LightEngineState
    {
        EStop,
        EStop_Projector,
        EStop_Projector_Active,
        EStop_Temp,
        EStop_Temp_Active,
        EStop_Ethernet_Dropped,
        Ok
    }


    //public static class LightEngine
    //{
    //    // Short circuits on first E-Stop state
    //    public LightEngineState ParseState(ReadOnlySpan<byte> span)
    //    {
    //        foreach(var b in span)
    //        {
    //            if(b)
    //        }
    //        return LightEngineState.Ok;
    //    }
    //}

}
