using System;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        public const byte CLEAR_OBSTACLE_SPEED = 10;            //TODO verify
        public const byte OBSTACLE_DRIVE_STATE_SPEED = 20;      // TODO put this somewhere else - Make it work for multiple states too

        private const double STRAIGHT = 0;              //TODO look at depending on what we do for angles (i.e. where is 0)
        private const double RIGHT = Math.PI / 2;       //TODO look at depending on what we do for angles (i.e. where is 0)

        private sbyte left;
        private sbyte right;
        //private double angle;


        // Make constructor from Coordinate?

        public DriveCommand(sbyte left, sbyte right, byte speed)
        {
            this.left = left;
            this.right = right;
        }

        public DriveCommand(double angle, byte speed)
        {
            // Value checking for valid speed - change speed to be not a byte? Enum, something else?

            // Use angle and speed to calculate what the resultant magnitude and direction are for all 6 wheels

            if (angle.Equals(0))
            {
                // Drive straight
                this.left = (sbyte)speed;
                this.right = (sbyte)speed;
            }
            else if (angle < Math.PI/2)
            {
                // Turn right
                this.left = (sbyte)speed;
                this.right = (sbyte)-speed;

            }
            else if (angle > 3 * Math.PI / 2 && angle < 2 * Math.PI)
            {
                // Turn left
                this.left = (sbyte)-speed;
                this.right = (sbyte)speed;
            }
            else
            {
                // Problem
                // TODO handle
            }
        }

        public static DriveCommand Straight(byte speed)
        {
            // Create DriveCommand that represents a Straight command of specified speed
            DriveCommand straight = new DriveCommand(STRAIGHT, speed);
            return straight;
        }

        public static DriveCommand RightTurn(byte speed)
        {
            // Create DriveCommand that represents a Right command of specified speed
            DriveCommand right = new DriveCommand(RIGHT, speed);
            return right;
        }

        public sbyte Left
        {
            get { return this.left; }
            set { this.left = value; }
        }

        public sbyte Right
        {
            get { return this.right; }
            set { this.right = value; }
        }

        /*
        public double Angle
        {
            get { return this.angle; }
            set { this.angle = value; }
        }
        */

        public Tuple<sbyte, sbyte> Command
        {
            get { return new Tuple<sbyte, sbyte>(left, right); }
        }
    }
}
