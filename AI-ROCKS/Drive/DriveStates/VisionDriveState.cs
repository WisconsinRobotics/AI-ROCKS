﻿using System;

using ObstacleLibrarySharp;
using AForge.Video;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace AI_ROCKS.Drive.DriveStates
{

    class VisionDriveState : IDriveState
    {
        MJPEGStream stream;
        const string camera_url = "http://192.168.1.8/cgi-bin/mjpg/video.cgi";
        const string camera_login = "admin";
        const string camera_password = "i#3Er0b0";

        // Constants for hough circles
        const int param1 = 12;
        const int param2 = 24;
        const double dp = 1.5;
        const double minDist = 100;
        const int minRadius = 1;

        // Limits of HSV masking
        Hsv lower = new Hsv(22, 120, 114);
        Hsv upper = new Hsv(37, 255, 255);

        const int leftThreshold = 200;
        const int rightThreshold = 300;

        float X;
        DriveCommand command;

        public VisionDriveState()
        {                
            stream = new MJPEGStream(camera_url);
            stream.Login = camera_login;
            stream.Password = camera_password;
            stream.ForceBasicAuthentication = true;
            stream.NewFrame += NetworkCamGrab;
            stream.Start();
            X = 0;
        }

        /// <summary>
        /// Find the next DriveCommand to be issued to ROCKS.
        /// </summary>
        /// <returns>DriveCommand - the next drive command for ROCKS to execute.</returns>
        public DriveCommand FindNextDriveCommand()
        {
            if (X < 0)
            {
                // Ball not found
                return command = new DriveCommand(0, 0, 0);
            }
            else if (X < leftThreshold) 
            {
                // Ball is to the left of us
                return command = new DriveCommand(-1, 1, 1);
            }
            else if (X > rightThreshold)
            {
                // Ball is to the right of us
                return command = new DriveCommand(1, -1, 1);
            }
            else 
            {
                // Ball is straight ahead
                return command = new DriveCommand(0, 0, 1);
            }
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

            return null;
        }

        private void NetworkCamGrab(Object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap b = eventArgs.Frame;
            Image<Bgr, byte> myImage = new Image<Bgr, byte>(b);
            ProcessFrame(myImage);
        }

        private void ProcessFrame(Image<Bgr, byte> m)
        {
            Image<Bgr, byte> blur = m.SmoothGaussian(15);
            Image<Hsv, byte> hsv = blur.Convert<Hsv, byte>();
            Image<Gray, byte> mask = hsv.InRange(lower, upper);
            Image<Bgr, byte> coloredMask = mask.Convert<Bgr, byte>() & m;
            Image<Gray, byte> grayMask = coloredMask.Convert<Gray, byte>();

            CircleF[] c = grayMask.Convert<Gray, byte>().HoughCircles(new Gray(param1), new Gray(param2), dp, minDist, minRadius)[0];
            if (c.Length > 0)
                X = c[0].Center.X;
        }
    }
}
