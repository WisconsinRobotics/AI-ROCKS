using System;

namespace AI_ROCKS.Drive.Models
{
    class GPS
    {
        private float latDegrees;
        private float latMinutes;
        private float latSeconds;
        private float longDegrees;
        private float longMinutes;
        private float longSeconds;


        public GPS(float latDegrees, float latMinutes, float latSeconds, float longDegrees, float longMinutes, float longSeconds)
        {
            this.latDegrees = latDegrees;
            this.latMinutes = latMinutes;
            this.latSeconds = latSeconds;
            this.longDegrees = longDegrees;
            this.longMinutes = longMinutes;
            this.longSeconds = longSeconds;
        }

        public Tuple<double, double> ToDecimalDegrees()
        {
            // TODO check sign
            double latDecimalDegrees = latDegrees + (latMinutes + latSeconds / 60) / 60;
            double longDecimalDegrees = longDegrees + (longMinutes + longSeconds / 60) / 60;

            return new Tuple<double, double>(latDecimalDegrees, longDecimalDegrees);
        }

        public Tuple<double, double> ToRadians()
        {
            // TODO check sign
            Tuple <double, double> decimalDegrees = this.ToDecimalDegrees();
            double latDecimalDegrees = decimalDegrees.Item1;
            double longDecimalDegrees = decimalDegrees.Item2;

            double latRadians = latDecimalDegrees * Math.PI / 180;
            double longRadians = longDecimalDegrees * Math.PI / 180;

            return new Tuple<double, double>(latRadians, longRadians);
        }

        /// <summary>
        /// Distance between two GPS points, using the haversine formula.
        /// Refer here for more info: http://www.movable-type.co.uk/scripts/latlong.html.
        /// </summary>
        /// <param name="other">GPS to measure distance to.</param>
        /// <returns>double - the great-circle distance between this and `other`, in meters.</returns>
        public double GetDistanceTo(GPS other)
        {
            // TODO this is used in GeoCoordinate - use that underneath our GPS?

            Tuple<double, double> radians = this.ToRadians();
            Tuple<double, double> otherRadians = other.ToRadians();

            // Haversine formula
            double R = 6371e3; // metres
            double latAscent = radians.Item1;
            double latGate = otherRadians.Item1;
            double Δlat = latGate - latAscent;
            double Δlong = otherRadians.Item2 - radians.Item2;

            double a = Math.Sin(Δlat / 2) * Math.Sin(Δlat / 2) +
                    Math.Cos(latAscent) * Math.Cos(latGate) *
                    Math.Sin(Δlong / 2) * Math.Sin(Δlong / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = R * c;

            return distance;
        }

        /// <summary>
        /// Find heading/bearing toward another GPS points. This uses Rhumb bearing calculations.
        /// Refer here for more info: http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>
        /// <param name="other">GPS to measure heading to.</param>
        /// <returns>double - the heading toward `other`, in degrees using Azimuth angle (i.e. compass angles).</returns>
        public double GetHeadingTo(GPS other)
        {
            // TODO return azimuth-based angle. Is this good or return something else? -> Depends on how our IMU compass data works

            Tuple<double, double> radians = this.ToRadians();
            Tuple<double, double> otherRadians = other.ToRadians();

            var Δψ = Math.Log(Math.Tan(Math.PI / 4 + otherRadians.Item1 / 2) / Math.Tan(Math.PI / 4 + radians.Item1 / 2));
            var Δlon = otherRadians.Item2 - radians.Item2;

            // If dLon over 180° take shorter rhumb line across the anti-meridian:
            if (Math.Abs(Δlon) > Math.PI) Δlon = Δlon > 0 ? -(2 * Math.PI - Δlon) : (2 * Math.PI + Δlon);

            var bearing = Math.Atan2(Δlon, Δψ);
            var bearingDeg = bearing * 180 / Math.PI;

            while (bearingDeg < 0)
            {
                bearingDeg += 360;
            }

            return bearingDeg;


            // Possibly more accurate? Left for reference
            /*
            var y = Math.Sin(otherRadians.Item2 - radians.Item2) * Math.Cos(otherRadians.Item1);
            var x = Math.Cos(radians.Item1) * Math.Sin(otherRadians.Item1) -
                    Math.Sin(radians.Item1) * Math.Cos(otherRadians.Item1) * Math.Cos(otherRadians.Item2 - radians.Item2);
            var bearing = Math.Atan2(y, x); //radians
            */
        }

        public static double ConvertAzimuthToUnitCircle(double azimuthAngle)
        {
            // Convert from Azimuth system that compasses use (0 = N, 90 = E, 180 = S, 270 = W - clockwise)
            // to unit circle that ObstacleLibrary uses (0 = E, 90 = N, 180 = W, 270 = S - counterclockwise).
            // Do through equation: h = 450 - a. If h > 360, h = h - 360.  
            double unitCircleDeg = 450 - azimuthAngle;
            if (unitCircleDeg >= 360)
            {
                unitCircleDeg -= 360;
            }

            double unitCircleRad = unitCircleDeg * Math.PI / 180.0;
            return unitCircleRad;
        }

        public float LatDegrees
        {
            get { return this.latDegrees; }
        }

        public float LatMinutes
        {
            get { return this.latMinutes; }
        }

        public float LatSeconds
        {
            get { return this.latSeconds; }
        }

        public float LongDegrees
        {
            get { return this.longDegrees; }
        }

        public float LongMinutes
        {
            get { return this.longMinutes; }
        }

        public float LongSeconds
        {
            get { return this.longSeconds; }
        }

        public override String ToString()
        {
            String stringy = "latDeg: " + this.latDegrees
                + " latMin: " + this.latMinutes
                + " latSec: " + this.latSeconds
                + " longDeg: " + this.longDegrees
                + " longMin: " + this.longMinutes
                + " longSec: " + this.longSeconds;

            return stringy;
        }
    }
}
