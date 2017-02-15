using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AI_ROCKS.Drive;

namespace AI_ROCKS.PacketHandlers
{
    class GPSHandler : PacketHandler
    {
        // TODO use ObstacleLibrary
        //private <?> path      // Current path driven by the cumulative GPS coordinates received, processed by the Ramer Douglass Peucker algorithm
        private GPS gps;


        public GPSHandler()
        {   
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

            // Make payload into GPS object
            GPS parsedGPS = BclPayloadToGPS(payload);

            // Set current data to GPS object
            this.Data = parsedGPS;

            // Return success or not
            return true;
        }

        /// <summary>
        /// Transform a GPS coordinate in the form of a BCL payload into the GPS object resulting from this payload.
        /// </summary>
        /// <param name="payload">The payload representing the GPS coordinate.</param>
        /// <returns>GPS - The GPS coordinate formed from the BCL payload.</returns>
        private GPS BclPayloadToGPS(byte[] payload)
        {
            // BCL packet -> GPS object
            return null;
        }

        /// <summary>
        /// Property for the current GPS data.
        /// </summary>
        public GPS Data
        {
            get { return this.gps; }
            private set { this.gps = value; }
        }
    }
}
