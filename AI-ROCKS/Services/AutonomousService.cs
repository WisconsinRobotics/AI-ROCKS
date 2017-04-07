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

        // RDP
        private const double REGION_SEPARATION_DISTANCE = 300.0;    // Distance between regions - helps reduce noise
        private const double RDP_THRESHOLD = 50.0;                  // How much the LRF data is reduced

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;

        private DriveContext driveContext;
        private LRF lrf;


        public AutonomousService(String lrfPort, StateType initialStateType)
        {

            this.driveContext = new DriveContext(initialStateType);
            this.ObstacleEvent += driveContext.HandleObstacleEvent;

            int lrfUDPPort = 0;
            bool lrfInit = false;
            this.lrf = new LRF();
            if (Int32.TryParse(lrfPort, out lrfUDPPort))
            {
                // For getting LRF data over UDP
                lrfInit = lrf.Initialize(lrfUDPPort);
            }
            else
            {
                // For getting LRF data over serial
                //lrfInit = lrf.Initialize(lrfPort);
            }

            if (!lrfInit)
            {
                // TODO Fail - send error code to ROCKS

                // For now, throw exception (so you don't spend an hour debugging to end up figuring out you specified the port wrong..#triggered)
                throw new ArgumentException("Invalid port for LRF - must be integer (UDP) or COM port (serial)");
            }
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
                    this.driveContext.Drive(DriveCommand.Straight(DriveCommand.SPEED_CLEAR_OBSTACLE));
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
            
            // Only get Coordinates within the LRF FOV to avoid detecting wheels as an obstacle
            List<Coordinate> coordinates = lrf.GetCoordinates(DriveContext.LRF_MIN_ANGLE, DriveContext.LRF_MAX_ANGLE);

            List<Region> regions = Region.GetRegionsFromCoordinateList(coordinates, REGION_SEPARATION_DISTANCE, RDP_THRESHOLD);
            Plot plot = new Plot(regions);

            // See if any obstacle within maximum allowed distance
            bool obstacleDetected = false;
            foreach (Region region in plot.Regions)
            {
                foreach (Coordinate coordinate in region.ReducedCoordinates)
                {
                    if (coordinate.R < OBSTACLE_DETECTION_DISTANCE)
                    {
                        if (Math.Abs(coordinate.X) < DriveContext.ASCENT_WIDTH/2)
                        {
                            obstacleDetected = true;
                            break;
                        }
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
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < this.driveContext.LastObstacleDetected + milliseconds;
        }
    }
}
