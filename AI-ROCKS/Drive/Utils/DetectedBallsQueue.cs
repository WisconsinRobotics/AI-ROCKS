using System;
using System.Collections.Generic;
using System.Linq;

using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.Drive.Utils
{
    // Notes spewn by Matt to make this make sense in my head
    // This is a fixed-size queue
    // If not 50 (or capacity amount of) consecutive detections, drop and restart
    // Require:
    //  - average timestamp within threshold (3 seconds?)
    //  - average camera distance within required distance (80%?)
    //  - average gps distance within some threshold of required distance (required distance +/- 3m) (80%?)


    class DetectedBallsQueue
    {
        private Queue<DetectedBall> detectedBalls;
        private Object queueLock;
        private int capacity;

        public DetectedBallsQueue(int capacity)
        {
            this.detectedBalls = new Queue<DetectedBall>(capacity);
            queueLock = new Object();
            this.capacity = capacity;
        }


        public void Enqueue(DetectedBall detectedBall)
        {
            lock (queueLock)
            {
                while (this.detectedBalls.Count >= this.capacity)
                {
                    this.detectedBalls.Dequeue();
                }

                this.detectedBalls.Enqueue(detectedBall);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>DetectedBall - return null if queue is empty, otherwise element at front of queue (oldest).</returns>
        public DetectedBall Dequeue()
        {
            lock (queueLock)
            {
                DetectedBall detectedBall = null;

                if (this.detectedBalls.Count != 0)
                {
                    detectedBall = this.detectedBalls.Dequeue();
                }

                return detectedBall;
            }
        }

        public void Clear()
        {
            lock (queueLock)
            {
                if (this.detectedBalls.Count != 0)
                {
                    this.detectedBalls.Clear();
                }
            }
        }

        // If not 50 (or capacity amount of) consecutive detections, drop and restart
        // Require:
        //  - average timestamp within threshold (3 seconds?)
        //  - average camera distance within required distance (80%?)
        //  - average gps distance within some threshold of required distance (required distance +/- 3m) (80%?)
        public bool VerifyBallDetection(Double distancePercentageThreshold, Double timestampThreshold, Double cameraDistanceThreshold, Double gpsDistanceThreshold)
        {
            lock (queueLock)
            {
                // Require a full queue (capacity-number of detected balls) to even consider being a success
                if (this.detectedBalls.Count < this.capacity)
                {
                    return false;
                }

                // Check for valid ball detection by averaging
                Double averageTimestamp = 0.0;
                Double averageCameraDistance = 0.0;
                Double averageGPSDistance = 0.0;

                int countCameraDistanceOutsideThreshold = 0;
                int countGPSDistanceOutsideThreshold = 0;

                // Find averages
                foreach (DetectedBall detectedBall in this.detectedBalls)
                {
                    averageTimestamp += detectedBall.TimestampMillis;

                    averageCameraDistance += detectedBall.CameraDistance;
                    if (detectedBall.CameraDistance > cameraDistanceThreshold)
                    {
                        countCameraDistanceOutsideThreshold++;
                    }

                    averageGPSDistance += detectedBall.GPSDistance;
                    if (detectedBall.GPSDistance > gpsDistanceThreshold)
                    {
                        countGPSDistanceOutsideThreshold++;
                    }

                }

                averageTimestamp = averageTimestamp / this.capacity;
                averageCameraDistance = averageCameraDistance / this.capacity;
                averageGPSDistance = averageGPSDistance / this.capacity;

                // Timestamp checking
                if (averageTimestamp > timestampThreshold)
                {
                    return false;
                }

                // Camera distance checking
                if (averageCameraDistance > cameraDistanceThreshold)
                {
                    return false;
                }

                // GPS distance checking
                if (averageCameraDistance > gpsDistanceThreshold)
                {
                    return false;
                }

                return true;

            }
        }

        public Double Count { get { return this.detectedBalls.Count; } }
    }
}
