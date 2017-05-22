using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_ROCKS.Drive.Models
{
    class DetectedBall
    {
        private TennisBall ball;
        private Double cameraDistance;
        private Double gpsDistance;
        private Double timestampMillis;

        public DetectedBall(TennisBall ball, Double gpsDistance, Double timestampMillis)
        {
            this.ball = ball;
            this.gpsDistance = gpsDistance;
            this.cameraDistance = this.ball.DistanceToCenter;
            this.timestampMillis = timestampMillis;
        }


        public TennisBall TennisBall { get { return this.TennisBall; } }

        public Double CameraDistance { get { return this.cameraDistance; } }

        public Double GPSDistance { get { return this.gpsDistance; } }

        public Double TimestampMillis { get { return this.timestampMillis; } }
    }
}
