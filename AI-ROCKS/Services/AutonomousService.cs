using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Utils;
using LRFLibrarySharp;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        // TODO update for field testing
        private const long OBSTACLE_WATCHDOG_MILLIS = 1000;         // 5 second delay   // TODO verify and update
        private const long CLEAR_OBSTACLE_DELAY_MILLIS = 1000;      // 1 second delay   // TODO verify and update
        // TODO update for field testing
        private const long OBSTACLE_DETECTION_DISTANCE = 500;       // 2 meters         // TODO verify and update

        private const string LRF_SERIAL_PORT = "COM3";                                  // TODO make this better - param?
        private const int LRF_PORT = 20001;
        private const double REGION_SEPARATION_DISTANCE = 60.0;                         // TODO verify, move somewhere?
        private const double RDP_THRESHOLD = 5.0;

        private DriveContext driveContext;
        private LRF lrf;

        private readonly object sendDriveCommandLock;
        private long lastObstacleDetected;

        public event EventHandler<ObstacleEventArgs> ObstacleEvent;


        public AutonomousService(StateType initialStateType)
        {
            this.driveContext = new DriveContext(this, initialStateType);
            this.sendDriveCommandLock = new Object();

            this.lrf = new LRF();
            lrf.Initialize(LRF_SERIAL_PORT);    // For getting LRF data over serial
            //lrf.Initialize(LRF_PORT);         // For getting LRF data over UDP
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
            lrf.RefreshData();
            // TODO figure out why we can't use CoordinateFilter over UDP
            //List<Coordinate> coordinates = lrf.GetCoordinates(CoordinateFilter.Front);    // For serial
            List<Coordinate> coordinates = lrf.GetCoordinates();                            // For over UDP

            //coordinates = coordinates.Where(coord => coord.Theta < Math.PI / 2 || coord.Theta > 3 * Math.PI / 2).OrderBy(c1 => c1.Theta).ToList();    // For serial
            coordinates = coordinates.Where(coord => coord.Theta < Math.PI / 2 || coord.Theta > 3 * Math.PI / 2).ToList();

            List<Region> regions = Region.GetRegionsFromCoordinateList(coordinates, DriveContext.ASCENT_WIDTH, RDP_THRESHOLD); //REGION_SEPARATION_DISTANCE, RDP_THRESHOLD);            
            Plot plot = new Plot(regions);

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
