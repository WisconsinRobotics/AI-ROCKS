using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.Drive;

namespace AI_ROCKS.PacketHandlers
{
    class GPSHandler
    {
        // TODO use ObstacleLibrary
        //private <?> path      // Current path driven by the cumulative GPS coordinates received, processed by the Ramer Douglass Peucker algorithm
        private GPS gps;


        public GPSHandler()
        {   
        }


        /// <summary>
        /// Send a GPS coordinate to ROCKS.
        /// //TODO need? Send right to base station? This is mostly just here for continuity's sake
        /// </summary>
        /// <param name="gps">GPS - the GPS coordinate we are sending.</param>
        /// <returns>Success or failure of send.</returns>
        public bool SendGPSCoordinate(GPS gps)
        {
            // GPS object -> BCL packet
            
            // Do sending to ROCKS

            // Return result (true, false)
            return true;
        }

        /// <summary>
        /// Receive a GPS coordinate from ROCKS in the form of a BCL packet. Return the GPS object resulting from this packet.
        /// </summary>
        /// <param name="bclPackets">BCL packets received from ROCKS.</param>
        /// <returns>GPS object created from parsing the BCL packets into a GPS object.</returns>
        public GPS ReceiveGPSCoordinates(byte[] bclPackets)
        {
            // Receive packets from BCL, convert to GPS object, return this object
            GPS gps = BCLPacketToGPS(bclPackets);

            this.gps = gps;

            return gps;
        }

        private GPS BCLPacketToGPS(byte[] bclPacket)
        {
            // BCL packet -> GPS object
            return null;
        }

        public GPS Data
        {
            get { return this.gps; }
            private set { this.gps = value; }
        }

    }
}
