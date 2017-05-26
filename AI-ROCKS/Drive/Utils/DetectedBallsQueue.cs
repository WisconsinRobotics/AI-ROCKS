using System;
using System.Collections.Generic;
using System.Linq;

using AI_ROCKS.Drive.Models;

namespace AI_ROCKS.Drive.Utils
{
    /// <summary>
    /// This is a fixed-size queue used for verifying the DetectedBalls are valid tennis balls.
    /// </summary>
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

        /// <summary>
        /// For all DetectedBalls in queue, verify whether the cumulative DetectedBalls in the this queue accurately detect a tennis ball,
        /// according to specified parameters. This uses average timestamp (within specified time), average camera and GPS distances 
        /// (distance to ball), and percentage of correct camera and GPS distances to verify that a TennisBall is accurately detected. 
        /// </summary>
        /// <param name="distancePercentageThreshold">Percentage of DetectedBalls that must be correct (for distance).</param>
        /// <param name="timestampThreshold">DetectedBalls must average to be within this timestamp threshold.</param>
        /// <param name="cameraDistanceThreshold">Distance threshold for distance calculated by camera.</param>
        /// <param name="gpsDistanceThreshold">Distance threshold for distance calculated by GPS.</param>
        /// <returns>bool - If the current queue's DetectedBalls represent a valid, detected ball.</returns>
        public bool VerifyBallDetection(Double distancePercentageThreshold, Double timestampThreshold, Double cameraDistanceThreshold, Double gpsDistanceThreshold)
        {
            Queue<DetectedBall> detectedBallsCopy = new Queue<DetectedBall>(this.detectedBalls);
            
            // Require a full queue (capacity-number of detected balls) to even consider being a success
            if (detectedBallsCopy.Count < this.capacity)
            {
                return false;
            }

            // TODO other parameters to be checking on:
            // Radius? Get average and standard deviation to be sure
            // Location? Make sure we're not hopping around the frame, erroneously detecting something

            // Check for valid ball detection by averaging
            long averageTimestamp = 0;
            Double averageCameraDistance = 0.0;
            Double averageGPSDistance = 0.0;

            int countCameraDistanceOutsideThreshold = 0;
            int countGPSDistanceOutsideThreshold = 0;

            // Find averages
            foreach (DetectedBall detectedBall in detectedBallsCopy)
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
            if (averageTimestamp <= timestampThreshold)
            {
                // TODO log to base station

                Console.Write(" Not verified - timestamp | ");
                return false;
            }

            // Camera distance checking
            Double cameraDistancePercentageInThreshold = 1 - ((double)countCameraDistanceOutsideThreshold / this.capacity);
            if (cameraDistancePercentageInThreshold < distancePercentageThreshold)
            {
                // TODO log to base station

                Console.Write(" Not verified - camera distance percentage | ");
                return false;
            }

            if (averageCameraDistance > cameraDistanceThreshold)
            {
                // TODO log to base station

                Console.Write(" Not verified - average camera distance | ");
                return false;
            }

            // GPS distance checking
            Double gpsDistancePercentageInThreshold = 1 - ((double)countGPSDistanceOutsideThreshold / this.capacity);
            if (gpsDistancePercentageInThreshold < distancePercentageThreshold)
            {
                // TODO log to base station

                Console.Write(" Not verified - GPS distance percentage | ");
                return false;
            }

            if (averageCameraDistance > gpsDistanceThreshold)
            {
                // TODO log to base station

                Console.Write(" Not verified - GPS distance | ");
                return false;
            }

            Console.Write(" Verified: averageTimestamp: {0}, averageCameraDistance: {1}, averageGPSDistance: {2} | ", averageTimestamp, averageCameraDistance, averageGPSDistance);

            return true;
        }

        public Double Count { get { return this.detectedBalls.Count; } }
    }
}
