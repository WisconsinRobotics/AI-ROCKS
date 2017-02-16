using System;

namespace AI_ROCKS.Drive
{
    class GPS
    {
        private double latitude;
        private double longitude;


        public GPS(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }


        public double Latitude
        {
            get { return this.latitude; }
            set { this.latitude = value; }
        }

        public double Longitude
        {
            get { return this.longitude; }
            set { this.longitude = value; }
        }
    }
}
