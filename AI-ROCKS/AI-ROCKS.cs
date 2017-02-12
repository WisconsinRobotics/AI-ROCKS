using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.Services;

namespace AI_ROCKS
{
    class Program
    {
        static void Main(string[] args)
        {
            // Do things
            AutonomousService autonomousService = new AutonomousService();

            // Set up connection with ROCKS (Service Master?, etc)

            // TODO
            // While connection is present, run autonomous service
            // End after ball is found? -> Have Execute() return bool?
            while (true)
            {
                autonomousService.Execute();
            }
        }
    }
}
