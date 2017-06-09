using System;

using AI_ROCKS.Drive;

namespace AI_ROCKS.PacketHandlers
{
    class DriveHandler : IPacketHandler
    {
        private const byte OPCODE_ALL_WHEEL_SPEED = 0x2E;

        public DriveHandler()
        {
        }


        /// <summary>
        /// Send a given DriveCommand to ROCKS.
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        /// <returns>bool - If DriveCommand was successfully sent.</returns>
        public static void SendDriveCommand(DriveCommand driveCommand)
        {
            // Form payload for BCL drive command from specified DriveCommand
            byte[] bclPayload = DriveCommandToBclPayload(driveCommand);

            // Send opcode, payload to AscentPacketHandler to send drive packet to ROCKS
            AscentPacketHandler.SendPayloadToROCKS(OPCODE_ALL_WHEEL_SPEED, bclPayload, AscentPacketHandler.ROCKS_AI_SERVICE_ID);

            // For debugging
            Console.WriteLine("left: " + (sbyte)driveCommand.Left + " | right: " + (sbyte)driveCommand.Right);
        }

        /// <summary>
        /// Convert a DriveCommand to a BCL payload in a byte array.
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        /// <returns>byte[] - Byte array representing a BCL payload.</returns>
        private static byte[] DriveCommandToBclPayload(DriveCommand driveCommand)
        {
            // Convert DriveCommand to byte array BCL packet
            byte[] payload = { (byte)driveCommand.Left, (byte)driveCommand.Right };
            
            return payload;
        }


        /// <summary>
        /// Handles receiving a payload from ROCKS representing a DriveCommand.
        /// </summary>
        /// <param name="opcode">Opcode for packet received from ROCKS.</param>
        /// <param name="payload">BCL packets received from ROCKS representing a DriveCommand.</param>
        /// <returns>bool - Success of receiving and parsing payload into a DriveCommand.</returns>
        public bool HandlePacket(byte opcode, byte[] payload)
        {
            // Will this ever be used? 
            // If we do vision processing on the base station, this may be useful. Keep just in case?

            throw new NotImplementedException();
        }
    }
}
