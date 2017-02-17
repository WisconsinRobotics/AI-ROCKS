using System;
using System.Timers;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Utils;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        private const long OBSTACLE_WATCHDOG_MILLIS = 5000;         // 5 second delay   // TODO verify and update
        private const long CLEAR_OBSTACLE_DELAY_MILLIS = 1000;      // 1 second delay   // TODO verify and update
        private const long OBSTACLE_DETECTION_DISTANCE = 2000;      // 2 meters         // TODO verify and update

        private DriveContext driveContext;
        private readonly object sendDriveCommandLock;
        private long lastObstacleDetected;

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;


        public AutonomousService(StateType initialStateType)
        {
            this.driveContext = new DriveContext(this, initialStateType);
            this.sendDriveCommandLock = new Object();
        }


        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute(Object source, ElapsedEventArgs e)
        {
            // If detected an obstacle within the last 5 seconds, continue straight to clear obstacle
            if (IsLastObstacleWithinInterval(OBSTACLE_WATCHDOG_MILLIS))
            {
                // If more than 0.5 seconds have passed since last event, it's safe to start issuing drive 
                // commands - otherwise race condition may occur when continually detecting an obstacle
                if (!IsLastObstacleWithinInterval(CLEAR_OBSTACLE_DELAY_MILLIS))
                {
                    // Send "straight" DriveCommand to AscentPacketHandler
                    this.driveContext.Drive(DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED));
                }
                
                return;
            }

            // Get DriveCommand from current drive state
            DriveCommand driveCommand = this.driveContext.FindNextDriveCommand();

            // Issue drive command
            this.driveContext.Drive(driveCommand);

            // If state change is required, change state
            if (this.driveContext.IsStateChangeRequired())
            {
                this.driveContext.ChangeState();
            }
        }

        /// <summary>
        /// Detect if an ObstacleEvent must be raised according to the LRF data being read. Trigger
        /// ObstacleEvent if an obstacle exists within the maximum allowable distance threshold.
        /// </summary>
        public void DetectObstacleEvent(Object source, ElapsedEventArgs e)
        {
            // Get LRF data
            Plot plot = new Plot(); // TODO this data would come from LRF

            // See if any event within maximum allowed distance
            // Probably add this as a function in ObstacleLibrary:
            bool obstacleDetected = false;
            foreach (Region region in plot.Regions)
            {
                foreach (Coordinate coordinate in region.ReducedCoordinates)
                {
                    // TODO test this logic
                    if (coordinate.R < OBSTACLE_DETECTION_DISTANCE)
                    {
                        obstacleDetected = true;
                        break;
                    }
                }

                if (obstacleDetected)
                {
                    break;
                }
            }

            // If obstacle detected, trigger event
            if (obstacleDetected)
            {
                OnObstacleEvent(new ObstacleEventArgs(plot));
            }
        }

        /// <summary>
        /// Trigger an ObstacleEvent.
        /// </summary>
        /// <param name="e">ObstacleEventArgs</param>
        protected virtual void OnObstacleEvent(ObstacleEventArgs e)
        {
            EventHandler<ObstacleEventArgs> handler = ObstacleEvent;

            if (handler != null)
            {
                // BeginInvoke here? use same thread or spawn new thread?
                handler(this, e);
            }
        }

        private bool IsLastObstacleWithinInterval(long milliseconds)
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < lastObstacleDetected + milliseconds;
        }

        public long LastObstacleDetected
        {
            set { this.lastObstacleDetected = value; }
        }

        public Object SendDriveCommandLock
        {
            get { return this.sendDriveCommandLock; }
        }
    }
}
