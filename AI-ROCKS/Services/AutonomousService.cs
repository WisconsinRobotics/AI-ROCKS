﻿﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Timers;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Models;
using AI_ROCKS.Drive.Utils;
using ObstacleLibrarySharp;
using AI_ROCKS.PacketHandlers;

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


        public AutonomousService(StateType initialStateType, GPS gate, int lrfPort, bool lrfTest = false)
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
                        IPEndPoint sendAddr = new IPEndPoint(IPAddress.Loopback, 11001);

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

                        handshake = true;
                    }
                }
                return;
            }

            this.plot = Plot.Deserialize(list);
        }

        /// <summary>
        /// Main execution function for AutonomousService.
        /// </summary>
        public void Execute(Object source, ElapsedEventArgs e)
        {
            // If detected an obstacle within the last 5 seconds, continue straight to clear obstacle
            if (IsLastObstacleWithinInterval(OBSTACLE_WATCHDOG_MILLIS))
            {
                Console.WriteLine("Watchdog");
                StatusHandler.SendDebugAIPacket(Status.AIS_IN_WATCHDOG, "Watchdog.");

                // If more than 0.5 seconds have passed since last event, it's safe to start issuing drive 
                // commands - otherwise race condition may occur when continually detecting an obstacle
                if (!IsLastObstacleWithinInterval(CLEAR_OBSTACLE_DELAY_MILLIS))
                {
                    this.driveContext.Drive(DriveCommand.Straight(Speed.CLEAR_OBSTACLE));
                    StatusHandler.SendDebugAIPacket(Status.AIS_OUT_WATCHDOG, "Out of watchdog.");
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
                this.driveContext.ChangeState(nextState);
            }
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
                StatusHandler.SendDebugAIPacket(Status.AIS_OBS_DETECT, "Obstacle detected.");
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
    }
}
