using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class ObstacleAvoidanceDriveState : IDriveState
    {
        public ObstacleAvoidanceDriveState()
        {
            
        }


        public DriveCommand FindNextDriveCommand()
        {
            return DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED);
        }

        public StateType GetNextStateType()
        {
            return StateType.ObstacleAvoidanceState;
        }

        public Line FindBestGap(Plot obstacles)
        {
            //TODO implementation

            throw new NotImplementedException();
        }
    }
}
