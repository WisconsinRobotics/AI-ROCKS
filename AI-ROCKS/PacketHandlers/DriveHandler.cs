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


        public bool SendDriveCommand(DriveCommand driveCommand)
        {
            byte[] bclPacket = DriveCommandToBCLPacket(driveCommand);

            // Send packet to ROCKS
            // Return result (success, failure)

            return true;
        }

        private byte[] DriveCommandToBCLPacket(DriveCommand driveCommand)
        {
            // Convert DriveCommand to byte array BCL packet
            return new byte[0];
        }
    }
}
