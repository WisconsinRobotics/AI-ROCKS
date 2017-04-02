using System;
using System.Collections.Generic;
//using AForge.Video;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

using AI_ROCKS.Drive.Models;
using ObstacleLibrarySharp;
using AI_ROCKS.PacketHandlers;
using System.Device.Location;

namespace AI_ROCKS.Drive.DriveStates
{
    class VisionDriveState : IDriveState
    {
        //MJPEGStream stream;
        //const string CAMERA_URL = "http://192.168.1.8/cgi-bin/mjpg/video.cgi";
        const string CAMERA_USERNAME = "admin";
        const string CAMERA_PASSWORD = "i#3Er0b0";
        const string CAMERA_IP_MAST = "192.168.1.8";
        const string CAMERA_URL = "rtsp://" + CAMERA_USERNAME + ":" + CAMERA_PASSWORD + "@" + CAMERA_IP_MAST + ":554/cam/realmonitor?channel=1&subtype=0";

        // Constants for hough circles
        const int param1 = 12;
        const int param2 = 24;
        const double dp = 1.5;
        const double minDist = 100;
        const int minRadius = 1;

        // Limits of HSV masking
        Hsv lower = new Hsv(22, 120, 114);
        Hsv upper = new Hsv(37, 255, 255);
        Hsv lowerTight = new Hsv(22, 120, 114);
        Hsv upperTight = new Hsv(37, 255, 255);
        Hsv lowerLoose = new Hsv(22, 52, 72);
        Hsv upperLoose = new Hsv(32, 240, 254);

        const int leftThreshold = 200;
        const int rightThreshold = 300;

        // TODO put these somewhere else? A Vision handler or something?
        public const Double FOCAL_LENGTH = -1;      // 3.6mm (6mm optional)     //TODO validate, convert into what we need (units)?
        public const Double KNOWN_WIDTH = 2.6;      // inches                   //TODO validate
        public const int PIXELS_WIDTH = 1920;       // May change, make dynamic?
        public const int PIXELS_HEIGHT = 1080;      // May change, make dynamic?

        private bool switchToGPS = false;

        private TennisBall ball;
        private VideoCapture webcam;
        private GPS gate;

        public VisionDriveState()//GPS gate)    // TODO get from DriveContext upon instantiation
        {
            this.webcam = new VideoCapture(CAMERA_URL);
            this.webcam.ImageGrabbed += WebcamGrab;
            this.webcam.Start();

            /*
            stream = new MJPEGStream(CAMERA_URL);
            stream.Login = CAMERA_USERNAME;
            stream.Password = CAMERA_PASSWORD;
            stream.ForceBasicAuthentication = true;
            stream.NewFrame += NetworkCamGrab;
            stream.Start();
            */

            // Ball not detected at start, so initialize to null
            this.ball = null;

            // TODO how to access gate
            this.gate = null; // gate;
        }

        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            // Ball detected
            if (ball != null)
            {
                // Within required distance
                if (IsWithinRequiredDistance(ball))
                {
                    // TODO handle sending success - need ACK too? Look into
                    return DriveCommand.Straight(DriveCommand.SPEED_HALT);
                }

                // Not within required distance
                float ballX = this.ball.CenterPoint.X;
                if (ballX < leftThreshold)
                {
                    // Ball is to the left
                    return DriveCommand.LeftTurn(DriveCommand.SPEED_VISION);
                }
                else if (ballX > rightThreshold)
                {
                    // Ball is to the right
                    return DriveCommand.RightTurn(DriveCommand.SPEED_VISION);
                }
                else
                {
                    // Ball is straight ahead
                    return DriveCommand.Straight(DriveCommand.SPEED_VISION);
                }
            }

            // Ball not detected
            GPS ascent = AscentPacketHandler.GPSData;
            double distanceToGate = ascent.GetDistanceTo(this.gate);

            if (distanceToGate > 4.0)
            {
                // Kick back to GPS
                switchToGPS = true;     // TODO incorporate this
                return DriveCommand.Straight(DriveCommand.SPEED_HALT);
            }

            // TODO return azimuth-based angle. Is this good or return something else? -> Depends on how our IMU compass data works
            double headingAngle = ascent.GetHeadingTo(this.gate);
            if (distanceToGate > 3.0)
            {
                // Turn and face heading
                // Drive straight toward heading
            }
            else if (distanceToGate > 2.0)
            {
                // Turn toward heading
                // Scan, use heading as reference
            }
            else
            {
                // Scan
                // ... more to do for this case
            }
            
            return null;    //TODO
        }

        /// <summary>
        /// Get the next StateType. This is used to check if the state must be switched by Autonomous Service.
        /// </summary>
        /// <returns>StateType - the next StateType</returns>
        public StateType GetNextStateType()
        {
            // Logic for finding when state needs to be switched from VisionDriveState to GPSDriveState
            return 0;
        }

        public Line FindBestGap(Plot obstacles)
        {
            // Given a Plot representing the obstacles, find Line representing the best gap.

            // Refer to GPSDriveState's psuedo code. Do we want to account for detecting the gate as a Region 
            // and actually heading toward it, rather than avoiding it?

            // TODO Vision group's algorithm here
            /*
            Overview:
            
            See ball:
                1) Open path to ball?
                2) Region = ball?
                3) Kick back to GPS (?) (for now)

            Don't see ball:
                4) Turn camera mast - drive accordingly
                5) Compare IMU/GPS - orient accordingly and drive straight

            ------
            Psuedocode:
            // NOTE: (1) and (2) above can be combined. This psuedocode gives them separate for simplicity's sake but for efficiency they should most likely be combined

            TennisBall ball = get current tennis ball (from class variable, VisionHelper, etc)
            List<Region> regions = obstacles.Regions;
            
            if (regions.Count == 0)
                // Drive straight
                Return line representing gap straight in front of us (refer to ObstacleAvoidanceDriveState)

            // Ball is detected
            if (ball != null)

                // (1) If path straight to ball is open, drive toward it

                // Get angles corresponding to Coordiante at minimum "left" and "right" clearance on sides of tennis ball (i.e. ASCENT_WIDTH / 2 on either side, perpendicular to the path we'd take - easiest shown in diagram)
                // Can probably use function for checking, or at least consolidate this with the below for loop to be more optimal
                
                leftClearanceAngle = get left clearance angle
                rightClearanceAngle = get right clearance angle
                bool isImpedingRegionDetected = false;      // Probably name better but you get the point
                for each region in regions
                    regionStartAngle = region.StartCoordinate.Angle     // left-most point - be careful with less than/greater than comparision since we use unit circle which increases right to left
                    regionEndAngle = region.EndCoordinate.Angle
                    if regionStartAngle is within leftClearanceAngle and rightClearanceAngle
                        // region exists in straight path to ball - check if it is the ball as a region (see (2) below)
                        isImpedingRegionDetected = true;
                        break;
                    if regionEndAngle is within leftClearanceAngle and rightClearanceAngle
                        // region exists in straight path to ball - check if it is the ball as a region (see (2) below)
                        isImpedingRegionDetected = true;
                        break;
                    if regionStartAngle > leftClearanceAngle and regionEndAngle < rightClearanceAngle
                        // region exists in straight path to ball - check if it is the ball as a region (see (2) below)
                        isImpedingRegionDetected = true;
                        break;
                if !isImpedingRegionDetected (i.e. no region exists that is blocking the path straight to the ball)
                    // Drive straight toward the ball (use angle of ball to simply turn right/left)
                    return line representing the open area around the ball that we can fit through  //Make this better? Make this the open gap between actual regions where the ball is located? Make FindBestGap return a Coordinate? Look into
                else
                    See (2) below
                

                --------
                // (2) If path straight to ball is not open, see if a region and ball are at the same location. If so, drive toward that region

                // Try to find region that is the ball. If found, return it as best gap
                Double angle = ball.Angle;
                for each region in regions
                    Line regionLineApproximation = line representing region - from start Coordinate to end Coordinate (i.e. new Line(region.StartCoordinate, region.EndCoordinate))
                    Coordinate approxRegionMidpoint = midpoint of regionLineApproximation
                    if angle is approximately equal to approxRegionMidpoint.Angle       // use function for testing this threshold
                        if size is relatively accurate      // Maybe? This may be difficult as the base/holder of tennis ball is unknown to us (pvc pipe? Solid wooden base or something? etc)
                            return line;

                
                --------
                // (3) No region representing ball was found. Kick back to GPS

                return null     // How to handle this? Look more into this

            else
                // (4) Turn camera mast

                // TODO
                    // Maybe have modes? 
                    // Seek mode (don't see ball) vs. approach mode (see ball, navigate toward)? Since this will most likely be complex with a lot of logic, may make sense

                
                --------
                // (5) Compare IMU/GPS
                Get current GPS
                Get goal GPS
                Compute difference between the two
                    Use this to find direction, heading (i.e. cardinal direction) we should be driving
                Get heading we should be heading = calculate from above
                Get heading we are facing from IMU
                If different, turn toward heading we should be facing (zero-point turn - we want to go slow to increase time for GPS to be accurate to our location and if we're "close" to goal don't want to go fast)
                If same heading (or once we have the same heading if using above turning method), slowly drive straight (hopefully we find ball)
                (?) Repeat until we are within a small distance from goal (i.e. GPS is not accurate)?
                    if not found when within some threshold of closeness for GPS, seek?
                    if found, detect ball and automatically go to the "found ball" logic above 
            */


            List<Region> regions = obstacles.Regions;
            
            double bestGapDistance = 0;
            Line bestGap = null;

            // Sanity check - if zero Regions exist, return Line representing gap straight in front of Ascent
            if (regions.Count == 0)
            {
                // Make Line that is twice the width of Ascent and 1/2 the maximum distance away to signify 
                // the best gap is straight ahead of us
                Coordinate leftCoord = new Coordinate(-DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);
                Coordinate rightCoord = new Coordinate(DriveContext.ASCENT_WIDTH, DriveContext.LRF_MAX_RELIABLE_DISTANCE / 2, CoordSystem.Cartesian);

                bestGap = new Line(leftCoord, rightCoord);
                return bestGap;
            }

            // If open path to ball exists, drive toward it
            if (IsOpenPathToBall())
            {
                // Drive toward it
            }


            return null;
        }

        private void WebcamGrab(Object sender, EventArgs e)
        {
            Mat frame = new Mat();
            webcam.Retrieve(frame);
            ProcessFrame(frame.ToImage<Bgr, byte>());
        }

        /*
        private void NetworkCamGrab(Object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = eventArgs.Frame;
            Image<Bgr, byte> frame = new Image<Bgr, byte>(bitmap);
            ProcessFrame(frame);
        }
        */

        private void ProcessFrame(Image<Bgr, byte> image)
        {
            Image<Bgr, byte> blur = image.SmoothGaussian(15);
            Image<Hsv, byte> hsv = blur.Convert<Hsv, byte>();

            // Masks
            Image<Gray, byte> mask = hsv.InRange(lowerTight, upperTight).Erode(2).Dilate(2);
            Image<Bgr, byte> coloredMask = mask.Convert<Bgr, byte>() &image;
            Image<Gray, byte> grayMask = coloredMask.Convert<Gray, byte>();

            CircleF candidateBall = new CircleF();
            bool isBallDetected = FindTennisBall(grayMask, ref candidateBall);

            this.ball = isBallDetected ? new TennisBall(candidateBall) : null;
        }

        private bool FindTennisBall(Image<Gray, byte> mask, ref CircleF outCircle)
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
                    outCircle = CvInvoke.MinEnclosingCircle(max);
                    found = true;
                }
            }

            return found;
        }

        private bool IsOpenPathToBall()
        {
            // TODO
            return false;
        }

        private bool IsWithinRequiredDistance(TennisBall ball)
        {
            // TODO
            return false;
        }
    }
}
