using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using AI_ROCKS.Services;

namespace AI_ROCKS
{
    class Program
    {
        private const long EXECUTE_INTERVAL_MILLIS = 200;


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
            
            // Refer to AWAKE and BadgerJAUS InitializeTimer() -> Execute is hooked up to Timer event executing every x milliseconds
            Timer timer = new Timer(EXECUTE_INTERVAL_MILLIS);
            timer.AutoReset = true;
            timer.Elapsed += autonomousService.Execute;
            timer.Enabled = true;

            // Create thread for ObstacleEvent
            // Tie in to autonomousService.DetectObstacleEvent()
        }
    }
}
