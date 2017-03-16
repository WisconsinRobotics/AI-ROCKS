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
    }
}
