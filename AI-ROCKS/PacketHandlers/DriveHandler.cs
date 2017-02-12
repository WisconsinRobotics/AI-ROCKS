using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.Drive;

namespace AI_ROCKS.PacketHandlers
{
    class DriveHandler
    {
        public DriveHandler()
        {
        }


        /// <summary>
        /// Send a given DriveCommand to ROCKS.
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        /// <returns>bool - If DriveCommand was successfully sent.</returns>
        public static bool SendDriveCommand(DriveCommand driveCommand)
        {
            // Form payload for BCL drive command from specified DriveCommand
            byte[] bclPayload = DriveCommandToBclPayload(driveCommand);

            // Send opcode, payload to AscentShimLayer to send drive packet to ROCKS
            
            // Return result (success, failure)

            return true;
        }

        /// <summary>
        /// Convert a DriveCommand to a BCL payload in a byte array.
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        /// <returns>byte[] - Byte array representing a BCL payload.</returns>
        private static byte[] DriveCommandToBclPayload(DriveCommand driveCommand)
        {
            // Convert DriveCommand to byte array BCL packet

            // Return BCL packet
            return new byte[0];
        }
    }
}
