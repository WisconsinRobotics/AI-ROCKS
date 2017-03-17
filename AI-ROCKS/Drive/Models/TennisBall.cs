using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.Models
{
    class TennisBall
    {
        private Coordinate centerPoint;
        private Double radius;
        private Double distanceToCenter;
        private Double angle;


        public TennisBall(Coordinate centerPoint, Double radius, Double distanceToCenter)
        {
            this.centerPoint = centerPoint;
            this.radius = radius;
            this.distanceToCenter = distanceToCenter;
            this.angle = centerPoint.Theta;
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
    }
}
