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
            String lrfPort = String.Empty;
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
                    default:
                    {
                        // Invalid command line argument
                        ExitFromInvalidArgrument("Unrecognized command line argument: " + curr);
                        break;
                    }
                }
            }

            if (lrfPort.Equals(String.Empty))
            {
                ExitFromInvalidArgrument("Command line argument for LRF port is required!");
            }

            // Initialize AscentPacketHandler
            AscentPacketHandler.Initialize(destinationIP);

            // Create AutonomousService
            AutonomousService autonomousService = new AutonomousService(lrfPort, initialStateType);

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
            Environment.Exit(160);
        }
    }
}
