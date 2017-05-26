using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Timer = System.Timers.Timer;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

using AI_ROCKS.Drive.Models;
using AI_ROCKS.Drive.Utils;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.DriveStates
{
    class Camera
    {
        // Camera constants
        public const Double FOCAL_LENGTH = 112.0;   // 3.6mm (6mm optional)     //TODO validate, convert into what we need (units)?
        public const Double KNOWN_WIDTH = 2.6;      // inches                   //TODO validate
        public const int PIXELS_WIDTH = 1920;       // May change, make dynamic?
        public const int PIXELS_HEIGHT = 1080;      // May change, make dynamic?
        const int FRAME_RATE = 15;

        // Circle detection
        const int MIN_RADIUS = 10;

        // Gaussian blur
        const int GAUSSIAN_KERNELSIZE = 15;

        // Limits of HSV masking
        Hsv minHSV = new Hsv(30, 30, 70);
        //(20, 30, 90); - something worked kind of
        //(20, 20, 68); - top 1/4 lit, bottom underlit (lit overhead from side)
        //(20, 30, 70); - top 1/4 lit, bottom underlit (lit overhead behind)    -- note, moving saturation down detected concrete/cement as ball
        //(30, 30, 90); - ball is almost entirely front lit, completely lit up
        //(30, 30, 90); - ball is 1/2 covered on a slant. lit from overhead side        works well with 70 too
        //(25, 30, 70); - ball is completely backlit, this distinguishes yellow (parking lines) from green

        Hsv maxHSV = new Hsv(50, 170, 255);

        // Detection objects
        private TennisBall tennisBall;
        private long ballTimestamp;
        private readonly Object ballLock;
        private ConcurrentStack<Mat> frameStack;
        private Timer timer;

        // Navigation objects
        private VideoCapture camera;

        
        // For use with IP cameras that use a URL
        public Camera(String cameraUrl)
        {
            StartCamera(cameraUrl);

            this.tennisBall = null;
            this.ballLock = new Object();
            this.frameStack = new ConcurrentStack<Mat>();
        }

        // For use with webcams that use a device ID
        public Camera(int cameraDeviceId)
        {
            StartCamera(cameraDeviceId);

            this.tennisBall = null;
            this.ballLock = new Object();
            this.frameStack = new ConcurrentStack<Mat>();
        }

        
        private void StartCamera(int cameraDeviceId)
        {
            this.camera = new VideoCapture(cameraDeviceId);
            StartCamera();
        }

        private void StartCamera(String cameraUrl)
        {
            this.camera = new VideoCapture(cameraUrl);
            StartCamera();
        }

        private void StartCamera()
        {
            // Use stack to collect frames and process at our own frame rate
            this.camera.ImageGrabbed += FrameGrabbed;

            this.timer = new Timer();
            this.timer.Interval = 1000 / FRAME_RATE;
            this.timer.Elapsed += Tick;

            this.camera.Start();
            this.timer.Start();
        }

        private void FrameGrabbed(Object sender, EventArgs e)
        {
            Mat frame = new Mat();
            camera.Retrieve(frame);
            PushFrame(frame);
        }

        private void PushFrame(Mat frame)
        {
            this.frameStack.Push(frame);
        }

        private void Tick(Object sender, EventArgs e)
        {
            Mat frame = new Mat();
            Mat[] frames = new Mat[10];
            if (this.frameStack.TryPopRange(frames) > 0)
            {
                ProcessFrame(frames[0]);
            }

            this.frameStack.Clear();
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
        
        public TennisBall GetBallCopy()
        {
            TennisBall ball = null;
            lock (ballLock)
            {
                if (this.tennisBall != null)
                {
                    ball = new TennisBall(this.tennisBall);
                }
            }

            return ball;
        }

        public long BallTimestamp
        {
            get { return this.ballTimestamp;  }
        }

    }
}