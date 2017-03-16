using System;
using System.Net;
using Timer = System.Timers.Timer;

using AI_ROCKS.Drive;
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
            // Parse args:
            // -l COMX          - COM or UDP port LRF is on
            // -d X             - DriveState to start in (according to StateType enum). Default GPSDriveState
            // -g <address>     - using Gazebo (i.e. testing)

            String lrfPort = "";
            StateType initialStateType = StateType.GPSState;
            IPAddress destinationIP = IPAddress.Loopback;

            for (int i = 0; i < args.Length; i++)
            {
                String curr = args[i];
                switch (curr)
                {
                    case "-l":
                    {
                        // LRF
                        lrfPort = args[++i];
                        break;
                    }
                    case "-d":
                    {
                        // StateType
                        int res = 0;
                        Int32.TryParse(args[++i], out res);
                        initialStateType = (StateType)res;
                        break;
                    }
                    case "-g":
                    {
                        try
                        {
                            destinationIP = IPAddress.Parse(args[++i]);
                        }
                        catch (Exception)
                        {
                            // Invalid IP for Gazebo testing
                            ExitFromInvalidArgrument("Invalid IP address for Gazebo testing: " + args[i]);
                        }
                        
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

            // Create AutonomousService
            AutonomousService autonomousService = new AutonomousService(lrfPort, initialStateType);

            // Initialize AscentPacketHandler
            AscentPacketHandler.Initialize(destinationIP);

            // Set up connection with ROCKS (Service Master?, etc)
            // TODO

            // While connection is present, run autonomous service
            
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

            while (true) { }

            // TODO when to end? When gate is detected?
        }

        private static void ExitFromInvalidArgrument(String errorMessage)
        {
            // Invalid command line argument - write to both error and debug outputs
            Console.Error.Write(errorMessage + "\n");
            System.Diagnostics.Debug.Write(errorMessage + "\n");

            // Exit with Windows ERROR_BAD_ARGUMENTS system error code
            System.Environment.Exit(160);
        }
    }
}
