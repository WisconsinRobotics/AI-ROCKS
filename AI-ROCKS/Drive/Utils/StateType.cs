using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.Drive;

namespace AI_ROCKS.Drive
{
    enum StateType
    {
        GPSState = 0,
        VisionState = 1
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
