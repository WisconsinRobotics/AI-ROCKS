using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AI_ROCKS.Drive;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        private DriveContext driveContext;
        private object obstacleLock;
        private long lastObstacleDetected;
        private const long OBSTACLE_WATCHDOG_SECONDS = 5;


        public AutonomousService()
        {
            this.driveContext = new DriveContext();
            this.obstacleLock = new object();

        }

        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute()
        {
            // Autononous driving code ...
            // ...
            // ...
            

            // Obstacle avoidance stuff? - Or does that go in DriveContext? -> Look more into events
            

            // If obstacleLock is taken (i.e. if ObstacleEvent is triggered), return
            

            // If detected an obstacle within the last 5 seconds, continue straight
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() < lastObstacleDetected + OBSTACLE_WATCHDOG_SECONDS)
            {
                // Send "straight" DriveCommand to AscentShimLayer
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


    }
}
