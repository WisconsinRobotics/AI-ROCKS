using System;

using AI_ROCKS.Drive.Utils;

namespace AI_ROCKS.PacketHandlers
{
    class IMUHandler : PacketHandler
    {
        private IMU imu;


        public IMUHandler()
        {
        }


        /// <summary>
        /// Handles receiving a payload from ROCKS representing IMU data.
        /// </summary>
        /// <param name="opcode">Opcode for packet received from ROCKS.</param>
        /// <param name="payload">BCL packets received from ROCKS representing IMU data.</param>
        /// <returns>bool - Success of receiving and parsing payload into a IMU object.</returns>
        public bool HandlePacket(byte opcode, byte[] payload)
        {
            // Is opcode, payload valid (able to be made into IMU object). If no, return false

            // Form payload into IMU object, update current data
            IMU parsedIMU = BclPayloadToIMU(payload);
            this.imu = parsedIMU;

            // TODO return value
            return true;
        }

        /// <summary>
        /// Transform IMU data in the form of a BCL payload into the IMU object resulting from this payload.
        /// </summary>
        /// <param name="payload">The payload representing the IMU data.</param>
        /// <returns>GPS - The IMU data formed from the BCL payload.</returns>
        private IMU BclPayloadToIMU(byte[] payload)
        {
            // TODO validity checking

            // BCL packet -> IMU object
            short xAccel = (short)((payload[0] << 8) | payload[1]);
            short yAccel = (short)((payload[2] << 8) | payload[3]);
            short zAccel = (short)((payload[4] << 8) | payload[5]);
            short xOrient = (short)((payload[6] << 8) | payload[7]);
            short yOrient = (short)((payload[8] << 8) | payload[9]);
            short zOrient = (short)((payload[10] << 8) | payload[11]);

            IMU parsedIMU = new IMU(xAccel, yAccel, zAccel, xOrient, yOrient, zOrient);

            return parsedIMU;
        }

        /// <summary>
        /// Property for the current IMU data.
        /// </summary>
        public IMU Data
        {
            get { return this.imu; }
        }

        /// <summary>
        /// Property for the current compass data. This is ZOrient of the current IMU data.
        /// </summary>
        public short Compass
        {
            get { return this.imu.ZOrient; }
        }
    }
}
