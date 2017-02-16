using System;

using ObstacleLibrarySharp;

namespace AI_ROCKS.Drive.Utils
{
    class ObstacleEventArgs : EventArgs
    {
        private readonly Plot plot;

        public ObstacleEventArgs(Plot plot)
        {
            this.plot = plot;
        }

        public Plot Data
        {
            get { return this.plot; }
        }
    }
}
