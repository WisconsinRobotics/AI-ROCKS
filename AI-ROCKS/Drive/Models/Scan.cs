using System;

using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;

namespace AI_ROCKS.Drive.Models
{
    class Scan
    {
        GPS gate;

        bool isAligningToHeading;       // Is the Scan currently aligning to the heading of the gate
        bool isScanning;                // Is the Scan currently scanning (and not aligning)

        bool isScanNearlyComplete = false;    // Kind of a hack
        double scanStartHeading;
        double deltaTheta = 0.0;

        bool isDrivingStraightForDuration;
        long driveTowardHeadingForMillis = 0;
        long driveStraightUntilMillis;

        public const double HEADING_THRESHOLD = 5.0; // 5 degrees


        public Scan(GPS gate, bool useHeading)
        {
            this.gate = gate;

            this.isAligningToHeading = useHeading;
            this.isScanning = !useHeading;
            if (!useHeading)
            {
                scanStartHeading = AscentPacketHandler.Compass;
            }
        }

        // Implies we use heading first, then drive toward it.
        // Once aligned, drive straight for driveTowardHeadingForMillis milliseconds
        public Scan(GPS gate, long driveTowardHeadingForMillis)
        {
            this.gate = gate;

            this.isAligningToHeading = true;
            this.isScanning = false;

            this.driveTowardHeadingForMillis = driveTowardHeadingForMillis;
            this.isDrivingStraightForDuration = false;
        }

        public DriveCommand FindNextDriveCommand()
        {
            short ascentHeading = AscentPacketHandler.Compass;

            if (this.isAligningToHeading)
            {
                // Turn toward heading
                // Scan, use heading as reference

                GPS ascent = AscentPacketHandler.GPSData;
                double headingToGate = ascent.GetHeadingTo(this.gate);

                Console.Write("Scan aligning toward heading | compass: " + ascentHeading + " | Heading to gate: " + headingToGate + " | ");



                // Have reached heading. Start turning right
                if (IMU.IsHeadingWithinThreshold(ascentHeading, headingToGate, HEADING_THRESHOLD))
                {
                    this.isAligningToHeading = false;

                    // If driving for a certain duration
                    if (driveTowardHeadingForMillis > 0)
                    {
                        // Drive straight for a duration
                        this.isDrivingStraightForDuration = true;
                        this.driveStraightUntilMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + this.driveTowardHeadingForMillis;

                        return DriveCommand.Straight(Speed.VISION_SCAN);
                    }
                    else
                    {
                        // Start scan
                        this.isScanning = true;
                        scanStartHeading = ascentHeading;

                        return DriveCommand.RightTurn(Speed.VISION_SCAN);
                    }
                }

                // Turn toward heading angle
                if (ascentHeading < ((headingToGate + 180) % 360) && ascentHeading > headingToGate)
                {
                    return DriveCommand.LeftTurn(Speed.VISION_SCAN);
                }
                else
                {
                    return DriveCommand.RightTurn(Speed.VISION_SCAN);
                }
            }
            else if (this.isDrivingStraightForDuration)
            {
                if (this.driveStraightUntilMillis > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    // Drive straight for duration
                    return DriveCommand.Straight(Speed.VISION_SCAN);
                }
                else
                {
                    // Start scan
                    this.isDrivingStraightForDuration = false;

                    this.isScanning = true;
                    scanStartHeading = ascentHeading;

                    return DriveCommand.RightTurn(Speed.VISION_SCAN);
                }
            }
            else
            {
                // Scan
                // ... more to do for this case
                Console.Write("Scan scanning | ");

                // Increment deltaTheta accordingly
                deltaTheta = (ascentHeading - scanStartHeading + 360) % 360;

                Console.Write("Scan scanning | compass: " + ascentHeading + " | deltaTheta: " + deltaTheta + " | ");

                if (deltaTheta > 345)
                {
                    // Use boolean incase of wrap around to 0 due to mod
                    isScanNearlyComplete = true;
                }

                return DriveCommand.RightTurn(Speed.VISION_SCAN);
            }
        }

        public bool IsComplete()
        {
            short ascentHeading = AscentPacketHandler.Compass;

            GPS ascent = AscentPacketHandler.GPSData;
            double headingToGate = ascent.GetHeadingTo(this.gate);

            return isScanning && isScanNearlyComplete && IMU.IsHeadingWithinThreshold(ascentHeading, headingToGate, HEADING_THRESHOLD);
        }
    }
}
