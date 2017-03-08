using System;

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

        //TODO event typing into some new ObstacleAvoidanceDriveState that triggers when the robot needs to avoid an obstacle
        public Line FindBestGap(Plot obstacles)
        {
            // Given a Plot representing the obstacles, find Line representing the best gap.

            // Refer to GPSDriveState's psuedo code. Do we want to account for detecting the gate as a Region 
            // and actually heading toward it, rather than avoiding it?

            // TODO Vision group's algorithm here


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
