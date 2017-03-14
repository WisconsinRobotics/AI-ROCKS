using System;

using AI_ROCKS.Drive.Utils;

namespace AI_ROCKS.PacketHandlers
{
    class GPSHandler : PacketHandler
    {
        //private <?> path      // Current path driven by the cumulative GPS coordinates received, processed by the Ramer Douglass Peucker algorithm
        private GPS gps;


        public GPSHandler()
        {
            // TODO better way of doing this to avoid null pointers?
            this.gps = new GPS(0, 0, 0, 0, 0, 0);
        }


        /// <summary>
        /// Handles receiving a payload from ROCKS representing GPS data.
        /// </summary>
        /// <param name="opcode">Opcode for packet received from ROCKS.</param>
        /// <param name="payload">BCL packets received from ROCKS representing GPS data.</param>
        /// <returns>bool - Success of receiving and parsing payload into a GPS object.</returns>
        public bool HandlePacket(byte opcode, byte[] payload)
        {
            // Is opcode, payload valid (able to be made into GPS object). If no, return false

            // Form payload into GPS object, update current data
            GPS parsedGPS = BclPayloadToGPS(payload);
            this.gps = parsedGPS;

            //TODO return value
            return true;
        }

        /// <summary>
        /// Transform a GPS coordinate in the form of a BCL payload into the GPS object resulting from this payload.
        /// </summary>
        /// <param name="payload">The payload representing the GPS coordinate.</param>
        /// <returns>GPS - The GPS coordinate formed from the BCL payload.</returns>
        private GPS BclPayloadToGPS(byte[] payload)
        {
            //TODO validity checking
            
            // BCL packet -> GPS object
            short latDegrees = (short)((payload[0] << 8) | (payload[1]));
            short latMinutes = (short)((payload[2] << 8) | (payload[3]));
            short latSeconds = (short)((payload[4] << 8) | (payload[5]));
            short longDegrees = (short)((payload[6] << 8) | (payload[7]));
            short longMinutes = (short)((payload[8] << 8) | (payload[9]));
            short longSeconds = (short)((payload[10] << 8) | (payload[11]));

            GPS parsedGPS = new GPS(latDegrees, latMinutes, latSeconds, longDegrees, longMinutes, longSeconds);
            return parsedGPS;
        }

        /// <summary>
        /// Property for the current GPS data.
        /// </summary>
        public GPS Data
        {
            get { return this.gps; }
        }
    }
}
