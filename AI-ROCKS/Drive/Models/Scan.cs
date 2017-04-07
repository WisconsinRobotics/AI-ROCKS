using System;

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

        public DriveCommand FindNextDriveCommand()
        {
            short ascentHeading = AscentPacketHandler.Compass;

            if (isAligningToHeading)
            {
                // Turn toward heading
                // Scan, use heading as reference

                GPS ascent = AscentPacketHandler.GPSData;
                double headingToGate = ascent.GetHeadingTo(this.gate);

                // Have reached heading. Start turning right
                if (IMU.IsHeadingWithinThreshold(ascentHeading, headingToGate, HEADING_THRESHOLD))
                {
                    this.isAligningToHeading = false;
                    this.isScanning = true;
                    scanStartHeading = ascentHeading;

                    return DriveCommand.RightTurn(DriveCommand.SPEED_VISION_SCAN);
                }

                // Turn toward heading angle
                if (ascentHeading < ((headingToGate + 180) % 360) && ascentHeading > headingToGate)
                {
                    return DriveCommand.LeftTurn(DriveCommand.SPEED_VISION_SCAN);
                }
                else
                {
                    return DriveCommand.RightTurn(DriveCommand.SPEED_VISION_SCAN);
                }
            }
            else
            {
                // Scan
                // ... more to do for this case

                // Increment deltaTheta accordingly
                deltaTheta = (ascentHeading - scanStartHeading + 360) % 360;

                if (deltaTheta > 345)
                {
                    // Use boolean incase of wrap around to 0 due to mod
                    isScanNearlyComplete = true;
                }

                return DriveCommand.RightTurn(DriveCommand.SPEED_VISION_SCAN);
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
