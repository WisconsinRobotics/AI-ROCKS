using System;

using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.PacketHandlers
{
    class GPSHandler : IPacketHandler
    {
        private GPS gps;
        private GPS receivedGate;


        public GPSHandler()
        {
            // Initialize to avoid null pointers
            this.gps = new GPS(0, 0, 0, 0, 0, 0);

            this.receivedGate = null;
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

            switch (opcode)
            {
                case AscentPacketHandler.OPCODE_REPORT_GPS:
                {
                    // Form payload into GPS object, update current data
                    GPS parsedGPS = BclPayloadToGPS(payload);
                    this.gps = parsedGPS;

                    break;
                }
                case AscentPacketHandler.OPCODE_SET_GPS:
                {
                    GPS parsedGPS = BclPayloadToGPS(payload);
                    this.receivedGate = parsedGPS;

                    break;
                }
            }

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
            float latDegrees = (payload[0] << 8) | (payload[1]);
            float latMinutes = (payload[2] << 8) | (payload[3]);
            float latSeconds = (payload[4] << 8) | (payload[5]);       // Divide by 10 since it's sent as fixed-point
            latSeconds = latSeconds / 10;
            float longDegrees = (payload[6] << 8) | (payload[7]);      // Always negative where we operate so it's sent as positive - we make it negative here
            longDegrees = -1 * longDegrees;
            float longMinutes = (payload[8] << 8) | (payload[9]);
            float longSeconds = (payload[10] << 8) | (payload[11]);    // Divide by 10 since it's sent as fixed-point
            longSeconds = longSeconds / 10;

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
        
        /// <summary>
        /// Property for the GPS coordinates of the gate.
        /// </summary>
        public GPS Gate
        {
            get { return this.receivedGate; }
        }
    }
}
