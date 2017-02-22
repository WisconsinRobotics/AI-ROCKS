using System;

using ObstacleLibrarySharp;
using LRFLibrarySharp;
using System.Collections.Generic;

namespace AI_ROCKS.Drive.DriveStates
{
    // CURRENTLY WORKING AS IF COMPASS RETURNS ASMUTH (COMPASS NOT UNIT CIRCLE)
    class GPSDriveState : IDriveState
    {
        private const float DIRECTION_VATIANCE_NOISE = .001f; // gives threshold that "straight" is considered

        GPS finalGPS;

        PacketHandlers.GPSHandler gpsHandler;
        GPS currGPS;
        //PacketHandlers.CompassHandler compassHandler;  NOT IMPLEMENTED YET
        // Compass currCompass;

        DriveCommand command;

        public GPSDriveState()
        {
            this.gpsHandler = new PacketHandlers.GPSHandler();
            // this.compassHandler = new PacketHandlers.compassHandler();
        }


        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            // Get "current" data from AscentShimLayer in form of GPS object
            currGPS = gpsHandler.Data;
            // currCompass = compassHandler.Data;

            // Do GPS driving
            // find angle between our gps and the end gps with trig 
            double idealDirection = Math.Atan2((finalGPS.Latitude - currGPS.Latitude), (finalGPS.Longitude - finalGPS.Longitude));
            idealDirection = idealDirection * (180 / Math.PI);
            idealDirection = -idealDirection;
            idealDirection = idealDirection + 90;
            if (idealDirection < 0)
                idealDirection = idealDirection + 360;

            // if lined up within numeric precision, drive straight
            if (Math.Abs(idealDirection - currCompass) < DIRECTION_VATIANCE_NOISE 
                || Math.Abs(idealDirection - currCompass) > (360 - DIRECTION_VATIANCE_NOISE))
            {
                return command = DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED);
            }
            else if (((currCompass - idealDirection) % 360) < 180) // heading east of ideal, need to turn left
            {
                return command = new DriveCommand(-1, 1, 1);
            }
            else // heading east of ideal
            {
                return command = new DriveCommand(1, -1, 1);
            }
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            // Logic for finding when state needs to be switched from GPSDriveState to VisionDriveState
            return 0;
        }

        // Given a Plot representing the obstacles, find Line representing the best gap.

        // Do GPS driving according to our rendition of the "Follow the Gap" algorithm:
        // Reference here: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
        //
        // This algorithm avoids obstacles by finding the gap between them. It has a threshold gap (i.e. robot width),
        // and if the measured gap is greater than the threshold gap, the robot follows the calculated gap angle. 
        // In our case, the best gap will also be the one with the smallest displacement from the goal (the gate).

        // 1) Get LRF, GPS data 
        // 2) Calculate valid (large enough) gaps as Line objects, store in a list
        // 3) Find which gap is "best" (gap center angle has smallest deviation from straight line to goal)
        // 4) Find heading angle (actual angle to more according to combination of gap center and goal angles)
        // 5) Make DriveCommand for this angle and speed, return it
        //TODO event typing into some new ObstacleAvoidanceDriveState that triggers when the robot needs to avoid an obstacle
        public Line FindBestGap(Plot obstacles)
        {
            List<Region> regions = obstacles.Regions;
            double threshold = DriveContext.ASCENT_WIDTH; //in mm 
            Line bestGap = null;
            Line gapLine;
            //start to iterate through the list to find the bestGap
            for (int i = 0; i < regions.Count - 1; i++)
            {
                Region leftRegion = regions[i];
                Region rightRegion = regions[i+1];
                // gap is distance, just needs to be big enough, maybe 1.5 times width of robot
                // also doesn't get gap distance for between first or last with the ends
                double gap = Plot.GapDistanceBetweenRegions(leftRegion, rightRegion); // this returns true gap distance, not horizontal distance
                if (gap >= threshold)
                {
                    gapLine = new Line(new Coordinate(leftRegion.EndCoordinate.X, leftRegion.EndCoordinate.Y, CoordSystem.Cartesian),
                                       new Coordinate(rightRegion.StartCoordinate.X, rightRegion.StartCoordinate.Y, CoordSystem.Cartesian));

                    Coordinate midpoint = new Coordinate((gapLine.EndCoordinate.X - gapLine.StartCoordinate.X) / 2, (gapLine.EndCoordinate.Y - gapLine.StartCoordinate.Y) / 2, CoordSystem.Cartesian);
                    if(bestGap == null)
                    {
                        bestGap = gapLine;
                    }
                }
            }
        return bestGap;
        }
         

            

         
        }
