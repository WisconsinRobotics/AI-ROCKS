using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_ROCKS.Drive.Utils
{
    public static class Speed
    {
        public const byte SPEED_NORMAL_OPERATION = 50;
        public const byte SPEED_CLEAR_OBSTACLE = 40;
        public const byte SPEED_AVOID_OBSTACLE = 30;
        public const byte SPEED_SLOW_TURN = 25;
        public const byte SPEED_VISION = 30;
        public const byte SPEED_HALT = 0;
    }
}
