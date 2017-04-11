using System;
using System.Collections.Generic;
using System.Linq;
using AI_ROCKS.Services;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class ObstacleAvoidanceDriveState : IDriveState
    {
        public ObstacleAvoidanceDriveState()
        {
        }


        public DriveCommand FindNextDriveCommand()
        {
            return DriveCommand.Straight(DriveCommand.SPEED_NORMAL_OPERATION);
        }

        public StateType GetNextStateType()
        {
            return StateType.ObstacleAvoidanceState;
        }

        public Line FindBestGap(Plot obstacles)
        {
            List<Region> regions = obstacles.Regions;

            double bestGapDistance = 0;
            Line bestGap = null;

            // Sanity check
            if (regions.Count == 0)
            {
                // Return Line representing gap straight in front of Ascent
                return DriveContext.GapStraightInFront();
            }

            // Check first and last Region gaps (may be same Region if only one Region)
            Region firstRegion = regions.ElementAt(0);
            Region lastRegion = regions.ElementAt(regions.Count - 1);

            // Check if leftmost Coordinate in the leftmost Region is on the right half of the entire FOV. If it is, make leftEdgeCoordinate where the 
            // max-acceptable-range meets the left FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate leftEdgeCoordinate = Line.FindClosestPointOnLine(DriveContext.LRF_LEFT_FOV_EDGE, firstRegion.StartCoordinate);
            if (firstRegion.StartCoordinate.X > 0)
            {
                leftEdgeCoordinate = new Coordinate(-AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }
            Line leftEdgeGap = new Line(leftEdgeCoordinate, firstRegion.StartCoordinate);

            // Check if rightmost Coordinate in the rightmost Region is on the left half of the entire FOV. If it is, make rightEdgeCoordinate where the
            // max-acceptable-range meets the right FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate rightEdgeCoordinate = Line.FindClosestPointOnLine(DriveContext.LRF_RIGHT_FOV_EDGE, lastRegion.EndCoordinate);
            if (lastRegion.EndCoordinate.X < 0)
            {
                rightEdgeCoordinate = new Coordinate(AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }
            Line rightEdgeGap = new Line(rightEdgeCoordinate, lastRegion.EndCoordinate);
            
            // Check two possible edge gaps for bestGap
            Line bestEdgeGap = leftEdgeGap.Length > rightEdgeGap.Length ? leftEdgeGap : rightEdgeGap;
            if (bestEdgeGap.Length > DriveContext.ASCENT_WIDTH)
            {
                bestGap = bestEdgeGap;
                bestGapDistance = bestEdgeGap.Length;
            }

            // Check all non-edge Regions for bestGap (only if there is more than 1 Region)
            for (int i = 0; i < regions.Count - 1; i++)
            {
                Region leftRegion = regions.ElementAt(i);
                Region rightRegion = regions.ElementAt(i + 1);

                double gapDistance = Plot.GapDistanceBetweenRegions(leftRegion, rightRegion);

                // If gap distance is big enough for robot to fit and larger than current bestGapDistance
                if (gapDistance > DriveContext.ASCENT_WIDTH)
                {
                    if (gapDistance > bestGapDistance)
                    {
                        bestGapDistance = gapDistance;
                        bestGap = new Line(leftRegion.EndCoordinate, rightRegion.StartCoordinate);
                    }
                }
            }

            return bestGap;
        }
    }
}
