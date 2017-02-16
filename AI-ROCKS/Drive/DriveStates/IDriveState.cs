using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive
{
    interface IDriveState
    {
        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        DriveCommand FindNextDriveCommand();

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        StateType GetNextStateType();

        /// <summary>
        /// Finds the best gap for a given Plot. This is normally in response to an ObstacleEvent being triggered and
        /// assumes there in an obstacle within the maximum acceptable distance. Checking does occur if this function 
        /// is erroneously called however. Note: This function returns null if no best gap is found.
        /// </summary>
        /// <param name="obstacles">The Plot containing Regions to analyze to find the best gap.</param>
        /// <returns>Line - the Line representing the best gap, or null if no sufficient best gap was found.</returns>
        Line FindBestGap(Plot obstacles);
    }
}
