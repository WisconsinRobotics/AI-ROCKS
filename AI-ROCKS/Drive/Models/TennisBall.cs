using System;
using System.Drawing;
using Emgu.CV.Structure;

using AI_ROCKS.Drive.DriveStates;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.Models
{
    class TennisBall
    {
        private Coordinate centerPoint;     // With respect to image frame, not distance in space (like an obstacle)
        private Double radius;
        private Double distanceToCenter;
        private Double angle;
        

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
                this.centerPoint = new Coordinate(ball.CenterPoint);
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
        /// <summary>
        /// Find distance using the perceived width, in pixels, of an object.
        /// </summary>
        /// <param name="perceivedWidth">Perceived width, in pixels, of an object/</param>
        /// <returns>Double - distance to center, in meters.</returns>
        private Double FindDistanceToCenter(Double perceivedWidth)
        {
            Double distanceFt = (Camera.KNOWN_WIDTH * Camera.FOCAL_LENGTH) / perceivedWidth;
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
