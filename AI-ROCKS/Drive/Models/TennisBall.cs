using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV.Structure;
using ObstacleLibrarySharp;
using AI_ROCKS.Drive.DriveStates;

namespace AI_ROCKS.Drive.Models
{
    class TennisBall
    {
        private Coordinate centerPoint;     // With respect to image frame, not distance in space (like an obstacle)
        private Double radius;
        private Double distanceToCenter;
        private Double angle;


        // TODO have this?
        /*
        public TennisBall(Coordinate centerPoint, Double radius, Double distanceToCenter)
        {
            this.centerPoint = centerPoint;
            this.radius = radius;
            this.distanceToCenter = distanceToCenter;
            this.angle = centerPoint.Theta;
        }
        */

        // TODO have this? Or have only CircleF constructor
        /*
        public TennisBall(CircleF circle, Double distanceToCenter, Double angle)
        {
            PointF center = circle.Center;
            this.centerPoint = new Coordinate(center.X, center.Y, CoordSystem.Cartesian);

            this.radius = circle.Radius;
            this.distanceToCenter = distanceToCenter;
            this.angle = angle;
        }
        */

        public TennisBall(CircleF circle)
        {
            PointF center = circle.Center;
            this.centerPoint = new Coordinate(center.X, center.Y, CoordSystem.Cartesian);

            this.radius = circle.Radius;
            this.distanceToCenter = FindDistanceToCenter(2 * circle.Radius);
            this.angle = FindAngle(circle, distanceToCenter);
        }

        public TennisBall(TennisBall ball)
        {
            if (ball != null)
            {
                this.centerPoint = ball.CenterPoint;
                this.radius = ball.Radius;
                this.distanceToCenter = ball.DistanceToCenter;
                this.angle = ball.Angle;
            }
        }


        public Coordinate CenterPoint
        {
            get { return this.centerPoint; }
        }

        public Double Radius
        {
            get { return this.radius; }
        }

        public Double DistanceToCenter
        {
            get { return this.distanceToCenter; }
        }

        public Double Angle
        {
            get { return this.angle; }
        }

        // perveivedWidth = pixels
        private Double FindDistanceToCenter(Double perceivedWidth)
        {
            // TODO better usage of these consts (somewhere in some handler?)
            Double distanceFt = (VisionDriveState.KNOWN_WIDTH * VisionDriveState.FOCAL_LENGTH) / perceivedWidth;
            return distanceFt / 3.28084;
        }

        private Double FindAngle(CircleF circle, Double distanceToCenter)
        {
            PointF center = circle.Center;

            // Create "top-down" view of ball with respect to x and y
            float x = center.X - VisionDriveState.PIXELS_WIDTH / 2;
            float y = (float)Math.Sqrt(Math.Pow(distanceToCenter, 2) - Math.Pow(x, 2));

            Coordinate topDown = new Coordinate(x, y, CoordSystem.Cartesian);

            return topDown.Theta;
        }

    }
}
