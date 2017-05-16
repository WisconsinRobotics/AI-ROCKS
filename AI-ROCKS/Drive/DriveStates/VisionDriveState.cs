using System;

using AI_ROCKS.Drive.Models;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class VisionDriveState : IDriveState
    {
        public VisionDriveState(GPS gate)
        {    
        }

        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
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

        //TODO event typing into some new ObstacleAvoidanceDriveState that triggers when the robot needs to avoid an obstacle
        public Line FindBestGap(Plot obstacles)
        {
            // Given a Plot representing the obstacles, find Line representing the best gap.

            // Refer to GPSDriveState's psuedo code. Do we want to account for detecting the gate as a Region 
            // and actually heading toward it, rather than avoiding it?

            // TODO Vision group's algorithm here

            return null;
        }
    }
}
