using System;

using ObstacleLibrarySharp;
using LRFLibrarySharp;
using System.Collections.Generic;
using System.Linq;

using AI_ROCKS.Drive.Utils;
using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.Drive.DriveStates
{
    // CURRENTLY WORKING AS IF COMPASS RETURNS ASMUTH (COMPASS NOT UNIT CIRCLE)
    class GPSDriveState : IDriveState
    {
        private const float DIRECTION_VATIANCE_NOISE = 5f; // gives threshold that "straight" is considered
        private const long LRF_MAX_RELIABLE_DISTANCE = 6000;    // TODO get from LRFLibrary

        private double idealDirection;
        GPS finalGPS = new GPS(0, 0, 58, 0, 3, 7);

        //GPS currGPS;
        //short currCompass;

        DriveCommand command;
        // TODO CONSTANTS FOR DRIVE COMMAND SPEED
        public GPSDriveState()
        {
        }
        static int count = 0;
        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            if (count == 0)
            {
                count++;
                return DriveCommand.Straight(50);
            }
            GPS currGPS = AI_ROCKS.PacketHandlers.AscentPacketHandler.GPSData;
            short currCompass = AI_ROCKS.PacketHandlers.AscentPacketHandler.Compass; // currCompass needs to be received as a compass from gazebo
            idealDirection = currGPS.GetHeadingTo(finalGPS);
            // might need to be changed
            /*           if (currCompass < 0)
                           currCompass = (short)(-1 * currCompass);
                       else
                           currCompass = (short)(360 - currCompass);*/

            // get data in good form 

            /*float finalLat, finalLong, currLat, currLong;
            finalLat = finalGPS.LatDegrees + (finalGPS.LatMinutes / 60f) + (finalGPS.LatSeconds / 60f / 60f);
            finalLong = finalGPS.LongDegrees+ (finalGPS.LongMinutes / 60f) + (finalGPS.LongSeconds / 60f / 60f);
            currLat = currGPS.LatDegrees + (currGPS.LatMinutes / 60f) + (currGPS.LatSeconds / 60f / 60f);
            currLong = currGPS.LongDegrees + (currGPS.LongMinutes / 60f) + (currGPS.LongSeconds / 60f / 60f);
            
            // calculate ideal direction
            idealDirection = Math.Atan2((finalLat - currLat), (finalLong - currLong));
            if (idealDirection < 0)
            {
                idealDirection += (float)(2 * Math.PI);
            }
            idealDirection = idealDirection * (180 / Math.PI);
            idealDirection = 90 - idealDirection;
            if ((finalLong - currLong) < 0)
            {
                idealDirection = idealDirection + 180;
            }
            //Flipping direction to match the opposite navigation system as in gazebo
            //idealDirection = (idealDirection + 180) % 360; */
             // if lined up within numeric precision, drive straight
            if (Math.Abs(idealDirection - currCompass) < DIRECTION_VATIANCE_NOISE
                || Math.Abs(idealDirection - currCompass) > (360 - DIRECTION_VATIANCE_NOISE))
            {
                return command = DriveCommand.Straight(50);
            }

            // not aligned with endGPS point, need to turn
            // the math here is strange due to compass vs unit circle stuff
            // the first case takes care of all time when the ideal direction is in some way east of us,
            // and the second case takes care of all time when the ideal direction is in some way west of us
            double opposite = (idealDirection + 180) % 360;
            if (idealDirection < opposite) // this means that modulo was not necessary ie ideal direction < 180
            {
                if (currCompass > idealDirection && currCompass < opposite) // turn left
                {
                    return command = DriveCommand.LeftTurn(50);
                }
                else // turn right
                {
                    return command = DriveCommand.RightTurn(50);
                }
            }
            else // modulo necessary
            {
                if ((currCompass > idealDirection && currCompass < 360) || (currCompass > 0 && currCompass < opposite)) // turn left
                {
                    return command = DriveCommand.LeftTurn(50);
                }
                else // turn right
                {
                    return command = DriveCommand.RightTurn(50);
                }
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
            double threshold = DriveContext.ASCENT_WIDTH / 10; //in mm 
            Line bestGap = null;
            double gap;
            Line gapLine;
            Coordinate midpoint;
            double bestAngle = Double.MaxValue;
            double angle;

            GPS currGPS = AI_ROCKS.PacketHandlers.AscentPacketHandler.GPSData;
            short currCompass = AI_ROCKS.PacketHandlers.AscentPacketHandler.Compass;

            idealDirection = currGPS.GetHeadingTo(finalGPS);

            // Check first and last Region gaps (may be same Region if only one Region)
            Region firstRegion = regions.ElementAt(0);
            Region lastRegion = regions.ElementAt(regions.Count - 1);

            // left and right fields of view (FOV)
            Line lrfLeftFOV =
                new Line(new Coordinate(0, 0, CoordSystem.Cartesian), new Coordinate(-1 * LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));
            Line lrfRightFOV =
                new Line(new Coordinate(0, 0, CoordSystem.Cartesian), new Coordinate(LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));

            // Check if leftmost Coordinate in the leftmost Region is on the right half of the entire FOV. If it is, make leftEdgeCoordinate where the 
            // max -acceptable-range meets the left FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate leftEdgeCoordinate = Line.FindClosestPointOnLine(lrfLeftFOV, firstRegion.StartCoordinate);
            if (firstRegion.StartCoordinate.X > 0)
            {
                leftEdgeCoordinate = new Coordinate(-AI_ROCKS.Services.AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }
            Line leftEdgeGap = new Line(leftEdgeCoordinate, firstRegion.StartCoordinate);
            // give leftEdgeGap the old checkerooski!
            if (leftEdgeGap.Length >= threshold)
            {
                Coordinate leftMidPoint = leftEdgeGap.FindMidpoint();
                if (leftMidPoint.X > 0)
                {
                    angle = currCompass + (90 - (Math.Atan2(leftMidPoint.Y, leftMidPoint.X) * (180 / Math.PI)));
                    angle = angle % 360;
                }
                else if (leftMidPoint.X < 0)
                {
                    angle = currCompass - (90 - (Math.Atan2(-1 * leftMidPoint.Y, leftMidPoint.X) * (180 / Math.PI)));
                    angle = angle % 360;
                }
                else // midpoint.X is 0
                {
                    angle = currCompass; // so elegant :)
                }
                if (Math.Abs(idealDirection - angle) < bestAngle)
                {
                    bestAngle = angle;
                    bestGap = leftEdgeGap;
                }
            }

            // Check if rightmost Coordinate in the rightmost Region is on the left half of the entire FOV. If it is, make rightEdgeCoordinate where the
            // max -acceptable-range meets the right FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate rightEdgeCoordinate = Line.FindClosestPointOnLine(lrfRightFOV, lastRegion.EndCoordinate);
            if (lastRegion.EndCoordinate.X < 0)
            {
                rightEdgeCoordinate = new Coordinate(AI_ROCKS.Services.AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }
            Line rightEdgeGap = new Line(rightEdgeCoordinate, lastRegion.EndCoordinate);
            // give rightEdgeGap the old checkerooski!
            if (rightEdgeGap.Length >= threshold)
            {
                Coordinate rightMidPoint = rightEdgeGap.FindMidpoint();
                if (rightMidPoint.X > 0)
                {
                    angle = currCompass + (90 - (Math.Atan2(rightMidPoint.Y, rightMidPoint.X) * (180 / Math.PI)));
                    angle = angle % 360;
                }
                else if (rightMidPoint.X < 0)
                {
                    angle = currCompass - (90 - (Math.Atan2(-1 * rightMidPoint.Y, rightMidPoint.X) * (180 / Math.PI)));
                    angle = angle % 360;
                }
                else // midpoint.X is 0
                {
                    angle = currCompass; // so elegant :)
                }
                if (Math.Abs(idealDirection - angle) < bestAngle)
                {
                    bestAngle = angle;
                    bestGap = rightEdgeGap;
                }
            }
            
            //start to iterate through the list to find the bestGap
            for (int i = 0; i < regions.Count - 2; i++) // don't iterate through entire list, will result in index out of bounds error on rightRegion assignment
            {
                Region leftRegion = regions[i];
                Region rightRegion = regions[i + 1];
                
                // gap is distance, just needs to be big enough, maybe 1.5 times width of robot
                // also doesn't get gap distance for between first or last with the ends
                // I have a qualm with get gap distance, raw distance is returned, no projection is done
                gap = Plot.GapDistanceBetweenRegions(leftRegion, rightRegion); // this returns true gap distance, not horizontal distance
                if (gap >= threshold)
                {
                    gapLine = new Line(new Coordinate(leftRegion.EndCoordinate.X, leftRegion.EndCoordinate.Y, CoordSystem.Cartesian),
                                       new Coordinate(rightRegion.StartCoordinate.X, rightRegion.StartCoordinate.Y, CoordSystem.Cartesian));

                    midpoint = gapLine.FindMidpoint();
                    if (bestGap == null)
                    {
                        bestGap = gapLine;
                    }
                    else
                    {
                        if (midpoint.X > 0)
                        {
                           angle = currCompass + (90 - (Math.Atan2(midpoint.Y, midpoint.X) * (180 / Math.PI)));
                           angle = angle % 360;
                        }
                        else if (midpoint.X < 0)
                        {
                            angle = currCompass - (90 - (Math.Atan2(-1 * midpoint.Y, midpoint.X) * (180 / Math.PI)));
                            angle = angle % 360;
                        }
                        else // midpoint.X is 0
                        {
                            angle = currCompass; // so elegant :)
                        }
                        if (Math.Abs(idealDirection - angle) < bestAngle)
                        {
                            bestAngle = angle;
                            bestGap = gapLine;
                        }
                    }
                }
            }
            return bestGap;
            // may need to add code in taking into account slope! 
        }
        
    }
}
