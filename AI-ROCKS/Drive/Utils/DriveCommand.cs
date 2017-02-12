using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        private byte[] magnitude;
        private byte[] direction;
        private double angle;


        public DriveCommand(byte[] magnitude, byte[] direction)
        {
            this.magnitude = magnitude;
            this.direction = direction;
        }

        public DriveCommand(double angle, byte speed)
        {
            // Value checking for valid speed - change speed to be not a byte? Enum, something else?

            // Use angle and speed to calculate what the resultant magnitude and direction are for all 6 wheels

            // Set magnitude and direction accordingly
        }


        public byte[] Magnitude
        {
            get { return this.magnitude; }
            set { this.magnitude = value; }
        }

        public byte[] Direction
        {
            get { return this.direction; }
            set { this.direction = value; }
        }

        public double Angle
        {
            get { return this.angle; }
            set { this.angle = value; }
        }

        public Tuple<byte[], byte[]> Command
        {
            get { return new Tuple<byte[], byte[]>(magnitude, direction); }
        }
    }
}
