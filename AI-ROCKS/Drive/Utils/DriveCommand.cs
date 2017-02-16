using System;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        public const byte CLEAR_OBSTACLE_SPEED = 2;     //TODO verify
        private const double STRAIGHT = 0;              //TODO look at depending on what we do for angles (i.e. where is 0)
        private const double RIGHT = Math.PI / 2;              //TODO look at depending on what we do for angles (i.e. where is 0)

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

        public static DriveCommand Straight(byte speed)
        {
            // Create DriveCommand that represents a Straight command of specified speed

            // TODO verify angle
            DriveCommand straight = new DriveCommand(STRAIGHT, speed);
            return straight;
        }

        public static DriveCommand Right(byte speed)
        {
            // Create DriveCommand that represents a Right command of specified speed

            // TODO verify angle
            DriveCommand straight = new DriveCommand(STRAIGHT, speed);
            return straight;
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
