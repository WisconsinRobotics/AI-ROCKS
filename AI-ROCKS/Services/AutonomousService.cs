using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Utils;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        private DriveContext driveContext;
        private object obstacleLock;
        private long lastObstacleDetected;
        private const long OBSTACLE_WATCHDOG_SECONDS = 5;

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;


        public AutonomousService()
        {
            this.driveContext = new DriveContext(this);
            this.obstacleLock = new object();

        }

        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute(Object source, ElapsedEventArgs e)
        {
            // Autononous driving code ...
            // ...
            // ...
            

            // Obstacle avoidance stuff? - Or does that go in DriveContext? -> Look more into events
            

            // If detected an obstacle within the last 5 seconds, continue straight
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < lastObstacleDetected + OBSTACLE_WATCHDOG_SECONDS)
            {
                // Send "straight" DriveCommand to AscentShimLayer
                return;
            }


            // If obstacleLock is taken (i.e. if ObstacleEvent is triggered), return
            if (!Monitor.IsEntered(obstacleLock))
            {
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
        private void DetectObstacleEvent()
        {
            // Get LRF data
            Plot plot = new Plot(); //This data would come from LRF

            // See if any event within maximum allowed distance

            // Trigger event
            OnObstacleEvent(new ObstacleEventArgs(plot));
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
            else
            {
                // No subscribers, throw exception
                // This else can be removed once formal structure is figured out
            }
        }
    }
}
