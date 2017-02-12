using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.PacketHandlers;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive
{
    class GPSDriveState : IDriveState
    {
        
        public GPSDriveState()
        {

        }


        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            
            // Get "current" data from AscentShimLayer in form of GPS object
            
            // Do GPS driving (get from GPS group):
            // ..
            // ..

            // Form DriveCommand for where to drive the robot

            // Return Drive Command (it is sent by DriveContext)
                        
            return null;
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            // Logic for finding when state needs to be switched from GPSDriveState to VisionDriveState
            return 0;
        }

        //TODO event typing into some new ObstacleAvoidanceDriveState that triggers when the robot needs to avoid an obstacle
        public Region FindBestGap(Plot obstacles)
        {
            // Given a Plot representing the obstacles, find Region representing the best gap.

            // TODO GPS group's algorithm here

            return null;
        }
    }
}
