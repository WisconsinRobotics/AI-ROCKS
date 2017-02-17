using System;
using System.Collections.Generic;
using System.Linq;

using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class ObstacleAvoidanceDriveState : IDriveState
    {
        // TODO move all of these to a more appropriate place
        private const float ASCENT_WIDTH = 200;                 // TODO fill in with actual value
        private const long LRF_MAX_RELIABLE_DISTANCE = 6000;    // TODO get from LRFLibrary
        private Line lrfLeftFOV;                                // TODO put this somewhere else
        private Line lrfRightFOV;                               // TODO put this somewhere else


        public ObstacleAvoidanceDriveState()
        {
            // TODO make this accessible from all DriveStates - here just for testing (aka un-hack-ify)
            lrfLeftFOV = new Line(new Coordinate(0, 0, CoordSystem.Cartesian), 
                new Coordinate(-1 * LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));
            lrfRightFOV = new Line(new Coordinate(0, 0, CoordSystem.Cartesian), 
                new Coordinate(LRF_MAX_RELIABLE_DISTANCE, 0, CoordSystem.Cartesian));
        }


        public DriveCommand FindNextDriveCommand()
        {
            return DriveCommand.Straight(DriveCommand.CLEAR_OBSTACLE_SPEED);
        }

        public StateType GetNextStateType()
        {
            return StateType.ObstacleAvoidanceState;
        }

        public Line FindBestGap(Plot obstacles)
        {
            List<Region> regions = obstacles.Regions;


            // Test code - delete
            //Coordinate c1 = new Coordinate(-100, 0, CoordSystem.Cartesian);
            //Coordinate c2 = new Coordinate(-100, 100, CoordSystem.Cartesian);
            //Coordinate c3 = new Coordinate(200, 300, CoordSystem.Cartesian);
            //Coordinate c4 = new Coordinate(200, 200, CoordSystem.Cartesian);

            //Region r1 = new Region(new List<Coordinate> { c1, c2 }, 4);
            //Region r2 = new Region(new List<Coordinate> { c3, c4 }, 4);
            //Region r3 = new Region(new List<Coordinate> { c1, c2, c3, c4 }, 4);

            //regions = new List<Region> { r3 }; 


            double bestGapDistance = 0;
            Line bestGap = null;

            // Sanity check - if zero Regions exist, return Line representing gap straight in front of Ascent
            if (regions.Count == 0)
            {
                // Make Line that is twice the width of Ascent and 1/2 the maximum distance away to signify 
                // the best gap is straight ahead of us
                Coordinate leftCoord = new Coordinate(-ASCENT_WIDTH, 
                    LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);
                Coordinate rightCoord = new Coordinate(ASCENT_WIDTH, 
                    LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);

                bestGap = new Line(leftCoord, rightCoord);
                return bestGap;
            }

            // Check the first and last Region gaps (these may be the same Region if only one Region)
            Region firstRegion = regions.ElementAt(0);
            Region lastRegion = regions.ElementAt(regions.Count - 1);

            // Get gap distance from first region to left FOV edge
            Coordinate leftEdgeCoordinate = Line.FindClosestPointOnLine(lrfLeftFOV, firstRegion.StartCoordinate);
            Line leftEdgeGap = new Line(leftEdgeCoordinate, firstRegion.StartCoordinate);

            // Get gap distance from last region to right FOV edge
            Coordinate rightEdgeCoordinate = Line.FindClosestPointOnLine(lrfRightFOV, lastRegion.EndCoordinate);
            Line rightEdgeGap = new Line(rightEdgeCoordinate, lastRegion.EndCoordinate);

            // Check two possible edge gaps for bestGap
            Line bestEdgeGap = leftEdgeGap.Length > rightEdgeGap.Length ? leftEdgeGap : rightEdgeGap;
            if (bestEdgeGap.Length > ASCENT_WIDTH)
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
                if (gapDistance > ASCENT_WIDTH)
                {
                    if (gapDistance > bestGapDistance)
                    {
                        bestGapDistance = gapDistance;
                        bestGap = new Line(leftRegion.EndCoordinate, rightRegion.StartCoordinate);
                    }
                }
            }

            // Return bestGap - will be null if no best gap is found
            return bestGap;
        }
    }
}
