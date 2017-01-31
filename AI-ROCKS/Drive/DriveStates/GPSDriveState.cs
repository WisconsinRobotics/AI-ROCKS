using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.PacketHandlers;

namespace AI_ROCKS.Drive
{
    class GPSDriveState : IDriveState
    {
        private GPSHandler gpsHandler;


        public GPSDriveState()
        {
            this.gpsHandler = new GPSHandler();
        }


        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            GPS gps = gpsHandler.Data;
            gpsHandler.SendGPSCoordinate(gps);
            return null;
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            // Logic for finding when state needs to be switched from VisionDriveState to GPSDriveState
            return 0;
        }
    }
}
