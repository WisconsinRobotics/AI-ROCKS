using System;
using Timer = System.Timers.Timer;

using AI_ROCKS.Services;

namespace AI_ROCKS
{
    class Program
    {
        private const long EXECUTE_INTERVAL_MILLIS = 100;  //TODO change
        private const long OBSTACLE_INTERVAL_MILLIS = 100; //TODO change

        static void Main(string[] args)
        {
            // Parse args
            // -t - test mode
            // -s COMX - COM port LRF is on
            // -d X - DriveState to start in (according to StateType enum

            // Create AutonomousService
            AutonomousService autonomousService = new AutonomousService();

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
    }
}
