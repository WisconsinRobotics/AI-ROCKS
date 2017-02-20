using System;
using System.Threading;

using AI_ROCKS.Drive.DriveStates;
using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;
using AI_ROCKS.Services;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive
{
    class DriveContext
    {
        public const float ASCENT_WIDTH = 1168.4f;//1.1684f                 // TODO fill in with actual value

        private IDriveState driveState;
        private StateType stateType;
        private AutonomousService autonomousService;

        public DriveContext(AutonomousService autonomousService, StateType initialStateType)
        {
            // GPSDriveState is default unless specified
            this.driveState = StateTypeHelper.ToDriveState(initialStateType);
            this.stateType = initialStateType;
            
            this.autonomousService = autonomousService;
            
            // Subscribe to ObstacleEvent
            autonomousService.ObstacleEvent += HandleObstacleEvent;
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
        /// Issue the specified DriveCommand to ROCKS through AscentPacketHandler.
        /// </summary>
        /// <param name="driveCommand">The DriveCommand to be executed.</param>
        public void Drive(DriveCommand driveCommand)
        {
            // Obtain lock
            bool isLocked = false;
            Monitor.TryEnter(autonomousService.SendDriveCommandLock, ref isLocked);

            if (isLocked)
            {
                // Send DriveCommand to AscentPacketHandler
                DriveHandler.SendDriveCommand(driveCommand);

                // Release lock
                Monitor.Exit(autonomousService.SendDriveCommandLock);
            }
            
            // Return value?
        }

        /// <summary>
        /// If the current DriveState has determined that the state must be changed.
        /// </summary>
        /// <returns>bool - If state change is required.</returns>
        public bool IsStateChangeRequired()
        {
            return driveState.GetNextStateType() != stateType;
        }

        //TODO look at this function
        /// <summary>
        /// Change the current DriveState if required and return the corresponding StateType of the new DriveState.
        /// </summary>
        /// <returns>StateType - the StateType for the new DriveState</returns>
        public StateType ChangeState()
        {
            // If change is not required, return current state
            // TODO: ALL BELOW THINGS:
            // - Most likely will be an expensive call, so keep nextStateType as a global here?
            // - Even check if state change is required? Or assume this is called after IsStateChangeRequired() has been called -> be safe for calling function or let the call do whatever it wants?
            // - Specify param or nah? Need to assume more than two states
            if (!IsStateChangeRequired())
            {
                return this.stateType;
            }

            StateType nextStateType = driveState.GetNextStateType();

            driveState = StateTypeHelper.ToDriveState(nextStateType);
            return nextStateType;
        }

        public void HandleObstacleEvent(Object sender, ObstacleEventArgs e)
        {
            DriveCommand driveCommand;
            Plot obstacles = e.Data;

            // Find the best gap
            Line bestGap = this.driveState.FindBestGap(obstacles);

            if (bestGap != null)
            {
                // Drive toward bestGap's midpoint
                Coordinate midpoint = bestGap.FindMidpoint();

                // Straight ahead is 0 - calculate angle accordingly
                double angle = midpoint.Theta;   // TODO Determine this - how to scale it for our angle representation
                driveCommand = new DriveCommand(angle, DriveCommand.OBSTACLE_DRIVE_STATE_SPEED);
            }
            else
            {
                // Turn right
                driveCommand = DriveCommand.RightTurn(DriveCommand.OBSTACLE_DRIVE_STATE_SPEED);      // TODO find appropriate value here - want to be slower?
            }

            lock (autonomousService.SendDriveCommandLock)
            {
                DriveHandler.SendDriveCommand(driveCommand);
                autonomousService.LastObstacleDetected = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            
            // Return value?
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
