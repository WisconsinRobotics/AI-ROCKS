using System;

using AI_ROCKS.Drive.Utils;

namespace AI_ROCKS.Drive
{
    class DriveCommand
    {
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
            DriveCommand straight = new DriveCommand(Angle.STRAIGHT, speed);
            return straight;
        }

        public static DriveCommand RightTurn(byte speed)
        {
            // Create DriveCommand that represents a Right command of specified speed
            DriveCommand right = new DriveCommand(Angle.RIGHT, speed);
            return right;
        }

        public static DriveCommand LeftTurn(byte speed)
        {
            // Create DriveCommand that represents a Left command of specified speed
            DriveCommand left = new DriveCommand(Angle.LEFT, speed);
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
