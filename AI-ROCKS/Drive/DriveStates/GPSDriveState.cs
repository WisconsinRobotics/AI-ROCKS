using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using AI_ROCKS.Drive.Models;
using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;
using AI_ROCKS.Services;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class GPSDriveState : IDriveState
    {
        private const float THRESHOLD_HEADING_ANGLE = 10.0f;     // Gives threshold that "straight" is considered on either side
        private double GATE_PROXIMITY = 3.0;              // Distance from gate for when to switch to Vision
        GPS gate = null;
        // GPS test values:
        // right outside door:  -lat 43 4 17.9 -long -89 24 41.1    (or 17.7 for further back)
        // middle by stop sign: -lat 43 4 19.8 -long -89 24 41.0
        // end of grass:        -lat 43 4 19.5 -long -89 24 42.4
        // gazebo:              -lat 43 30 29.7 -long -89 30 29.6
        // front of ehall:      -lat 43 4 19.8 -long -89 24 37.5
        // old arrow at top of parking garage:  -lat 43 4 18.084 -long -89 24 43.938

        // circle on ground: -lat 38 22 17.892 -long -110 42 15.114  38,22,17.892 -110,42,15.114
        // north side of parking lot: 38,22,19.216, -110,42,15.5575

        // Averaging queue for distances - used for state switching logic
        private ConcurrentQueue<double> averagingQueue = new ConcurrentQueue<double>();
        private const int AVERAGING_QUEUE_CAPACITY = 5;

        private bool isTaskComplete = false;

        public GPSDriveState(GPS gate)
        {
            this.gate = gate;
        }

        public GPSDriveState(GPS gate, double proxy)
        {
            this.gate = gate;
            this.GATE_PROXIMITY = proxy;
        }
        
        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            GPS currGPS = AscentPacketHandler.GPSData;
            short currCompass = AscentPacketHandler.Compass;
            double idealDirection = currGPS.GetHeadingTo(gate);
            double distance = AscentPacketHandler.GPSData.GetDistanceTo(gate);

            // Add distance to averaging queue
            while (this.averagingQueue.Count >= AVERAGING_QUEUE_CAPACITY)
            {
                double value;
                this.averagingQueue.TryDequeue(out value);
            }
            this.averagingQueue.Enqueue(distance);

            // Debugging - delete
            Console.Write("currCompass: " + currCompass + " | headingToGoal: " + idealDirection + " | distance: " + distance + " | ");

            // Stop when within proximity to see if average distance of last 5 distances is within proximity. 
            // If so, wait to switch to Vision, otherwise this acts as a buffer.
            if (distance <= GATE_PROXIMITY)
            {
                return DriveCommand.Straight(Speed.HALT);
            }

            // If current heading within threshold, go straight
            if (IMU.IsHeadingWithinThreshold(currCompass, idealDirection, THRESHOLD_HEADING_ANGLE))
            {
                return DriveCommand.Straight(Speed.SLOW_OPERATION);
            }

            // not aligned with endGPS point, need to turn
            // the math here is strange due to compass vs unit circle stuff
            // the first case takes care of all time when the ideal direction is in some way east of us,
            // and the second case takes care of all time when the ideal direction is in some way west of us
            double opposite = (idealDirection + 180) % 360;

            if (idealDirection < opposite)
            {
                // Modulo not necessary - ideal direction < 180
                if (currCompass > idealDirection && currCompass < opposite)
                {
                    // Turn left
                    return DriveCommand.LeftTurn(Speed.SLOW_TURN);
                }
                else
                {
                    // Turn right
                    return DriveCommand.RightTurn(Speed.SLOW_TURN);
                }
            }
            else
            {
                // Modulo necessary
                if ((currCompass > idealDirection && currCompass < 360) || (currCompass >= 0 && currCompass < opposite))
                {
                    // Turn left
                    return DriveCommand.LeftTurn(Speed.SLOW_TURN);
                }
                else
                {
                    // Turn right
                    return DriveCommand.RightTurn(Speed.SLOW_TURN);
                }
            }
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            // Avoid trashing
            if (this.averagingQueue.Count < 5)
            {
                return StateType.GPSState;
            }

            // Get average distance to avoid erroneous switching due to noise
            double averageDistance = 0.0;
            foreach (double distanceItr in this.averagingQueue)
            {
                averageDistance += distanceItr;
            }

            averageDistance = averageDistance / 5;

            // When to be switch from GPSDriveState to VisionDriveState
            if (averageDistance <= GATE_PROXIMITY)
            {
                if (GATE_PROXIMITY == DriveContext.HAIL_MARY_GATE_PROXIMITY)
                {
                    StatusHandler.SendSimpleAIPacket(Status.AIS_FOUND_GATE);
                    Console.WriteLine("Hail mary is within required distance - halting ");

                    this.isTaskComplete = true;
                }
                else
                {
                    // Send log back to base station
                    StatusHandler.SendDebugAIPacket(Status.AIS_SWITCH_TO_VISION, "Drive state switch: GPS to Vision.");
                    Console.WriteLine("WITHIN PROXIMITY | ");
                }

                return StateType.VisionState;
            }


            return StateType.GPSState;
        }

        /// <summary>
        /// Given a Plot representing the obstacles, find the Line representing the best gap.
        /// Do GPS driving according to our rendition of the "Follow the Gap" algorithm:
        /// Reference here: https://pdfs.semanticscholar.org/c432/3017af7bce46fc7574ada008b8af1011e614.pdf
        /// This algorithm avoids obstacles by finding the gap between them. It has a threshold gap (i.e. robot width),
        /// and if the measured gap is greater than the threshold gap, the robot follows the calculated gap angle. 
        /// In our case, the best gap will also be the one with the smallest displacement from the goal (the gate).
        /// </summary>
        public Line FindBestGap(Plot obstacles)
        {
            List<Region> regions = obstacles.Regions;
            Line bestGap = null;
            double bestAngle = Double.MaxValue;

            // Get GPS, heading data 
            GPS currGPS = AscentPacketHandler.GPSData;
            short currCompass = AscentPacketHandler.Compass;

            // Add distance to averaging queue
            double distance = currGPS.GetDistanceTo(this.gate);
            while (this.averagingQueue.Count >= AVERAGING_QUEUE_CAPACITY)
            {
                double value;
                this.averagingQueue.TryDequeue(out value);
            }
            this.averagingQueue.Enqueue(distance);

            // Heading from current GPS to gate GPS
            double idealDirection = currGPS.GetHeadingTo(gate);

            // Check first and last Region gaps (may be same Region if only one Region)
            Region firstRegion = regions.ElementAt(0);
            Region lastRegion = regions.ElementAt(regions.Count - 1);

            // Left and right fields of view (FOV)
            Line lrfLeftFOV = DriveContext.LRF_LEFT_FOV_EDGE;
            Line lrfRightFOV = DriveContext.LRF_RIGHT_FOV_EDGE;

            // Check if leftmost Coordinate in the leftmost Region is on the right half of the entire FOV. If it is, make leftEdgeCoordinate where the 
            // max-acceptable-range meets the left FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate leftEdgeCoordinate = Line.FindClosestPointOnLine(lrfLeftFOV, firstRegion.StartCoordinate);
            if (firstRegion.StartCoordinate.X > 0)
            {
                leftEdgeCoordinate = new Coordinate(-AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }

            // Checking the left edge gap 
            Line leftEdgeGap = new Line(leftEdgeCoordinate, firstRegion.StartCoordinate);
            if (leftEdgeGap.Length >= DriveContext.ASCENT_WIDTH)
            {
                double angle;
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
                else
                {
                    // midpoint.X is 0
                    angle = currCompass; 
                }

                if (Math.Abs(idealDirection - angle) < bestAngle)
                {
                    bestAngle = idealDirection - angle;
                    bestGap = leftEdgeGap;
                }
            }

            // Check if rightmost Coordinate in the rightmost Region is on the left half of the entire FOV. If it is, make rightEdgeCoordinate where the
            // max-acceptable-range meets the right FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate rightEdgeCoordinate = Line.FindClosestPointOnLine(lrfRightFOV, lastRegion.EndCoordinate);
            if (lastRegion.EndCoordinate.X < 0)
            {
                rightEdgeCoordinate = new Coordinate(AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }

            // Checking the right edge gap
            Line rightEdgeGap = new Line(rightEdgeCoordinate, lastRegion.EndCoordinate);
            if (rightEdgeGap.Length >= DriveContext.ASCENT_WIDTH)
            {
                double angle;
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
                else
                {
                    // midpoint.X is 0
                    angle = currCompass; 
                }

                if (Math.Abs(idealDirection - angle) < bestAngle)
                {
                    bestAngle = angle;
                    bestGap = rightEdgeGap;
                }
            }

            // Iterate through the rest of the gaps to find the bestGap by calculating valid (large enough) gaps as Line objects
            for (int i = 0; i < regions.Count - 2; i++) // don't iterate through entire list, will result in index out of bounds error on rightRegion assignment
            {
                Region leftRegion = regions[i];
                Region rightRegion = regions[i + 1];
                
                // Gap is distance, just needs to be big enough (the width of the robot)
                double gap = Plot.GapDistanceBetweenRegions(leftRegion, rightRegion);
                if (gap >= DriveContext.ASCENT_WIDTH)
                {
                    Line gapLine = new Line(new Coordinate(leftRegion.EndCoordinate.X, leftRegion.EndCoordinate.Y, CoordSystem.Cartesian), 
                                            new Coordinate(rightRegion.StartCoordinate.X, rightRegion.StartCoordinate.Y, CoordSystem.Cartesian));

                    Coordinate midpoint = gapLine.FindMidpoint();
                    if (bestGap == null)
                    {
                        bestGap = gapLine;
                    }
                    else
                    {
                        double angle;

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
                        else
                        {
                            // midpoint.X is 0
                            angle = currCompass; 
                        }

                        // Check if the gap found gets us closer to our destination 
                        if (Math.Abs(idealDirection - angle) < bestAngle)
                        {
                            bestAngle = angle;
                            bestGap = gapLine;
                        }
                    }
                }
            }

            return bestGap;
        }

        public bool IsTaskComplete()
        {
            return isTaskComplete;
        }

    }
}
