using System;

namespace AI_ROCKS.Drive.Models
{
    class IMU
    {
        private short xAccel;
        private short yAccel;
        private short zAccel;
        private short xOrient;
        private short yOrient;
        private short zOrient;


        public IMU(short xAccel, short yAccel, short zAccel, short xOrient, short yOrient, short zOrient)
        {
            this.xAccel = xAccel;
            this.yAccel = yAccel;
            this.zAccel = zAccel;
            this.xOrient = xOrient;
            this.yOrient = yOrient;
            this.zOrient = zOrient;
        }


        public static bool IsHeadingWithinThreshold(double heading, double targetHeading, double threshold)
        {
            double lowBound = targetHeading - threshold;
            double highBound = targetHeading + threshold;
            if (lowBound < 0)
            {
                lowBound += 360;
            }
            if (highBound >= 360)
            {
                highBound %= 360;
            }

            if (lowBound < highBound)
            {
                return lowBound < heading && heading < highBound;
            }
            else
            {
                return !(highBound < heading && heading < lowBound);
            }
        }

        public short XAccel
        {
            get { return this.xAccel; }
        }

        public short YAccel
        {
            get { return this.yAccel; }
        }

        public short ZAccel
        {
            get { return this.zAccel; }
        }

        public short XOrient
        {
            get { return this.xOrient; }
        }

        public short YOrient
        {
            get { return this.yOrient; }
        }

        public short ZOrient
        {
            get { return this.zOrient; }
        }

        public override String ToString()
        {
            String stringy = "xAccel: " + this.xAccel
                + " yAccel: " + this.yAccel
                + " zAccel: " + this.zAccel
                + " xOrient: " + this.xOrient
                + " yOrient: " + this.yOrient
                + " zOrient: " + this.zOrient;

            return stringy;
        }
    }
}
