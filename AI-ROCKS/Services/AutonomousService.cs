using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AI_ROCKS.Drive;

namespace AI_ROCKS.Services
{
    class AutonomousService
    {
        private DriveContext driveContext;


        public AutonomousService()
        {
            this.driveContext = new DriveContext();
        }


        public void Execute()
        {
            // Autononous driving code ...


            DriveCommand driveCommand = this.driveContext.FindNextDriveCommand();

            this.driveContext.Drive(driveCommand);


            // Other Autonomous driving code...


            if (this.driveContext.IsStateChangeRequired())
            {
                this.driveContext.ChangeState();
            }

            // Even more Autonomous driving code
        }


    }
}
