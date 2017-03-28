using System;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
        public const byte SPEED_NORMAL_OPERATION = 50;
        public const byte SPEED_CLEAR_OBSTACLE = 40;
        public const byte SPEED_AVOID_OBSTACLE = 30;

        private const double STRAIGHT = Math.PI / 2;
        private const double RIGHT = 0;
        private const double LEFT = Math.PI;

        private sbyte left;
        private sbyte right;

        // Make constructor from Coordinate?

        public DriveCommand(sbyte left, sbyte right, byte speed)
        {
            this.left = left;
            this.right = right;
        }

        public DriveCommand(double angle, byte speed)
        {
            // Value checking for valid speed - change speed to be not a byte? Enum, something else?

            // Use angle and speed to calculate what the resultant magnitude and direction are for left and right sides
            if (angle.CompareTo(Math.PI / 2) == 0)
            {
                // Drive straight
                this.left = (sbyte)speed;
                this.right = (sbyte)speed;
            }
            else if (angle.CompareTo(Math.PI/2) < 0)
            {
                // Turn right
                this.left = (sbyte)speed;
                this.right = (sbyte)-speed;

            }
            else if (angle.CompareTo(Math.PI / 2) > 0 && angle.CompareTo(Math.PI) <= 0)
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

        public static DriveCommand LeftTurn(byte speed)
        {
            DriveCommand left = new DriveCommand(LEFT, speed);
            return left;
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
        
        public Tuple<sbyte, sbyte> Command
        {
            get { return new Tuple<sbyte, sbyte>(left, right); }
        }
    }
}
