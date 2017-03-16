using System;
using System.Collections.Generic;
using System.Timers;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Utils;
using LRFLibrarySharp;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        // Obstacle avoidance
        private const long OBSTACLE_WATCHDOG_MILLIS = 1000;         // 5 second delay   // TODO verify and update
        private const long CLEAR_OBSTACLE_DELAY_MILLIS = 1000;      // 1 second delay   // TODO verify and update
        public const long OBSTACLE_DETECTION_DISTANCE = 2000;       // 2 meters         // TODO verify and update

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;
        private readonly object sendDriveCommandLock;
        private long lastObstacleDetected;

        // RDP
        private const double REGION_SEPARATION_DISTANCE = 60.0;                         // TODO verify, move somewhere?
        private const double RDP_THRESHOLD = 5.0;

        private DriveContext driveContext;
        private LRF lrf;


        public AutonomousService(String lrfPort, StateType initialStateType)
        {
            this.driveContext = new DriveContext(this, initialStateType);
            this.sendDriveCommandLock = new Object();

            this.lrf = new LRF();
            //lrf.Initialize(lrfPort);    // For getting LRF data over serial
            int lrfUDPPort = 0;
            Int32.TryParse(lrfPort, out lrfUDPPort);
            lrf.Initialize(lrfUDPPort);         // For getting LRF data over UDP
        }


        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute(Object source, ElapsedEventArgs e)
        {
            // If detected an obstacle within the last 5 seconds, continue straight to clear obstacle
            if (IsLastObstacleWithinInterval(OBSTACLE_WATCHDOG_MILLIS))
            {
                Console.WriteLine("Watchdog");
                // If more than 0.5 seconds have passed since last event, it's safe to start issuing drive 
                // commands - otherwise race condition may occur when continually detecting an obstacle
                if (!IsLastObstacleWithinInterval(CLEAR_OBSTACLE_DELAY_MILLIS))
                {
                    this.driveContext.Drive(DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED));
                }
                
                return;
            }

            // Get DriveCommand from current drive state, issue DriveCommand
            DriveCommand driveCommand = this.driveContext.FindNextDriveCommand();
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
            lrf.RefreshData();
            List<Coordinate> coordinates = lrf.GetCoordinates(CoordinateFilter.Front);
            List<Region> regions = Region.GetRegionsFromCoordinateList(coordinates, REGION_SEPARATION_DISTANCE, RDP_THRESHOLD); //DriveContext.ASCENT_WIDTH, RDP_THRESHOLD);
            Plot plot = new Plot(regions);

            // See if any event within maximum allowed distance
            // Probably add this as a function in ObstacleLibrary:
            bool obstacleDetected = false;
            foreach (Region region in plot.Regions)
            {
                foreach (Coordinate coordinate in region.ReducedCoordinates)
                {
                    if (coordinate.R < OBSTACLE_DETECTION_DISTANCE)
                    {
                        //if (coordinate.Theta > 1.0472 && coordinate.Theta < 2.0944)
                        //{
                            if (Math.Abs(coordinate.X) < DriveContext.ASCENT_WIDTH/2)
                            {
                                obstacleDetected = true;
                                break;
                            }
                        //}
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

        /// <summary>
        /// If the last obstacle detected happened within a specified threshold of time, in milliseconds.
        /// </summary>
        /// <param name="milliseconds">Threshold used to test if the last obstacle occured within a certain
        /// amount of time.</param>
        /// <returns>bool - true if the last obstacle was detected within threshold time, false otherwise</returns>
        private bool IsLastObstacleWithinInterval(long milliseconds)
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < lastObstacleDetected + milliseconds;
        }

        /// <summary>
        /// Property representing when the last obstacle was detected (unix time in milliseconds).
        /// </summary>
        public long LastObstacleDetected
        {
            set { this.lastObstacleDetected = value; }
        }

        /// <summary>
        /// Property to get the sendDriveCommandLock.
        /// </summary>
        public Object SendDriveCommandLock
        {
            get { return this.sendDriveCommandLock; }
        }
    }
}
