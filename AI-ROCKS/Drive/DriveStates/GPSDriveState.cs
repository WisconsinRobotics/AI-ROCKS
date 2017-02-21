using System;

using ObstacleLibrarySharp;
using LRFLibrary;

namespace AI_ROCKS.Drive.DriveStates
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
        public Line FindBestGap(Plot obstacles)
        {
            // Given a Plot representing the obstacles, find Line representing the best gap.

            // Do GPS driving according to our rendition of the "Follow the Gap" algorithm:
            // Reference here: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
            //
            // This algorithm avoids obstacles by finding the gap between them. It has a threshold gap (i.e. robot width),
            // and if the measured gap is greater than the threshold gap, the robot follows the calculated gap angle. 
            // In our case, the best gap will also be the one with the smallest displacement from the goal (the gate).
            //
            // 1) Get LRF, GPS data 
            // 2) Calculate valid (large enough) gaps as Line objects, store in a list
            // 3) Find which gap is "best" (gap center angle has smallest deviation from straight line to goal)
            // 4) Find heading angle (actual angle to more according to combination of gap center and goal angles)
            // 5) Make DriveCommand for this angle and speed, return it

            return null;
        }
    }
}
