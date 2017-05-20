using System;
using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

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
        const string CAMERA_IP_MAST = "192.168.1.7";    // TODO should be .8 but for now it's not
        const string CAMERA_URL = "rtsp://" + CAMERA_USERNAME + ":" + CAMERA_PASSWORD + "@" + CAMERA_IP_MAST + ":554/cam/realmonitor?channel=1&subtype=0";
        const int CAMERA_DEVICE_ID = 1;

        // TODO put these somewhere else? A Vision handler or something?
        // Camera constants
        public const Double FOCAL_LENGTH = 112.0;   // 3.6mm (6mm optional)     //TODO validate, convert into what we need (units)?
        public const Double KNOWN_WIDTH = 2.6;      // inches                   //TODO validate
        public const int PIXELS_WIDTH = 1920;       // May change, make dynamic?
        public const int PIXELS_HEIGHT = 1080;      // May change, make dynamic?

        // Circle detection
        const int MIN_RADIUS = 10;

        // Gaussian blur
        const int GAUSSIAN_KERNELSIZE = 15;

        // Limits of HSV masking
        Hsv minHSV = new Hsv(30, 30, 110);
        Hsv maxHSV = new Hsv(50, 170, 255);

        // Turning thresholds
        const int leftThreshold = 3 * PIXELS_WIDTH / 8;     // 3/8 from left
        const int rightThreshold = 5 * PIXELS_WIDTH / 8;    // 5/8 from left
        const double TURN_THRESHOLD_ANGLE_LEFT = 2 * Math.PI / 3;
        const double TURN_THRESHOLD_ANGLE_RIGHT = Math.PI / 3;

        // Navigation utils
        private const int DROP_BALL_DELAY = 5000;   // maybe name this more appropriately lol
        private const double BALL_REGION_THRESHOLD = 0.0872665;     // 5 degrees in radians
        private bool switchToGPS = false;

        // Detection objects
        private TennisBall tennisBall;
        private long ballTimestamp;
        private readonly Object ballLock;

        // Navigation objects
        private VideoCapture camera;
        private GPS gate;
        private Scan scan;

        // TODO for testing - remove
        int count = 0;


        public VisionDriveState(GPS gate)
        {
            StartCamera();

            this.tennisBall = null;
            this.ballLock = new Object();

            this.gate = gate;
            this.scan = null;
        }

        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            TennisBall ball = GetBallCopy();

            // Recently detected ball but now don't now. Stop driving to redetect since we may have dropped it due to bouncing.
            if (ball == null && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < ballTimestamp + DROP_BALL_DELAY)
            {
                Console.WriteLine("Dropped ball - halting ");
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
            return switchToGPS ? StateType.GPSState : StateType.VisionState;
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
            TennisBall ball = GetBallCopy();

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

        private void StartCamera()
        {
            this.camera = new VideoCapture(CAMERA_URL);
            this.camera.ImageGrabbed += FrameGrabbed;
            this.camera.Start();
        }

        private void FrameGrabbed(Object sender, EventArgs e)
        {
            // TODO for testing -- remove
            if (++count < 10)
            {
                return;
            }
            count = 0;


            Mat frame = new Mat();
            camera.Retrieve(frame);
            ProcessFrame(frame);
        }

        private void ProcessFrame(Mat frame)
        {
            // Convert to Image type
            Image<Bgr, byte> frameImage = frame.ToImage<Bgr, byte>();

            // Apply Gaussian Blur and Perform HSV masking
            Image<Bgr, byte> blur = frameImage.SmoothGaussian(GAUSSIAN_KERNELSIZE);
            Image<Gray, byte> mask = blur.Convert<Hsv, byte>().InRange(minHSV, maxHSV);

            // Erode and Dilate
            mask = mask.Erode(2).Dilate(2);

            // Produce a Colored mask and Grayscaled mask
            Image<Bgr, byte> coloredMask = mask.Convert<Bgr, byte>() & frameImage;
            Image<Gray, byte> grayMask = mask & frameImage.Convert<Gray, byte>();
            //Image<Gray, byte> grayMask = coloredMask.Convert<Gray, byte>();

            // Find the tennis ball
            CircleF candidateBall = new CircleF();
            bool isBallDetected = FindTennisBall(grayMask, ref candidateBall);

            lock (ballLock)
            {
                this.tennisBall = isBallDetected ? new TennisBall(candidateBall) : null;
            }
            if (isBallDetected)
            {
                ballTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        private bool FindTennisBall(Image<Gray, byte> mask, ref CircleF outCircle)
        {
            return FindEnclosingCircle(mask, ref outCircle);
        }

        private bool FindEnclosingCircle(Image<Gray, byte> mask, ref CircleF outCircle)
        {
            bool found = false;

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(mask.Copy(), contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

            if (contours.Size > 0)
            {
                VectorOfPoint max = contours[0];
                
                for (int i = 0; i < contours.Size; i++)
                {
                    VectorOfPoint contour = contours[i];
                    if (CvInvoke.ContourArea(contour, false) > CvInvoke.ContourArea(max, false))
                    {
                        max = contour;
                    }
                }
                if (CvInvoke.ContourArea(max, false) > 300)
                {
                    CircleF tempCircle = CvInvoke.MinEnclosingCircle(max);
                    if (tempCircle.Radius > MIN_RADIUS)
                    {
                        found = true;
                        outCircle = tempCircle;
                    }
                }
            }

            return found;
        }

        private DriveCommand DriveBallDetected(TennisBall ball)
        {
            // Sanity check
            if (ball == null)
            {
                // TODO could this be a race condition? Pass copy of ball as param?
                return null;
            }


            Console.Write("Ball detected ");


            // Detected ball so no longer scan
            this.scan = null;

            // Within required distance
            if (IsWithinRequiredDistance(ball))
            {
                Console.Write("WITHIN REQUIRED DISTANCE | ");

                // TODO handle sending success - need ACK too? Look into
                return DriveCommand.Straight(Speed.HALT);
            }

            return TurnTowardBall(ball);
        }

        private DriveCommand DriveNoBallDetected(TennisBall ball)
        {
            // Sanity check
            if (ball != null)
            {
                // TODO could this be a race condition? Pass copy of ball as param?
                return null;
            }


            Console.Write("Ball not detected ");


            // Ball not detected
            GPS ascent = AscentPacketHandler.GPSData;
            double distanceToGate = ascent.GetDistanceTo(this.gate);

            // Kick back to GPS
            if (distanceToGate > 5.0)
            {
                Console.WriteLine("Distance: " + distanceToGate + ". Switch to GPS");

                switchToGPS = true;
                return DriveCommand.Straight(Speed.HALT);
            }

            // Turn to face heading, drive toward it
            if (distanceToGate > 3.0)
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
                    // Clear scan, will rescan below
                    this.scan = null;
                }
            }

            if (distanceToGate > 2.0)
            {
                // TODO StatusHandler log
                Console.WriteLine("Distance: " + distanceToGate + ". Scanning (using heading)...");

                // Turn toward heading
                // Scan, use heading as reference
                this.scan = new Scan(this.gate, true);
            }
            else
            {
                // TODO StatusHandler log
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
        private DriveCommand TurnTowardBall(TennisBall ball)
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

        private TennisBall GetBallCopy()
        {
            TennisBall ball = null;
            lock (ballLock)
            {
                ball = new TennisBall(this.tennisBall);
            }

            return ball;
        }
    }
}