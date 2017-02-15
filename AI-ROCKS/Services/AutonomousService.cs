using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Utils;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        private const long OBSTACLE_WATCHDOG_MILLIS = 5000;
        private const long CLEAR_OBSTACLE_DELAY_MILLIS = 1000;  //TODO verify

        private DriveContext driveContext;
        private readonly object sendDriveCommandLock;
        private long lastObstacleDetected;

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;


        public AutonomousService()
        {
            this.driveContext = new DriveContext(this);
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
                //Console.Write("Watchdog caught in Execute at: " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "\n");

                // If more than 0.5 seconds have passed since last event, it's safe to start issuing drive 
                // commands - otherwise race condition may occur when continually detecting an obstacle
                if (!IsLastObstacleWithinInterval(CLEAR_OBSTACLE_DELAY_MILLIS))
                {
                    // Test write - delete
                    //Console.Write("Watchdog ready to issue straight drive command: " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\n");

                    // Send "straight" DriveCommand to AscentPacketHandler
                    this.driveContext.Drive(DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED));
                }
                else
                {
                    // Test write - delete
                    //Console.Write("Watchdog caught in Execute at: " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "\n");
                }
                
                return;
            }

            // Get DriveCommand from current drive state
            DriveCommand driveCommand = this.driveContext.FindNextDriveCommand();

            // Issue drive command
            this.driveContext.Drive(driveCommand);


            // Other Autonomous driving code...
            // ...
            // ...


            // If state change is required, change state
            if (this.driveContext.IsStateChangeRequired())
            {
                this.driveContext.ChangeState();
            }


            // Even more Autonomous driving code
            // ...
            // ...
        }

        /// <summary>
        /// Detect if an ObstacleEvent must be raised according to the LRF data being read. Trigger
        /// ObstacleEvent if an obstacle exists within the maximum allowable distance threshold.
        /// </summary>
        public void DetectObstacleEvent(Object source, ElapsedEventArgs e)
        {
            // Get LRF data
            Plot plot = new Plot(); //This data would come from LRF

            // See if any event within maximum allowed distance

            // If so, trigger event

            // Test code to trigger event every 10 seconds - delete
            // TODO delete
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 10 == 0)
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
