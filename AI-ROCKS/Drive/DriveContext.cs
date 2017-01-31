using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.PacketHandlers;

namespace AI_ROCKS.Drive
{
    class DriveContext
    {
        private IDriveState driveState;
        private StateType stateType;
        private DriveHandler driveHandler;


        public DriveContext()
        {
            // GPSDriveState is default 
            this.driveState = new GPSDriveState();
            this.driveHandler = new DriveHandler();
        }


        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS according to the current DriveState's implementation.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            return this.driveState.FindNextDriveCommand();
        }

        /// <summary>
        /// Issue the specified DriveCommand to ROCKS using DriveHandler
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        public void Drive(DriveCommand driveCommand)
        {
            driveHandler.SendDriveCommand(driveCommand);
            //this.driveState.Drive(driveCommand);
        }

        /// <summary>
        /// If the current DriveState has determined that the state must be changed.
        /// </summary>
        /// <returns>bool - If state change is required.</returns>
        public bool IsStateChangeRequired()
        {
            return driveState.GetNextStateType() != stateType;
        }

        /// <summary>
        /// Change the current DriveState if required and return the corresponding StateType of the new DriveState.
        /// </summary>
        /// <returns>StateType - the StateType for the new DriveState</returns>
        public StateType ChangeState()
        {
            // If change is not required, return current state
            // TODO most likely will be an expensive call, so keep nextStateType as a global here?
            if (!IsStateChangeRequired())
            {
                return this.stateType;
            }

            StateType nextStateType = driveState.GetNextStateType();

            driveState = StateTypeHelper.ToDriveState(nextStateType);

            return nextStateType;
        }

        public IDriveState DriveState
        {
            get { return this.driveState; }
            set { this.driveState = value; }
        }

        public StateType StateType
        {
            get { return stateType; }
            set { stateType = value; }
        }
    }
}
