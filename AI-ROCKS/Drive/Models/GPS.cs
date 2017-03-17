using System;

namespace AI_ROCKS.Drive.Models
{
    class GPS
    {
        private short latDegrees;
        private short latMinutes;
        private short latSeconds;
        private short longDegrees;
        private short longMinutes;
        private short longSeconds;


        public GPS(short latDegrees, short latMinutes, short latSeconds, short longDegrees, short longMinutes, short longSeconds)
        {
            this.latDegrees = latDegrees;
            this.latMinutes = latMinutes;
            this.latSeconds = latSeconds;
            this.longDegrees = longDegrees;
            this.longMinutes = longMinutes;
            this.longSeconds = longSeconds;
        }


        public short LatDegrees
        {
            get { return this.latDegrees; }
        }

        public short LatMinutes
        {
            get { return this.latMinutes; }
        }

        public short LatSeconds
        {
            get { return this.latSeconds; }
        }

        public short LongDegrees
        {
            get { return this.longDegrees; }
        }

        public short LongMinutes
        {
            get { return this.longMinutes; }
        }

        public short LongSeconds
        {
            get { return this.longSeconds; }
        }
    }
}
