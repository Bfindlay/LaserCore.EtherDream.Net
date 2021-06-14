using System.Runtime.InteropServices;

namespace LaserCore.Etherdream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DacStatusDto
    {

        public byte Protocol;
        /**
         * The light engine is one of three state machines in the DAC.
         *
         * The states are:
         *
         *  - 0: Ready.
         *  - 1: Warmup. In the case where the DAC is also used for thermal
         *       control of laser apparatus, this is the state that is
         *       entered after power-up.
         *  - 2: Cooldown. Lasers are off but thermal control is still active
         *  - 3: Emergency stop. An emergency stop has been triggered, either
         *       by an E-stop input on the DAC, an E-stop command over the
         *       network, or a fault such as over-temperature.
         *
         *  (Since thermal control is not implemented yet, it is not defined
         *  how transitions to and from the "Warmup" and "Cooldown" states
         *  occur.)
         */
        public byte LightEngineState;
        /**
         * The playback_state is one of three state machines in the DAC.
         * It reports the state of the playback system.
         *
         * The DAC has one playback system, which buffers data and sends it
         * to the analog output hardware at its current point rate. At any
         * given time, the playback system is connected to a source. Usually,
         * the source is the network streamer, which uses the protocol
         * described in this document; however, other sources exist, such as
         * a built-in abstract generator and file playback from SD card. The
         * playback system is in one of the following states:
         *
         *   - 0: Idle. This is the default state. No points may be added to
         *        the buffer. No output is generated; all analog outputs are
         *        at 0v, and the shutter is controlled by the data source.
         *   - 1: Prepared. The buffer will accept points. The output is the
         *        same as in the Idle state.
         *   - 2: Playing. Points are being sent to the output.
         *
         * See playback_flags for additional information.
         */
        public byte PlayBackState;
        /**
         * The currently-selected data source is specified in the source field:
         *
         *   - 0: Network streaming (the protocol defined in the rest of this
         *        document).
         *   - 1: ILDA playback from SD card.
         *   - 2: Internal abstract generator.
         */
        public byte Source;
        /**
         * The light_engine_state field gives the current state of the light
         * engine. If the light engine is Ready, light_engine_flags will be 0.
         * Otherwise, bits in light_engine_flags will be set as follows:
         *
         * [0]: Emergency stop occurred due to E-Stop packet or invalid
         *      command.
         * [1]: Emergency stop occurred due to E-Stop input to projector.
         * [2]: Emergency stop input to projector is currently active.
         * [3]: Emergency stop occurred due to overtemperature condition.
         * [4]: Overtemperature condition is currently active.
         * [5]: Emergency stop occurred due to loss of Ethernet link.
         * [15:5]: Future use.
         */
        public ushort LightEngineFlags;
        /**
         * The playback_flags field may be nonzero during normal operation.
         * Its bits are defined as follows:
         *
         * [0]: Shutter state: 0 = closed, 1 = open.

       *[1]: Underflow. 1 if the last stream ended with underflow, rather
         *      than a Stop command.Reset to zero by the Prepare command.
         * [2]: E-Stop. 1 if the last stream ended because the E-Stop state
         *      was entered. Reset to zero by the Prepare command.
         */
        public ushort PlaybackFlags;
        /// TODO: Undocumented?
        public ushort SourceFlags;

        /** Reports the number of points currently buffered. */
        public ushort BufferFullness;
        /**
         * The number of points per second for which the DAC is configured
         * (if Prepared or Playing), or zero if the DAC is idle.
         */
        public uint PointRate;
        /**
         * The number of points that the DAC has actually emitted since it
         * started playing (if Playing), or zero (if Prepared or Idle).
         */
        public uint PointCount;
    }
}