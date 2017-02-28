using System;
using System.Collections.Generic;
using System.Linq;
using AI_ROCKS.Services;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class ObstacleAvoidanceDriveState : IDriveState
    {
        // TODO move all of these to a more appropriate place
        private const long LRF_MAX_RELIABLE_DISTANCE = 6000;    // TODO get from LRFLibrary
        private Line lrfLeftFOV;                                // TODO put this somewhere else
        private Line lrfRightFOV;                               // TODO put this somewhere else


        public ObstacleAvoidanceDriveState()
        {
            // TODO make this accessible from all DriveStates - here just for testing (aka un-hack-ify)
            // Make Lines to represent the edges of the field of view (FOV)
            lrfLeftFOV = 
                new Line(new Coordinate(0, 0, CoordSystem.Cartesian), new Coordinate(-1 * LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));
            lrfRightFOV = 
                new Line(new Coordinate(0, 0, CoordSystem.Cartesian), new Coordinate(LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));
        }


        public DriveCommand FindNextDriveCommand()
        {
            return DriveCommand.Straight(DriveCommand.OBSTACLE_DRIVE_STATE_SPEED);
        }

        public StateType GetNextStateType()
        {
            return StateType.ObstacleAvoidanceState;
        }

        public Line FindBestGap(Plot obstacles)
        {
            List<Region> regions = obstacles.Regions;

            //regions.RemoveAt(regions.Count - 1);
            //regions.RemoveAt(0);
            
            double bestGapDistance = 0;
            Line bestGap = null;

            // Sanity check - if zero Regions exist, return Line representing gap straight in front of Ascent
            if (regions.Count == 0)
            {
                // Make Line that is twice the width of Ascent and 1/2 the maximum distance away to signify 
                // the best gap is straight ahead of us
                Coordinate leftCoord = new Coordinate(-DriveContext.ASCENT_WIDTH, LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);
                Coordinate rightCoord = new Coordinate(DriveContext.ASCENT_WIDTH, LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);

                bestGap = new Line(leftCoord, rightCoord);
                return bestGap;
            }

            // Check first and last Region gaps (may be same Region if only one Region)
            Region firstRegion = regions.ElementAt(0);
            Region lastRegion = regions.ElementAt(regions.Count - 1);

            // Check if leftmost Coordinate in the leftmost Region is on the right half of the entire FOV. If it is, make leftEdgeCoordinate where the 
            // max -acceptable-range meets the left FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate leftEdgeCoordinate = Line.FindClosestPointOnLine(lrfLeftFOV, firstRegion.StartCoordinate);
            if (firstRegion.StartCoordinate.X > 0)
            {
                leftEdgeCoordinate = new Coordinate(-AutonomousService.OBSTACLE_DETECTION_DISTANCE, 0, CoordSystem.Cartesian);
            }
            Line leftEdgeGap = new Line(leftEdgeCoordinate, firstRegion.StartCoordinate);

            // Check if rightmost Coordinate in the rightmost Region is on the left half of the entire FOV. If it is, make rightEdgeCoordinate where the
            // max -acceptable-range meets the right FOV line, since the FindClosestPointOnLine function return will cause errors for 180 degree FOV.
            Coordinate rightEdgeCoordinate = Line.FindClosestPointOnLine(lrfRightFOV, lastRegion.EndCoordinate);
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
