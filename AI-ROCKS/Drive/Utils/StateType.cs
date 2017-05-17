using System;

using AI_ROCKS.Drive.DriveStates;
using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.Drive
{
    enum StateType
    {
        GPSState = 0,
        VisionState = 1,
        ObstacleAvoidanceState = 2
    }

    class StateTypeHelper
    {
        /// <summary>
        /// Convert a StateType to its corresponding DriveState, which extends IDriveState.
        /// Return a new instance of the DriveState.
        /// </summary>
        /// <param name="stateType">The StateType being converted.</param>
        /// <returns>IDriveState - a new instance of the DriveState corresponding to the 
        /// StateType specified.</returns>
        public static IDriveState ToDriveState(StateType stateType, GPS gate)
        {
            IDriveState driveState;
            switch (stateType)
            {
                case StateType.GPSState:
                {
                    driveState = new GPSDriveState(gate);
                    break;
                }
                case StateType.VisionState:
                {
                    driveState = new VisionDriveState(gate);
                    break;
                }
                case StateType.ObstacleAvoidanceState:
                {
                    driveState = new ObstacleAvoidanceDriveState();
                    break;
                }
                default:
                {
                    driveState = new GPSDriveState(gate);
                    break;
                }
            }

            return driveState;
        }

        /// <summary>
        /// Convert a specified int to its corresponding StateType value.
        /// </summary>
        /// <throws>ArgumentException - if specified state does not convert to a StateType value.</throws>
        /// <param name="state">int being converted into a StateType</param>
        /// <returns>StateType - the StateType resulting from the specified int.</returns>
        public static StateType FromInteger(int state)
        {
            if (!Enum.IsDefined(typeof(StateType), state))
            {
                throw new ArgumentException("Invalid state number for StateType conversion: " + state);
            }

            String name = Enum.GetName(typeof(StateType), state);
            StateType stateType = (StateType)Enum.Parse(typeof(StateType), name);

            return stateType;
        }
    }
}
