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
        public const float ASCENT_WIDTH = 1168.4f;

        private IDriveState driveState;
        private StateType stateType;
        private AutonomousService autonomousService;    //TODO pass this? Shouldn't a service not be accessible to it's members? -> pass things I need to constructor rather than whole service?

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
        /// 
        /// If the SendDriveCommandLock cannot be obtained, do not send the DriveCommand. TODO why? Shore this up
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

        /// <summary>
        /// Function subscribed to AutonomousService's ObstacleEvent to handle an ObstacleEvent when it's triggered.
        /// 
        /// This function gets the Plot representing the detected obstacles and uses the current DriveState's 
        /// FindBestGap() to determine the best gap to drive toward. It then issues the corresponding DriveCommand.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">ObstacleEventArgs that contains the Plot representing the detected obstacles.</param>
        public void HandleObstacleEvent(Object sender, ObstacleEventArgs e)
        {
            DriveCommand driveCommand;
            Plot obstacles = e.Data;

            // Find the best gap
            Line bestGap = this.driveState.FindBestGap(obstacles);

            // If bestGap exists, drive toward it's midpoint. Otherwise, turn right
            if (bestGap != null)
            {
                Coordinate midpoint = bestGap.FindMidpoint();
                double angle = midpoint.Theta;

                driveCommand = new DriveCommand(angle, DriveCommand.SPEED_CLEAR_OBSTACLE);
            }
            else
            {
                driveCommand = DriveCommand.RightTurn(DriveCommand.SPEED_CLEAR_OBSTACLE);
            }

            // Send driveCommand
            lock (autonomousService.SendDriveCommandLock)
            {
                DriveHandler.SendDriveCommand(driveCommand);
                autonomousService.LastObstacleDetected = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// Property for the current DriveState.
        /// </summary>
        public IDriveState DriveState
        {
            get { return this.driveState; }
            set { this.driveState = value; }
        }

        /// <summary>
        /// Property for the current StateType.
        /// </summary>
        public StateType StateType
        {
            get { return stateType; }
            set { stateType = value; }
        }
    }
}
