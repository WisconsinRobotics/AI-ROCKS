using System;
using System.Collections.Generic;
using System.Linq;

using AI_ROCKS.Drive.Models;
using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class VisionDriveState : IDriveState
    {
        // Camera initialization
        const string CAMERA_USERNAME = "admin";
        const string CAMERA_PASSWORD = "i#3Er0b0";
        const string CAMERA_IP_MAST = "192.168.1.6";
        const string CAMERA_URL = "rtsp://" + CAMERA_USERNAME + ":" + CAMERA_PASSWORD + "@" + CAMERA_IP_MAST + ":554/cam/realmonitor?channel=1&subtype=0";
        const int CAMERA_DEVICE_ID = 0;

        // Camera constants
        public const int PIXELS_WIDTH = 1920;       // May change, make dynamic?
        public const int PIXELS_HEIGHT = 1080;      // May change, make dynamic?

        // Turning thresholds
        const int leftThreshold = 3 * PIXELS_WIDTH / 8;     // 3/8 from left
        const int rightThreshold = 5 * PIXELS_WIDTH / 8;    // 5/8 from left

        // Distance constants
        const double DISTANCE_SWITCH_TO_GPS = 6.0;
        const double DISTANCE_USE_HEADING = 6.0;
        const double DISTANCE_CLOSE_RANGE = 2.0;

        // Detection constants
        const int DROP_BALL_DELAY = 3000;   // maybe name this more appropriately lol

        // Navigation utils
        private const double BALL_REGION_THRESHOLD = 0.0872665;     // 5 degrees in radians
        private bool switchToGPS = false;

        // Navigation objects
        private GPS gate;
        private Scan scan;
        private Camera camera;

        int completedScans = 0;

        // Verification
        DetectedBallsQueue verificationQueue;   // For verification at end, not consistent logging of balls
        bool isWithinRequiredDistance = false;

        // Verification constants
        const int VERIFICATION_QUEUE_SIZE = 25;
        const double VERIFICATION_DISTANCE_PERCENTAGE = 0.80;   // 80%
        const int VERIFICATION_TIMESTAMP_THRESHOLD = 5000;      // 5 seconds


        public VisionDriveState(GPS gate)
        {
            this.camera = new Camera(CAMERA_URL);
            
            this.verificationQueue = new DetectedBallsQueue(VERIFICATION_QUEUE_SIZE);

            this.gate = gate;
            this.scan = null;
        }

        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            TennisBall ball = this.camera.GetBallCopy();

            // If verified to be within required distance, send success to ROCKS and halt
            if (this.isWithinRequiredDistance)
            {
                // Send success back to base station until receive ACK
                StatusHandler.SendSimpleAIPacket(Status.AIS_FOUND_GATE);
                Console.WriteLine("Within required distance - halting ");

                return DriveCommand.Straight(Speed.HALT);
            }

            // Recently detected ball but now don't now. Stop driving to redetect since we may have dropped it due to bouncing.
            if (ball == null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < this.camera.BallTimestamp + DROP_BALL_DELAY)
            {
                StatusHandler.SendSimpleAIPacket(Status.AIS_DROP_BALL);
                Console.WriteLine("Dropped ball - halting ");

                this.verificationQueue.Clear();
                return DriveCommand.Straight(Speed.HALT);
            }

            // Ball detected
            if (ball != null)
            {
                return DriveBallDetected(ball);
            }

            // No ball detected
            return DriveNoBallDetected(ball);
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            if (this.switchToGPS)
            {
                StatusHandler.SendDebugAIPacket(Status.AIS_SWITCH_TO_GPS, "Drive state switch: Vision to GPS.");
                return StateType.GPSState;
            }

            return StateType.VisionState;
        }

        // Given a Plot representing the obstacles, find Line representing the best gap.
        public Line FindBestGap(Plot obstacles)
        {
            /*
            Overview:
            
            See ball:
                1) Open path to ball?
                2) Region = ball?
                3) Kick back to GPS (?) (for now)

            Don't see ball:
                4) Turn camera mast - drive accordingly
                5) Compare IMU/GPS - orient accordingly and drive straight
            
             // Note: removed psuedocode corresponding to this logic - refer to previous commits to get it back
             */
            
            List<Region> regions = obstacles.Regions;
            Line bestGap = null;
            TennisBall ball = this.camera.GetBallCopy();

            // Sanity check - if zero Regions exist, return Line representing gap straight in front of Ascent
            if (regions.Count == 0)
            {
                // Make Line that is twice the width of Ascent and 1/2 the maximum distance away to signify the best gap is straight ahead of us
                Coordinate leftCoord = new Coordinate(-DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);
                Coordinate rightCoord = new Coordinate(DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);

                bestGap = new Line(leftCoord, rightCoord);
                return bestGap;
            }

            // If open path to ball exists, drive toward it
            if (IsOpenPathToBall(obstacles, ball) || IsBallDetectedRegionAndOpenPath(obstacles, ball))
            {
                Coordinate ballCoord = new Coordinate((float)ball.Angle, (float)ball.DistanceToCenter, CoordSystem.Polar);

                // Hack - make Line parallel to x-axis rather than perpendicular to Line to ball, but it works
                Coordinate leftGapCoord = new Coordinate(ballCoord.X - DriveContext.ASCENT_WIDTH / 2, ballCoord.Y, CoordSystem.Cartesian);
                Coordinate rightGapCoord = new Coordinate(ballCoord.X + DriveContext.ASCENT_WIDTH / 2, ballCoord.Y, CoordSystem.Cartesian);

                bestGap = new Line(leftGapCoord, rightGapCoord);

                return bestGap;               
            }

            // No ball/path to ball is found - kick back to GPS
            switchToGPS = true;
            return null;

            // TODO this logic! This will certainly not suffice in competition
        }

        public bool IsTaskComplete()
        {
            return this.isWithinRequiredDistance;
        }

        private DriveCommand DriveBallDetected(TennisBall ball)
        {
            // Sanity check
            if (ball == null)
            {
                this.verificationQueue.Clear();
                return null;
            }

            // Add to verification queue
            DetectedBall detectedBall = new DetectedBall(ball, AscentPacketHandler.GPSData.GetDistanceTo(this.gate), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            this.verificationQueue.Enqueue(detectedBall);

            // TODO debugging - delete
            Console.Write("Ball detected | Verifying ({0})... ", this.verificationQueue.Count);


            // Detected ball so no longer scan
            this.scan = null;

            // Within required distance - use verification queue to determine if we should send back success
            if (IsWithinRequiredDistance(ball))
            {
                if (this.verificationQueue.VerifyBallDetection(
                            VERIFICATION_DISTANCE_PERCENTAGE,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - VERIFICATION_TIMESTAMP_THRESHOLD,
                            DriveContext.REQUIRED_DISTANCE_FROM_BALL,
                            DriveContext.REQUIRED_DISTANCE_FROM_BALL + DriveContext.GPS_PRECISION))
                {
                    this.isWithinRequiredDistance = true;
                }

                Console.Write("WITHIN REQUIRED DISTANCE | ");

                // Halt to wait for success to be sent back to base station
                return DriveCommand.Straight(Speed.HALT);
            }

            return DriveTowardBall(ball);
        }

        private DriveCommand DriveNoBallDetected(TennisBall ball)
        {
            // Sanity check
            if (ball != null)
            {
                return null;
            }


            Console.Write("Ball not detected | ");

            // Clear verification queue if it has values
            this.verificationQueue.Clear();

            GPS ascent = AscentPacketHandler.GPSData;
            double distanceToGate = ascent.GetDistanceTo(this.gate);

            // Kick back to GPS
            if (distanceToGate > DISTANCE_SWITCH_TO_GPS)    // 6 meters
            {
                Console.WriteLine("Distance: " + distanceToGate + ". Switch to GPS");

                switchToGPS = true;
                return DriveCommand.Straight(Speed.HALT);
            }

            // Turn to face heading, drive toward it
            if (distanceToGate > DISTANCE_USE_HEADING)      // 3 meters
            {
                Console.WriteLine("Distance: " + distanceToGate + ". Turning toward heading to drive towrad it");

                short ascentHeading = AscentPacketHandler.Compass;
                double headingToGate = ascent.GetHeadingTo(this.gate);

                Console.Write("currCompass: " + ascentHeading + " | headingToGoal: " + headingToGate + " | distance: " + distanceToGate + " | ");

                // Aligned with heading. Start going straight
                if (IMU.IsHeadingWithinThreshold(ascentHeading, headingToGate, Scan.HEADING_THRESHOLD))
                {
                    return DriveCommand.Straight(Speed.VISION);
                }

                // Turn toward gate heading angle
                if (IMU.IsHeadingWithinThreshold(ascentHeading, (headingToGate + 90) % 360, 90))
                {
                    return DriveCommand.LeftTurn(Speed.VISION_SCAN);
                }
                else
                {
                    return DriveCommand.RightTurn(Speed.VISION_SCAN);
                }

                // Probably would work, kept as reference
                /*
                double lowBound = headingToGate;
                double highBound = (headingToGate + 180) % 360;

                if (lowBound < highBound)
                {
                    if (lowBound < ascentHeading && ascentHeading < highBound)
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
                    if (!(highBound < ascentHeading && ascentHeading < lowBound))
                    {
                        return DriveCommand.LeftTurn(DriveCommand.SPEED_VISION_SCAN);
                    }
                    else
                    {
                        return DriveCommand.RightTurn(DriveCommand.SPEED_VISION_SCAN);
                    }
                }
                */
            }

            // If scanning, complete scan
            if (this.scan != null)
            {
                Console.WriteLine("Scanning... Distance: " + distanceToGate);

                if (!this.scan.IsComplete())
                {
                    return scan.FindNextDriveCommand();
                }
                else
                {
                     this.completedScans++;
                    
                    // Clear scan, will rescan below
                    this.scan = null;
                }
            }

            if (this.completedScans >= 2)
            {
                // Align toward heading, drive for 5ish seconds, 
                StatusHandler.SendDebugAIPacket(Status.AIS_BEGIN_SCAN, "Distance > 2m: Driving 5m away, using heading as reference");
                Console.WriteLine("Distance: " + distanceToGate + ". Scanning (using heading). Driving 5m away...");

                this.scan = new Scan(this.gate, 10000);
            }
            else if (distanceToGate > DISTANCE_CLOSE_RANGE)      // 2 meters
            {
                // Turn toward heading
                // Scan, use heading as reference
                StatusHandler.SendDebugAIPacket(Status.AIS_BEGIN_SCAN, "Distance > 2m: Using heading as reference");
                Console.WriteLine("Distance: " + distanceToGate + ". Scanning (using heading)...");
                
                this.scan = new Scan(this.gate, true);
            }
            else
            {
                StatusHandler.SendDebugAIPacket(Status.AIS_BEGIN_SCAN, "Distance < 2m: Not using heading as reference");

                Console.WriteLine("Distance: " + distanceToGate + ". Scanning...");
                
                // Scan
                // ... more to do for this case

                this.scan = new Scan(this.gate, false);
            }

            return scan.FindNextDriveCommand();
        }

        private bool IsOpenPathToBall(Plot obstacles, TennisBall ball)
        {
            // TODO what if we dropped the ball for one frame - check timing threshold
            if (ball == null | obstacles == null)
            {
                return false;
            }

            Coordinate ballCoord = new Coordinate((float)ball.Angle, (float)ball.DistanceToCenter, CoordSystem.Polar);
            Line lineToBall = new Line(new Coordinate(0, 0, CoordSystem.Cartesian), ballCoord);
            
            foreach (Region region in obstacles.Regions)
            {
                foreach (Coordinate coord in region.ReducedCoordinates)
                {
                    // TODO Don't check depth now on account of it possibly being inaccurate. If it's reliable, add depth check here

                    if (Line.DistanceFromLine(lineToBall, coord) <= DriveContext.ASCENT_WIDTH / 2)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        // TODO horrible function name but whatever (for now)
        private bool IsBallDetectedRegionAndOpenPath(Plot obstacles, TennisBall ball)
        {
            // TODO what if we dropped the ball for one frame - check timing threshold
            if (ball == null)
            {
                return false;
            }

            bool ballIsRegion = false;
            int ballRegionIndex = 0;

            // Is a Region entirely within the start/end threshold (5 degrees)
            for (int i = 0; i < obstacles.Regions.Count; i++)
            {
                Region region = obstacles.Regions.ElementAt(i);

                Coordinate start = region.StartCoordinate;
                Coordinate end = region.EndCoordinate;

                if ((start.Theta <= ball.Angle + BALL_REGION_THRESHOLD && start.Theta >= ball.Angle - BALL_REGION_THRESHOLD)
                        && (end.Theta <= ball.Angle + BALL_REGION_THRESHOLD && end.Theta >= ball.Angle - BALL_REGION_THRESHOLD))
                {
                    ballIsRegion = true;
                    ballRegionIndex = i;
                    break;
                }
            }

            // If the ball is a Region, remove the Region representing the ball and return if there is an open path to it
            if (ballIsRegion)
            {
                obstacles.Regions.RemoveAt(ballRegionIndex);
                return IsOpenPathToBall(obstacles, ball);
            }

            return false;
        }

        // Could go off angle, but left as X coordinate for now
        private DriveCommand DriveTowardBall(TennisBall ball)
        {
            // Not within required distance
            float ballX = ball.CenterPoint.X;
            if (ballX < leftThreshold)  // TODO look into this for dynamic video sizes. ie. be able to account for 1080, 720, etc.
            {
                // Ball to left
                return DriveCommand.LeftTurn(Speed.VISION);
            }
            else if (ballX > rightThreshold)
            {
                // Ball to right
                return DriveCommand.RightTurn(Speed.VISION);
            }
            else
            {
                // Ball straight ahead
                return DriveCommand.Straight(Speed.VISION);
            }
        }

        private bool IsWithinRequiredDistance(TennisBall ball)
        {
            if (ball == null)
            {
                return false;
            }

            Console.Write("Distance to ball: " + ball.DistanceToCenter + " ");

            return ball.DistanceToCenter < DriveContext.REQUIRED_DISTANCE_FROM_BALL;
        }
    }
}