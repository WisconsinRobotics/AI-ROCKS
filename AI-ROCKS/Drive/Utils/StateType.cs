using System;

using AI_ROCKS.Drive.DriveStates;

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
        public static IDriveState ToDriveState(StateType stateType)
        {
            IDriveState driveState;
            switch (stateType)
            {
                case StateType.GPSState:
                    {
                        driveState = new GPSDriveState();
                        break;
                    }
                case StateType.VisionState:
                    {
                        driveState = new VisionDriveState();
                        break;
                    }
                case StateType.ObstacleAvoidanceState:
                    {
                        driveState = new ObstacleAvoidanceDriveState();
                        break;
                    }
                default:
                    {
                        driveState = new GPSDriveState();
                        break;
                    }
            }

            return driveState;
        }
    }
}
