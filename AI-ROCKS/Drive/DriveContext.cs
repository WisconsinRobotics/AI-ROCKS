using System;
using System.Threading;

using AI_ROCKS.Drive.DriveStates;
using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;
using ObstacleLibrarySharp;
using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.Drive
{
    class DriveContext
    {
        public const float ASCENT_WIDTH = 1168.4f;

        // LRF
        public const long LRF_MAX_RELIABLE_DISTANCE = 6000;
        public const float LRF_MIN_ANGLE = (float)Math.PI / 4;          // 45 degrees right edge
        public const float LRF_MAX_ANGLE = 3 * (float)Math.PI / 4;      // 135 degrees left edge

        // LRF field of view (FOV) edges
        public static readonly Line LRF_RIGHT_FOV_EDGE =
            new Line(new Coordinate(0, 0, CoordSystem.Polar), new Coordinate(LRF_MIN_ANGLE, LRF_MAX_RELIABLE_DISTANCE, CoordSystem.Polar));
        public static readonly Line LRF_LEFT_FOV_EDGE =
            new Line(new Coordinate(0, 0, CoordSystem.Polar), new Coordinate(LRF_MAX_ANGLE, LRF_MAX_RELIABLE_DISTANCE, CoordSystem.Polar));

        // Vision
        public const double REQUIRED_DISTANCE_FROM_BALL = 2.0;    // Meters

        private IDriveState driveState;
        private StateType stateType;
        private GPS gate;

        private readonly Object sendDriveCommandLock;
        private long lastObstacleDetected;

        public DriveContext(StateType initialStateType, GPS gate)
        {
            this.gate = gate;

            // GPSDriveState is default unless specified
            this.driveState = StateTypeHelper.ToDriveState(initialStateType, gate);
            this.stateType = initialStateType;
            
            this.sendDriveCommandLock = new Object();
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
            Monitor.TryEnter(this.sendDriveCommandLock, ref isLocked);

            if (isLocked)
            {
                // Send DriveCommand to AscentPacketHandler
                DriveHandler.SendDriveCommand(driveCommand);

                // Release lock
                Monitor.Exit(this.sendDriveCommandLock);
            }
        }

        public StateType GetNextStateType()
        {
            return this.driveState.GetNextStateType();
        }

        /// <summary>
        /// If the current DriveState has determined that the state must be changed.
        /// </summary>
        /// <returns>bool - If state change is required.</returns>
        public bool IsStateChangeRequired(StateType nextState)
        {
            return nextState != this.stateType;
        }

        //TODO look at this function
        /// <summary>
        /// Change the current DriveState if required and return the corresponding StateType of the new DriveState.
        /// </summary>
        /// <returns>StateType - the StateType for the new DriveState</returns>
        public StateType ChangeState(StateType nextState)
        {
            // If change is not required, return current state
            // TODO: ALL BELOW THINGS:
            // - Most likely will be an expensive call, so keep nextStateType as a global here?
            // - Even check if state change is required? Or assume this is called after IsStateChangeRequired() has been called -> be safe for calling function or let the call do whatever it wants?
            // - Specify param or nah? Need to assume more than two states
            if (!this.IsStateChangeRequired(nextState))
            {
                return this.stateType;
            }

            this.driveState = StateTypeHelper.ToDriveState(nextState, this.gate);
            this.stateType = nextState;

            return nextState;
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
            lock (this.sendDriveCommandLock)
            {
                DriveHandler.SendDriveCommand(driveCommand);
                this.lastObstacleDetected = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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

        /// <summary>
        /// Property representing when the last obstacle was detected (unix time in milliseconds).
        /// </summary>
        public long LastObstacleDetected
        {
            get { return this.lastObstacleDetected; }
            set { this.lastObstacleDetected = value; }
        }

        /// <summary>
        /// Property for the GPS coordinates of the gate.
        /// </summary>
        public GPS Gate
        {
            get { return this.gate; }
        }

        /// <summary>
        /// Return a Line representing a gap straight in front of Ascent. This gap is a Line twice the width 
        /// of Ascent and half the maximum distance.
        /// </summary>
        /// <returns> Line - Line representing an open gap straight in front of Ascent.</returns>
        public static Line GapStraightInFront()
        {
            // Return Line representing gap straight in front of Ascent
            Coordinate leftCoord = new Coordinate(-DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);
            Coordinate rightCoord = new Coordinate(DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);

            return new Line(leftCoord, rightCoord);
        }
    }
}
