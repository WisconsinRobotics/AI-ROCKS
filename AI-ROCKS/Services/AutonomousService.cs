﻿﻿﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Timers;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Models;
using AI_ROCKS.Drive.Utils;
using AI_ROCKS.PacketHandlers;
using ObstacleLibrarySharp;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        // Obstacle avoidance
        private const long OBSTACLE_WATCHDOG_MILLIS = 1000;         // 5 second delay   // TODO verify and update
        private const long CLEAR_OBSTACLE_DELAY_MILLIS = 1000;      // 1 second delay   // TODO verify and update
        public const long OBSTACLE_DETECTION_DISTANCE = 2000;       // 2 meters         // TODO verify and update

        // RDP
        private const double REGION_SEPARATION_DISTANCE = 300.0;    // Distance between regions - helps reduce noise
        private const double RDP_THRESHOLD = 50.0;                  // How much the LRF data is reduced

        private DriveContext driveContext;
        private Plot plot;

        // Obstacle detection
        public event EventHandler<ObstacleEventArgs> ObstacleEvent;
        private UdpClient rocks_lrf_socket;
        private bool handshake = false;

        // Execute() lock - avoid concurrent Execute() calls
        Object executeLock = new Object();

        public DateTimeOffset startTimeStamp;
        int fuckItGoForItCountDown;
        bool panic = false;

        public AutonomousService(StateType initialStateType, GPS gate, int lrfPort, int fuckItGoForItCountDown, bool lrfTest = false)
        {
            this.driveContext = new DriveContext(initialStateType, gate);
            this.ObstacleEvent += driveContext.HandleObstacleEvent;

            if (!lrfTest)
            {
                this.rocks_lrf_socket = new UdpClient(lrfPort);

                // Initialize async receive
                this.rocks_lrf_socket.BeginReceive(HandleSocketReceive, null);
            }
            else
            {
                this.plot = new Plot();
            }

            this.startTimeStamp = DateTime.Now;
            this.fuckItGoForItCountDown = fuckItGoForItCountDown;
        }

        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute(Object source, ElapsedEventArgs e)
        {
            // Don't execute if existing execution is not complete
            if (!Monitor.TryEnter(executeLock))
            {
                return;
            }

            // TODO debugging - delete
            //Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}", e.SignalTime);

            // this is for when we're running out of time and just roll back to only gps and hope for the best
            if (!this.panic && (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - this.startTimeStamp.ToUnixTimeMilliseconds() > this.fuckItGoForItCountDown))
            {
                StatusHandler.SendDebugAIPacket(Status.AIS_LOG, "Starting hail mary");
                this.panic = true;
                this.driveContext.HandleFinalCountDown();
            }

            // If detected an obstacle within the last 5 seconds, continue straight to clear obstacle
            if (IsLastObstacleWithinInterval(OBSTACLE_WATCHDOG_MILLIS))
            {
                StatusHandler.SendSimpleAIPacket(Status.AIS_OUT_WATCHDOG);
                Console.WriteLine("Watchdog");

                // If more than 0.5 seconds have passed since last event, it's safe to start issuing drive 
                // commands - otherwise race condition may occur when continually detecting an obstacle
                if (!IsLastObstacleWithinInterval(CLEAR_OBSTACLE_DELAY_MILLIS))
                {
                    this.driveContext.Drive(DriveCommand.Straight(Speed.CLEAR_OBSTACLE));
                }
                
                return;
            }

            // Get DriveCommand from current drive state, issue DriveCommand
            DriveCommand driveCommand = this.driveContext.FindNextDriveCommand();
            this.driveContext.Drive(driveCommand);

            // If state change is required, change state
            StateType nextState = this.driveContext.GetNextStateType();
            if (this.driveContext.IsStateChangeRequired(nextState))
            {
                Console.WriteLine("Switching from state: " + this.driveContext.StateType.ToString() + " to: " + nextState.ToString());

                this.driveContext.ChangeState(nextState);
            }

            Monitor.Exit(executeLock);
        }

        /// <summary>
        /// Detect if an ObstacleEvent must be raised according to the LRF data being read. Trigger
        /// ObstacleEvent if an obstacle exists within the maximum allowable distance threshold.
        /// </summary>
        public void DetectObstacleEvent(Object source, ElapsedEventArgs e)
        {
            // Sanity check
            if (this.plot == null)
            {
                return;
            }

            // See if any obstacle within maximum allowed distance
            bool obstacleDetected = false;
            foreach (Region region in this.plot.Regions)
            {
                foreach (Coordinate coordinate in region.ReducedCoordinates)
                {
                    if (coordinate.R < OBSTACLE_DETECTION_DISTANCE)
                    {
                        if (Math.Abs(coordinate.X) < DriveContext.ASCENT_WIDTH/2)
                        {
                            obstacleDetected = true;
                            break;
                        }
                    }
                }

                if (obstacleDetected)
                {
                    break;
                }
            }

            // If obstacle detected, trigger event
            if (obstacleDetected)
            {
                StatusHandler.SendDebugAIPacket(Status.AIS_OBS_DETECT, "Obstacle detected");
                OnObstacleEvent(new ObstacleEventArgs(this.plot));
            }
        }

        /// <summary>
        /// Trigger an ObstacleEvent.
        /// </summary>
        /// <param name="e">ObstacleEventArgs</param>
        protected virtual void OnObstacleEvent(ObstacleEventArgs e)
        {
            EventHandler<ObstacleEventArgs> handler = ObstacleEvent;

            if (handler != null)
            {
                // BeginInvoke here? use same thread or spawn new thread?
                handler(this, e);
            }
        }

        /// <summary>
        /// If the last obstacle detected happened within a specified threshold of time, in milliseconds.
        /// </summary>
        /// <param name="milliseconds">Threshold used to test if the last obstacle occured within a certain
        /// amount of time.</param>
        /// <returns>bool - true if the last obstacle was detected within threshold time, false otherwise</returns>
        private bool IsLastObstacleWithinInterval(long milliseconds)
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < this.driveContext.LastObstacleDetected + milliseconds;
        }

        public bool IsComplete()
        {
            return AscentPacketHandler.ReceivedAck && this.driveContext.IsComplete;
        }


        void HandleSocketReceive(IAsyncResult result)
        {
            IPEndPoint recvAddr = new IPEndPoint(IPAddress.Loopback, 0);
            byte[] data = rocks_lrf_socket.EndReceive(result, ref recvAddr);

            this.rocks_lrf_socket.BeginReceive(HandleSocketReceive, null);

            List<byte> list = new List<byte>();

            for (int i = 0; i < data.Length; i++)
            {
                list.Add(data[i]);
            }

            if (!this.handshake)
            {
                if (list.Count == 4)
                {
                    if (list[0] == 0xDE && list[1] == 0xAD && list[2] == 0xBE && list[3] == 0xEF)
                    {
                        IPEndPoint sendAddr = new IPEndPoint(IPAddress.Loopback, 10000);

                        byte[] lrf_min_angle = BitConverter.GetBytes(DriveContext.LRF_MIN_ANGLE);
                        byte[] lrf_max_angle = BitConverter.GetBytes(DriveContext.LRF_MAX_ANGLE);
                        byte[] region_separation_distance = BitConverter.GetBytes(REGION_SEPARATION_DISTANCE);
                        byte[] rdp_threshold = BitConverter.GetBytes(RDP_THRESHOLD);

                        List<byte> buffer = new List<byte>();
                        buffer.Add(0xDE);
                        buffer.Add(0xAD);

                        buffer.AddRange(lrf_min_angle);
                        buffer.AddRange(lrf_max_angle);
                        buffer.AddRange(region_separation_distance);
                        buffer.AddRange(rdp_threshold);

                        buffer.Add(0xBE);
                        buffer.Add(0xEF);

                        this.rocks_lrf_socket.Send(buffer.ToArray(), buffer.Count, sendAddr);

                        this.handshake = true;
                        StatusHandler.SendDebugAIPacket(Status.AIS_LOG, "Received LRF handshare");
                    }
                }
                return;
            }

            this.plot = Plot.Deserialize(list);
        }
    }
}
