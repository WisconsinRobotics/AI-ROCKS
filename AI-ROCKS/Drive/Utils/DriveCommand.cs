using System;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        public const byte CLEAR_OBSTACLE_SPEED = 2;     //TODO verify
        private const double STRAIGHT = 0;              //TODO look at depending on what we do for angles (i.e. where is 0)
        private const double RIGHT = Math.PI / 2;       //TODO look at depending on what we do for angles (i.e. where is 0)
        private const byte SPEED = 2;                   // TODO put this somewhere else

        private sbyte left;
        private sbyte right;
        private double angle;


        public DriveCommand(sbyte left, sbyte right)
        {
            this.left = left;
            this.right = right;
        }

        public DriveCommand(double angle, byte speed)
        {
            // Value checking for valid speed - change speed to be not a byte? Enum, something else?

            // Use angle and speed to calculate what the resultant magnitude and direction are for all 6 wheels

            if (angle == 0)
            {
                // drive straight
            }
            else if (angle < Math.PI/2)
            {
                // Turn right
                this.left = (sbyte)SPEED;
                this.right = -SPEED;

            }
            else if (angle < 2 * Math.PI && angle > 3 * Math.PI / 2)
            {
                // Turn left
                this.left = -SPEED;
                this.right = (sbyte)SPEED;
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

            // TODO verify angle
            DriveCommand straight = new DriveCommand(STRAIGHT, speed);
            return straight;
        }

        public static DriveCommand RightTurn(byte speed)
        {
            // Create DriveCommand that represents a Right command of specified speed

            // TODO verify angle
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

        public double Angle
        {
            get { return this.angle; }
            set { this.angle = value; }
        }

        public Tuple<sbyte, sbyte> Command
        {
            get { return new Tuple<sbyte, sbyte>(left, right); }
        }
    }
}
