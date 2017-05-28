using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Timer = System.Timers.Timer;

using AI_ROCKS.Drive;
using AI_ROCKS.Drive.Models;
using AI_ROCKS.PacketHandlers;
using AI_ROCKS.Services;

namespace AI_ROCKS
{
    class Program
    {
        private const long EXECUTE_INTERVAL_MILLIS = 100;  //TODO change
        private const long OBSTACLE_INTERVAL_MILLIS = 100; //TODO change

        static void Main(string[] args)
        {
            Console.SetWindowSize(110, 25);

            int lrfPort = 11000;
            StateType initialStateType = StateType.GPSState;
            IPAddress destinationIP = IPAddress.Loopback;
            bool lrfTest = false;
            bool gateGPSTest = false;

            GPS gate = null;
            List<float> latitude = null;
            List<float> longitude = null;

            for (int i = 0; i < args.Length; i++)
            {
                String curr = args[i];
                switch (curr)
                {
                    case "-l":
                    {
                        // LRF
                        if (!Int32.TryParse(args[++i], out lrfPort))
                        {
                            ExitFromInvalidArgrument("Invalid integer for StateType parsing: " + args[i]);
                        }
                        break;
                    }
                    case "-d":
                    {
                        // StateType
                        int res = 0;
                        
                        // If valid int
                        if (!Int32.TryParse(args[++i], out res))
                        {
                            ExitFromInvalidArgrument("Invalid integer for StateType parsing: " + args[i]);
                        }

                        // If valid StateType from int
                        try
                        {
                            initialStateType = StateTypeHelper.FromInteger(res);
                        }
                        catch (Exception e)
                        {
                            ExitFromInvalidArgrument(e.Message);
                        }
                        
                        break;
                    }
                    case "-g":
                    {
                        // Gazebo address
                        
                        // If valid IP
                        try
                        {
                            destinationIP = IPAddress.Parse(args[++i]);
                        }
                        catch (Exception)
                        {
                            ExitFromInvalidArgrument("Invalid IP address for Gazebo testing: " + args[i]);
                        }
                        
                        break;
                    }
                    case "-t":
                    {
                        // LRF test mode
                        lrfTest = true;
                        break;
                    }
                    case "-lat":
                    {
                        try
                        {
                            latitude = new List<float>();
                            latitude.Add(float.Parse(args[++i]));       // Degrees
                            latitude.Add(float.Parse(args[++i]));       // Minutes
                            latitude.Add(float.Parse(args[++i]));       // Seconds
                        }
                        catch (Exception)
                        {
                            ExitFromInvalidArgrument("Invalid latitude for GPS");
                        }

                        break;
                    }
                    case "-long":
                    {
                        try
                        {
                            longitude = new List<float>();
                            longitude.Add(float.Parse(args[++i]));      // Degrees
                            longitude.Add(float.Parse(args[++i]));      // Minutes
                            longitude.Add(float.Parse(args[++i]));      // Seconds
                        }
                        catch (Exception)
                        {
                            ExitFromInvalidArgrument("Invalid longitude for GPS");
                        }

                        break;
                    }
                    case "-nogps":
                    {
                        // GPS test mode
                        gateGPSTest = true;
                        break;
                    }
                    default:
                    {
                        // Invalid command line argument
                        ExitFromInvalidArgrument("Unrecognized command line argument: " + curr);
                        break;
                    }
                }
            }

            // Initialize AscentPacketHandler
            AscentPacketHandler.Initialize(destinationIP);

            if (gateGPSTest)
            {
                // Test mode
                gate = new GPS(0, 0, 0, 0, 0, 0);
            }
            else if (longitude != null || latitude != null)
            {
                if (longitude != null || latitude != null)
                {
                    // Specified as args
                    gate = new GPS(latitude.ElementAt(0), latitude.ElementAt(1), latitude.ElementAt(2),
                                    longitude.ElementAt(0), longitude.ElementAt(1), longitude.ElementAt(2));
                }
                else
                {
                    // Not both specified, throw error
                    ExitFromInvalidArgrument("Specifying latitude/longitude as a command line arg requires both to be specified");
                }
            }
            else
            {
                // Spin, wait for gate GPS from ROCKS (Base Station GUI)
                // TODO

                while (true)
                { 
                    //this should be handled 
                }
            }

            // Create AutonomousService
            AutonomousService autonomousService = new AutonomousService(initialStateType, gate, lrfPort, lrfTest);
            StatusHandler.SendDebugAIPacket(Status.AIS_LOG, "Autonomous Service initialized");

            // Create Timer for Execute function
            Timer executeTimer = new Timer(EXECUTE_INTERVAL_MILLIS);
            executeTimer.AutoReset = true;
            executeTimer.Elapsed += autonomousService.Execute;
            executeTimer.Enabled = true;

            // Create timer for ObstacleEvent, tied in to autonomousService.DetectObstacleEvent()
            Timer obstacleTimer = new Timer(OBSTACLE_INTERVAL_MILLIS);
            obstacleTimer.AutoReset = true;
            obstacleTimer.Elapsed += autonomousService.DetectObstacleEvent;
            obstacleTimer.Enabled = true;

            // Execute while not receive an ACK from base station and DriveContext is not complete
            while (!autonomousService.IsComplete())
            {
            }

            StatusHandler.SendDebugAIPacket(Status.AIS_LOG, "Received ACK from Base Station and drive is complete. Exiting AI-ROCKS..");
            Console.Write("WE DONE BITCHES");
        }

        private static void ExitFromInvalidArgrument(String errorMessage)
        {
            // Invalid command line argument - write to error output, debug output, and base station
            Console.Error.Write(errorMessage + "\n");
            System.Diagnostics.Debug.Write(errorMessage + "\n");
            StatusHandler.SendDebugAIPacket(Status.AIS_FATAL_ERROR, "Error: " + errorMessage);

            // Exit with Windows ERROR_BAD_ARGUMENTS system error code
            Environment.Exit(160);
        }
    }
}
